using CardPass3.WPF.Data.Repositories.Interfaces;
using CardPass3.WPF.Services.Readers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CardPass3.WPF.Modules.Login.ViewModels
{

public partial class LoginViewModel : ObservableObject
{
    private readonly IOperatorRepository _operatorRepo;
    private readonly IReaderConnectionService _readerService;
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

    /// <summary>
    /// Raised on successful login. LoginWindow subscribes and handles
    /// opening the Shell + closing itself — no View references needed here.
    /// </summary>
    public event EventHandler? LoginSucceeded;

    public LoginViewModel(
        IOperatorRepository operatorRepo,
        IReaderConnectionService readerService,
        ILogger<LoginViewModel> logger)
    {
        _operatorRepo = operatorRepo;
        _readerService = readerService;
        _logger = logger;
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync(string password)
    {
        IsLoggingIn = true;
        ErrorMessage = string.Empty;

        try
        {
            var op = await _operatorRepo.GetByNameAsync(OperatorName);

            if (op is null || !VerifyPassword(password, op.Password))
            {
                ErrorMessage = "Usuario o contraseña incorrectos.";
                _logger.LogWarning("Failed login attempt for operator '{Name}'.", OperatorName);
                return;
            }

            var functions = (await _operatorRepo.GetFunctionsAsync(op.IdOperator)).ToHashSet();
            op.FunctionNames.AddRange(functions);

            _logger.LogInformation("Operator '{Name}' logged in successfully.", OperatorName);

            // Start reader connections in background — Shell will be usable immediately
            _ = Task.Run(() => _readerService.StartAsync());

            // Notify the View to handle the window transition
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error de conexión con la base de datos.";
            _logger.LogError(ex, "Unexpected error during login.");
        }
        finally
        {
            IsLoggingIn = false;
        }
    }

    private bool CanLogin() => !string.IsNullOrWhiteSpace(OperatorName);

    /// <summary>
    /// Verifies the password against the hex-encoded SHA-256 hash stored in the DB.
    /// The original system stores passwords as space-separated uppercase hex bytes.
    /// </summary>
    private static bool VerifyPassword(string plaintext, string storedHash)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var hash = sha.ComputeHash(bytes);
        var computed = string.Join(" ", hash.Select(b => b.ToString("X2")));
        return string.Equals(computed, storedHash, StringComparison.OrdinalIgnoreCase);
    }
}
