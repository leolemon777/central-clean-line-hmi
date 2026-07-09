using PipelineControl.UI.ViewModels.Pages.IoMonitor.Models;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PipelineControl.UI.Views.Pages.IoMonitor.Controls;

public partial class IoBitGridView : UserControl
{
    public static readonly DependencyProperty ModuleLabelProperty =
        DependencyProperty.Register(nameof(ModuleLabel), typeof(string), typeof(IoBitGridView), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty RegisterValueHexProperty =
        DependencyProperty.Register(nameof(RegisterValueHex), typeof(string), typeof(IoBitGridView), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ColumnsProperty =
        DependencyProperty.Register(nameof(Columns), typeof(int), typeof(IoBitGridView), new PropertyMetadata(16));

    public static readonly DependencyProperty CellWidthProperty =
        DependencyProperty.Register(nameof(CellWidth), typeof(double), typeof(IoBitGridView), new PropertyMetadata(34D));

    public static readonly DependencyProperty CellHeightProperty =
        DependencyProperty.Register(nameof(CellHeight), typeof(double), typeof(IoBitGridView), new PropertyMetadata(24D));

    public static readonly DependencyProperty PointsProperty =
        DependencyProperty.Register(nameof(Points), typeof(IList), typeof(IoBitGridView), new PropertyMetadata(null));

    public static readonly DependencyProperty IsOutputProperty =
        DependencyProperty.Register(nameof(IsOutput), typeof(bool), typeof(IoBitGridView), new PropertyMetadata(false));

    public static readonly DependencyProperty SelectedBitProperty =
        DependencyProperty.Register(nameof(SelectedBit), typeof(int), typeof(IoBitGridView), new PropertyMetadata(-1));

    public static readonly DependencyProperty SelectionChangedCommandProperty =
        DependencyProperty.Register(nameof(SelectionChangedCommand), typeof(ICommand), typeof(IoBitGridView), new PropertyMetadata(null));

    public IoBitGridView()
    {
        InitializeComponent();
    }

    public string ModuleLabel
    {
        get => (string)GetValue(ModuleLabelProperty);
        set => SetValue(ModuleLabelProperty, value);
    }

    public string RegisterValueHex
    {
        get => (string)GetValue(RegisterValueHexProperty);
        set => SetValue(RegisterValueHexProperty, value);
    }

    public int Columns
    {
        get => (int)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    public double CellWidth
    {
        get => (double)GetValue(CellWidthProperty);
        set => SetValue(CellWidthProperty, value);
    }

    public double CellHeight
    {
        get => (double)GetValue(CellHeightProperty);
        set => SetValue(CellHeightProperty, value);
    }

    public IList? Points
    {
        get => (IList?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public bool IsOutput
    {
        get => (bool)GetValue(IsOutputProperty);
        set => SetValue(IsOutputProperty, value);
    }

    public int SelectedBit
    {
        get => (int)GetValue(SelectedBitProperty);
        set => SetValue(SelectedBitProperty, value);
    }

    public ICommand? SelectionChangedCommand
    {
        get => (ICommand?)GetValue(SelectionChangedCommandProperty);
        set => SetValue(SelectionChangedCommandProperty, value);
    }
}
