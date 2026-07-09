using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PipelineControl.UI.Views.Pages.IoMonitor.Controls;

public partial class IoBitCellView : UserControl
{
    public static readonly DependencyProperty BitIndexProperty =
        DependencyProperty.Register(nameof(BitIndex), typeof(int), typeof(IoBitCellView), new PropertyMetadata(0, OnBitIndexChanged));

    public static readonly DependencyProperty BitTextProperty =
        DependencyProperty.Register(nameof(BitText), typeof(string), typeof(IoBitCellView), new PropertyMetadata("0"));

    public static readonly DependencyProperty SignalTextProperty =
        DependencyProperty.Register(nameof(SignalText), typeof(string), typeof(IoBitCellView), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsOnProperty =
        DependencyProperty.Register(nameof(IsOn), typeof(bool), typeof(IoBitCellView), new PropertyMetadata(false));

    public static readonly DependencyProperty IsOutputProperty =
        DependencyProperty.Register(nameof(IsOutput), typeof(bool), typeof(IoBitCellView), new PropertyMetadata(false));

    public static readonly DependencyProperty IsForcedProperty =
        DependencyProperty.Register(nameof(IsForced), typeof(bool), typeof(IoBitCellView), new PropertyMetadata(false));

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(IoBitCellView), new PropertyMetadata(false));

    public static readonly DependencyProperty CellTooltipProperty =
        DependencyProperty.Register(nameof(CellTooltip), typeof(string), typeof(IoBitCellView), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ClickCommandProperty =
        DependencyProperty.Register(nameof(ClickCommand), typeof(ICommand), typeof(IoBitCellView), new PropertyMetadata(null));

    public static readonly DependencyProperty ShowBitTextProperty =
        DependencyProperty.Register(nameof(ShowBitText), typeof(bool), typeof(IoBitCellView), new PropertyMetadata(true));

    public IoBitCellView()
    {
        InitializeComponent();
    }

    public int BitIndex
    {
        get => (int)GetValue(BitIndexProperty);
        set => SetValue(BitIndexProperty, value);
    }

    public string BitText
    {
        get => (string)GetValue(BitTextProperty);
        set => SetValue(BitTextProperty, value);
    }

    public string SignalText
    {
        get => (string)GetValue(SignalTextProperty);
        set => SetValue(SignalTextProperty, value);
    }

    public bool IsOn
    {
        get => (bool)GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }

    public bool IsOutput
    {
        get => (bool)GetValue(IsOutputProperty);
        set => SetValue(IsOutputProperty, value);
    }

    public bool IsForced
    {
        get => (bool)GetValue(IsForcedProperty);
        set => SetValue(IsForcedProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public string CellTooltip
    {
        get => (string)GetValue(CellTooltipProperty);
        set => SetValue(CellTooltipProperty, value);
    }

    public ICommand? ClickCommand
    {
        get => (ICommand?)GetValue(ClickCommandProperty);
        set => SetValue(ClickCommandProperty, value);
    }

    public bool ShowBitText
    {
        get => (bool)GetValue(ShowBitTextProperty);
        set => SetValue(ShowBitTextProperty, value);
    }

    public CornerRadius CornerRadius => ShowBitText ? new CornerRadius(3) : new CornerRadius(2);

    private static void OnBitIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (IoBitCellView)d;
        control.BitText = ((int)e.NewValue).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private void CellButton_OnPreviewTouchDown(object sender, TouchEventArgs e)
    {
        e.Handled = true;
    }

    private void CellButton_OnPreviewTouchUp(object sender, TouchEventArgs e)
    {
        e.Handled = true;
        ExecuteClickCommand();
    }

    private void CellButton_OnPreviewStylusDown(object sender, StylusDownEventArgs e)
    {
        e.Handled = true;
    }

    private void CellButton_OnPreviewStylusUp(object sender, StylusEventArgs e)
    {
        e.Handled = true;
        ExecuteClickCommand();
    }

    private void ExecuteClickCommand()
    {
        var parameter = Tag;
        if (ClickCommand?.CanExecute(parameter) == true)
        {
            ClickCommand.Execute(parameter);
        }
    }
}
