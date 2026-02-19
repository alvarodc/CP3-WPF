using CardPass3.WPF.Modules.Login.ViewModels;
using CardPass3.WPF.Modules.Shell.Views;
using CardPass3.WPF.Services.Database;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;

namespace CardPass3.WPF.Modules.Login.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.LoginSucceeded   += OnLoginSucceeded;
        viewModel.DbConfigRequired += OnDbConfigRequired;

        Loaded += (_, _) => TxtOperator.Focus();
    }

    private void OnLoginSucceeded(object? sender, EventArgs e)
    {
        var shell = App.Services.GetRequiredService<ShellWindow>();
        shell.Show();
        Close();
    }

    private void OnDbConfigRequired(object? sender, EventArgs e)
    {
        var dialog = new DbConnectionErrorDialog();
        dialog.Owner = this;
        dialog.ShowDialog();

        switch (dialog.Result)
        {
            case DbConnectionErrorResult.Configure:
                // TODO (Iter-5): abrir pantalla de configuración de BD
                MessageBox.Show(
                    "La pantalla de configuración de base de datos estará disponible en la próxima versión.",
                    "Próximamente", MessageBoxButton.OK, MessageBoxImage.Information);
                break;

            case DbConnectionErrorResult.Reset:
                var dbConfig = App.Services.GetRequiredService<IDatabaseConfigService>();
                dbConfig.ResetToDefault();
                MessageBox.Show(
                    "La configuración se ha restablecido a los valores por defecto.\n\n" +
                    "  Servidor:  localhost\n" +
                    "  Puerto:    3306\n" +
                    "  Usuario:   cardpass3\n" +
                    "  Contraseña: cardpass3\n" +
                    "  Base de datos: cardpass3\n\n" +
                    "Vuelve a intentar iniciar sesión.",
                    "Configuración restablecida", MessageBoxButton.OK, MessageBoxImage.Information);
                break;

            // DbConnectionErrorResult.Back → no hacer nada, el usuario vuelve al login
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var vm = (LoginViewModel)DataContext;
            if (vm.LoginCommand.CanExecute(TxtPassword.Password))
                vm.LoginCommand.Execute(TxtPassword.Password);
        }
    }
}
