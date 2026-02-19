using System.Windows;

namespace CardPass3.WPF.Modules.Login.Views;

public enum DbConnectionErrorResult { Configure, Reset, Back }

public partial class DbConnectionErrorDialog : Window
{
    public DbConnectionErrorResult Result { get; private set; } = DbConnectionErrorResult.Back;

    public DbConnectionErrorDialog() => InitializeComponent();

    private void OnConfigure(object sender, RoutedEventArgs e)
    {
        Result = DbConnectionErrorResult.Configure;
        Close();
    }

    private void OnReset(object sender, RoutedEventArgs e)
    {
        Result = DbConnectionErrorResult.Reset;
        Close();
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        Result = DbConnectionErrorResult.Back;
        Close();
    }
}
