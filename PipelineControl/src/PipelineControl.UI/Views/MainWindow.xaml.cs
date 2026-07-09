using PipelineControl.UI.ViewModels;
using System.Windows;

namespace PipelineControl.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
