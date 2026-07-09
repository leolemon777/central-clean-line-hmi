using System.Windows;
using System.Windows.Input;

namespace PipelineControl.UI.Views.Shell;

public partial class MainWindow : Window
{
    private const double DesignMinWidth = 1024;
    private const double DesignMinHeight = 640;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_OnLoaded;
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyWorkingAreaBounds();
        WindowState = WindowState.Maximized;
    }

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleWindowState();
            return;
        }

        DragMove();
    }

    private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        ToggleWindowState();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleWindowState()
    {
        ApplyWorkingAreaBounds();
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void ApplyWorkingAreaBounds()
    {
        var workArea = SystemParameters.WorkArea;
        if (workArea.Width <= 0 || workArea.Height <= 0)
        {
            return;
        }

        MinWidth = Math.Min(DesignMinWidth, workArea.Width);
        MinHeight = Math.Min(DesignMinHeight, workArea.Height);
        MaxWidth = workArea.Width;
        MaxHeight = workArea.Height;
    }
}
