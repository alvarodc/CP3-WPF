using CardPass3.WPF.Data.Repositories.Interfaces;
using CardPass3.WPF.Services.Database;
using CardPass3.WPF.Services.Readers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CardPass3.WPF.Modules.Login.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IOperatorRepository _operatorRepo;
    private readonly IReaderConnectionService _readerService;
    private readonly IDatabaseConfigService _dbConfig;
    private readonly ILogger<LoginViewModel> _logger;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _operatorName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [ObservableProperty]
    private bool _isLoggingIn;

    /// <summary>Login correcto — la View abre ShellWindow y cierra LoginWindow.</summary>
    public event EventHandler? LoginSucceeded;

    /// <summary>
    /// La contraseña de BD no se pudo descifrar o la conexión es inválida.
    /// La View debe ofrecer navegar a la pantalla de configuración de BD.
    /// </summary>
    public event EventHandler? DbConfigRequired;

    public LoginViewModel(
        IOperatorRepository operatorRepo,
        IReaderConnectionService readerService,
        IDatabaseConfigService dbConfig,
        ILogger<LoginViewModel> logger)
    {
        _operatorRepo = operatorRepo;
        _readerService = readerService;
        _dbConfig = dbConfig;
        _logger = logger;
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync(string password)
    {
        IsLoggingIn = true;
        ErrorMessage = string.Empty;

        try
        {
            // ── Detectar configuración de BD inválida antes de intentar conectar ──
            if (_dbConfig.IsPasswordCorrupted)
            {
                _logger.LogWarning("Contraseña de BD corrupta o en formato antiguo. Solicitando reconfiguración.");
                DbConfigRequired?.Invoke(this, EventArgs.Empty);
                return;
            }

            var op = await _operatorRepo.GetByNameAsync(OperatorName);

            if (op is null || !VerifyPassword(password, op.Password))
            {
                ErrorMessage = "Usuario o contraseña incorrectos.";
                _logger.LogWarning("Intento de login fallido para el operador '{Name}'.", OperatorName);
                return;
            }

            var functions = (await _operatorRepo.GetFunctionsAsync(op.IdOperator)).ToHashSet();
            op.FunctionNames.AddRange(functions);
            _logger.LogInformation("Operador '{Name}' autenticado correctamente.", OperatorName);

            _ = Task.Run(() => _readerService.StartAsync());
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (MySqlConnector.MySqlException ex) when (
            ex.Message.Contains("Access denied") ||
            ex.Message.Contains("Unable to connect") ||
            ex.Message.Contains("Connection refused"))
        {
            // Error de conexión a BD — ofrecer reconfigurar en lugar de mensaje técnico
            _logger.LogError(ex, "Error de conexión a la base de datos.");
            DbConfigRequired?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error inesperado. Consulta los logs para más detalles.";
            _logger.LogError(ex, "Error inesperado durante el login.");
        }
        finally
        {
            IsLoggingIn = false;
        }
    }

    private bool CanLogin() => !string.IsNullOrWhiteSpace(OperatorName);

    private static bool VerifyPassword(string plaintext, string storedHash)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes   = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var hash    = sha.ComputeHash(bytes);
        var computed = string.Join(" ", hash.Select(b => b.ToString("X2")));
        return string.Equals(computed, storedHash, StringComparison.OrdinalIgnoreCase);
    }
}
