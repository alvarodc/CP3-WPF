using CardPass3.WPF.Modules.Login.ViewModels;
using CardPass3.WPF.Modules.Shell.Views;
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

        viewModel.LoginSucceeded  += OnLoginSucceeded;
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
        var result = MessageBox.Show(
            "No se puede conectar con la base de datos.\n\n" +
            "Esto puede deberse a que la configuración es incorrecta o a que la " +
            "contraseña está almacenada en un formato incompatible con esta versión.\n\n" +
            "¿Deseas abrir la pantalla de configuración de base de datos para " +
            "revisar y actualizar los parámetros de conexión?",
            "Error de conexión a la base de datos",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            // TODO (Iter-5): abrir ventana/panel de configuración de BD
            // Por ahora indicamos la ruta del fichero para que el técnico lo edite
            var configPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "CardPass3", "Database", "cp3db.config.json");

            MessageBox.Show(
                $"El fichero de configuración se encuentra en:\n\n{configPath}\n\n" +
                "Puedes editarlo manualmente o esperar a que la pantalla de " +
                "configuración esté disponible en la próxima versión.\n\n" +
                "Tras modificarlo, reinicia la aplicación.",
                "Configuración de base de datos",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
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
