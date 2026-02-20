using CardPass3.WPF.Data.Models;
using CardPass3.WPF.Data.Repositories.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CardPass3.WPF.Modules.Readers.ViewModels;

/// <summary>
/// ViewModel del formulario alta/edición de lector.
/// Se instancia con un Reader existente (edición) o vacío (alta).
/// </summary>
public partial class ReaderFormViewModel : ObservableObject
{
    private readonly IAreaRepository _areaRepo;

    // ── Propiedades del formulario ────────────────────────────────────────────

    [ObservableProperty] private string  _description        = string.Empty;
    [ObservableProperty] private string  _uniqueName         = string.Empty;
    [ObservableProperty] private string  _ipAddress          = "rpi110.local";
    [ObservableProperty] private string  _ipAddressEffective = string.Empty;
    [ObservableProperty] private int     _port               = 5000;
    [ObservableProperty] private int     _controlType        = 0; // 0=Entrada, 1=Salida
    [ObservableProperty] private bool    _enabled            = true;
    [ObservableProperty] private Area?   _selectedArea;
    [ObservableProperty] private string  _validationError    = string.Empty;

    public ObservableCollection<Area>   Areas        { get; } = new();
    public ObservableCollection<string> ControlTypes { get; } = new() { "Entrada", "Salida" };

    public bool IsEditMode { get; }
    public string Title => IsEditMode ? "Editar lector" : "Nuevo lector";

    // ── Resultado ─────────────────────────────────────────────────────────────

    public Reader? ResultReader { get; private set; }
    public bool    Confirmed    { get; private set; }

    // Evento que la vista escucha para cerrarse
    public event Action<bool>? CloseRequested;

    private readonly Reader _original;

    public ReaderFormViewModel(IAreaRepository areaRepo, Reader? reader = null)
    {
        _areaRepo  = areaRepo;
        IsEditMode = reader is not null;
        _original  = reader ?? new Reader();

        if (IsEditMode)
        {
            Description        = _original.ReaderDescription;
            UniqueName         = _original.UniqueName ?? string.Empty;
            IpAddress          = _original.IpAddress;
            IpAddressEffective = _original.IpAddressEffective ?? string.Empty;
            Port               = _original.Port;
            ControlType        = _original.ControlType;
            Enabled            = _original.Enabled;
        }
    }

    public async Task LoadAreasAsync(CancellationToken ct = default)
    {
        var areas = await _areaRepo.GetAllAsync(ct);
        Areas.Clear();
        foreach (var a in areas) Areas.Add(a);

        // Seleccionar el área actual en edición
        if (IsEditMode)
            SelectedArea = Areas.FirstOrDefault(a => a.IdArea == _original.AreasIdArea);
        else
            SelectedArea = Areas.FirstOrDefault();
    }

    // ── Validación y confirmación ─────────────────────────────────────────────

    [RelayCommand]
    private void Confirm()
    {
        if (!Validate()) return;

        ResultReader = new Reader
        {
            IdReader           = _original.IdReader,
            ReaderDescription  = Description.Trim(),
            UniqueName         = UniqueName.Trim(),
            IpAddress          = IpAddress.Trim(),
            IpAddressEffective = IpAddressEffective.Trim(),
            Port               = Port,
            ControlType        = ControlType,
            Enabled            = Enabled,
            AreasIdArea        = SelectedArea?.IdArea ?? 0,
            Driver             = 0,        // ReaderPi — único driver soportado
            VerifyMode         = 1,        // VerifyCard
            LicensesIdLicense  = _original.LicensesIdLicense,
            UseReaderDelay     = _original.UseReaderDelay,
            ReaderDelay        = _original.ReaderDelay,
        };

        Confirmed = true;
        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);

    private bool Validate()
    {
        ValidationError = string.Empty;

        if (string.IsNullOrWhiteSpace(Description))
        { ValidationError = "La descripción es obligatoria."; return false; }

        if (string.IsNullOrWhiteSpace(UniqueName))
        { ValidationError = "El nombre único es obligatorio."; return false; }

        if (string.IsNullOrWhiteSpace(IpAddress))
        { ValidationError = "La dirección IP es obligatoria."; return false; }

        if (Port <= 0)
        { ValidationError = "El puerto debe ser mayor que 0."; return false; }

        if (SelectedArea is null)
        { ValidationError = "Debe seleccionar un área."; return false; }

        return true;
    }
}
