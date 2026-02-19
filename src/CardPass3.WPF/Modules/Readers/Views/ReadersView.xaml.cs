using CardPass3.WPF.Modules.Readers.ViewModels;
using System.Windows.Controls;

namespace CardPass3.WPF.Modules.Readers.Views
{

public partial class ReadersView : UserControl
{
    public ReadersView(ReadersViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
