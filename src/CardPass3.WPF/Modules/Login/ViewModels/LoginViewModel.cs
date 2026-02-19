using CardPass3.WPF.Data.Repositories.Interfaces;
using CardPass3.WPF.Services.Readers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace CardPass3.WPF.Modules.Login.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IOperatorRepository _operatorRepo;
    private readonly IReaderConnectionService _readerService;
    private readonly ILogger<LoginViewModel> _logger;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _operatorName = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoggingIn;

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

            // Open main shell
            var shell = App.Services.GetRequiredService<ShellWindow>();
            shell.Show();

            // Start reader connections asynchronously — shell is already usable
            _ = Task.Run(() => _readerService.StartAsync());

            // Close login window
            Application.Current.Windows.OfType<LoginWindow>().FirstOrDefault()?.Close();
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
    /// Verifies password against the hex-encoded SHA-256 hash stored in the DB.
    /// The original system stores passwords as space-separated hex bytes.
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

// Forward declaration — LoginWindow and ShellWindow are defined in their Views folders
public class LoginWindow : Window { }
public class ShellWindow : Window { }
