using System.Windows;
using CsgoConfigReplace.App.ViewModels;

namespace CsgoConfigReplace.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.InitializeCommand.ExecuteAsync(null);
    }
}
