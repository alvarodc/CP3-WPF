using CardPass3.WPF.Modules.Readers.ViewModels;
using System.Windows;

namespace CardPass3.WPF.Modules.Readers.Views;

public partial class ReaderFormWindow : Window
{
    public ReaderFormWindow(ReaderFormViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        // El ViewModel pide el cierre vía evento — desacopla VM de la Window
        vm.CloseRequested += confirmed =>
        {
            DialogResult = confirmed;
            Close();
        };

        Loaded += async (_, _) => await vm.LoadAreasAsync();
    }
}
