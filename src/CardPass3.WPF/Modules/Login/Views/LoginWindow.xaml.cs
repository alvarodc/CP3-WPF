using CardPass3.WPF.Modules.Login.ViewModels;
using CardPass3.WPF.Modules.Shell.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;

namespace CardPass3.WPF.Modules.Login.Views
{

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.LoginSucceeded += OnLoginSucceeded;

        Loaded += (_, _) => TxtOperator.Focus();
    }

    private void OnLoginSucceeded(object? sender, EventArgs e)
    {
        var shell = App.Services.GetRequiredService<ShellWindow>();
        shell.Show();
        Close();
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
}
