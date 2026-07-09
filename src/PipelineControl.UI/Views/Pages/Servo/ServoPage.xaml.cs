using PipelineControl.UI.ViewModels.Pages.Servo;
using PipelineControl.UI.ViewModels.Pages.Servo.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PipelineControl.UI.Views.Pages.Servo;

public partial class ServoPage : UserControl
{
    private bool isJogActive;

    public ServoPage()
    {
        InitializeComponent();
    }

    // 左侧轴列表：点击整行选中该轴
    private void AxisRow_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ServoAxisViewModel axis
            && DataContext is ServoViewModel viewModel)
        {
            viewModel.SelectAxisCommand.Execute(axis);
        }
    }

    // 工控机触摸屏兜底：部分触摸驱动不走标准鼠标 Click，这里在 Touch/Stylus 抬起时主动触发命令。
    private void CommandButton_OnPreviewTouchUp(object sender, TouchEventArgs e)
    {
        if (sender is Button button && button.Command?.CanExecute(button.CommandParameter) == true)
        {
            button.Command.Execute(button.CommandParameter);
            e.Handled = true;
        }
    }

    private void CommandButton_OnPreviewStylusUp(object sender, StylusEventArgs e)
    {
        if (sender is Button button && button.Command?.CanExecute(button.CommandParameter) == true)
        {
            button.Command.Execute(button.CommandParameter);
            e.Handled = true;
        }
    }

    private void JogButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (TryBeginJog(sender))
        {
            if (sender is UIElement element)
            {
                element.CaptureMouse();
            }

            e.Handled = true;
        }
    }

    private void JogButton_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (TryEndJog(sender))
        {
            if (sender is UIElement element)
            {
                element.ReleaseMouseCapture();
            }

            e.Handled = true;
        }
    }

    private void JogButton_OnLostMouseCapture(object sender, MouseEventArgs e)
    {
        TryEndJog(sender);
    }

    private void JogButton_OnPreviewTouchDown(object sender, TouchEventArgs e)
    {
        if (TryBeginJog(sender))
        {
            if (sender is UIElement element)
            {
                element.CaptureTouch(e.TouchDevice);
            }

            e.Handled = true;
        }
    }

    private void JogButton_OnPreviewTouchUp(object sender, TouchEventArgs e)
    {
        if (TryEndJog(sender))
        {
            if (sender is UIElement element)
            {
                element.ReleaseTouchCapture(e.TouchDevice);
            }

            e.Handled = true;
        }
    }

    private void JogButton_OnLostTouchCapture(object sender, TouchEventArgs e)
    {
        TryEndJog(sender);
    }

    private void JogButton_OnPreviewStylusDown(object sender, StylusDownEventArgs e)
    {
        if (TryBeginJog(sender))
        {
            if (sender is UIElement element)
            {
                element.CaptureStylus();
            }

            e.Handled = true;
        }
    }

    private void JogButton_OnPreviewStylusUp(object sender, StylusEventArgs e)
    {
        if (TryEndJog(sender))
        {
            if (sender is UIElement element)
            {
                element.ReleaseStylusCapture();
            }

            e.Handled = true;
        }
    }

    private void JogButton_OnLostStylusCapture(object sender, StylusEventArgs e)
    {
        TryEndJog(sender);
    }

    private bool TryBeginJog(object sender)
    {
        if (DataContext is not ServoViewModel viewModel || !viewModel.IsJogMode)
        {
            return false;
        }

        if (sender is Button button && button.Command?.CanExecute(button.CommandParameter) == true)
        {
            isJogActive = true;
            button.Command.Execute(button.CommandParameter);
            return true;
        }

        return false;
    }

    private bool TryEndJog(object sender)
    {
        if (DataContext is not ServoViewModel viewModel || !viewModel.IsJogMode || !isJogActive)
        {
            return false;
        }

        isJogActive = false;
        var parameter = sender is Button button ? button.CommandParameter : viewModel.SelectedAxis;
        if (viewModel.EndJogCommand.CanExecute(parameter))
        {
            viewModel.EndJogCommand.Execute(parameter);
        }

        return true;
    }
}
