using PipelineControl.UI.ViewModels.Pages.IoMonitor;
using PipelineControl.UI.ViewModels.Pages.IoMonitor.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;

namespace PipelineControl.UI.Views.Pages.IoMonitor;

public partial class OutputTestPage : UserControl
{
    private readonly HashSet<OutputActionGroupViewModel> activeJogGroups = new();

    public OutputTestPage()
    {
        InitializeComponent();
    }

    private async void ActionButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (sender is Button button)
        {
            button.CaptureMouse();
            await BeginJogAsync(button);
        }
    }

    private async void ActionButton_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (sender is Button button)
        {
            await EndJogAsync(button);
            if (button.IsMouseCaptured)
            {
                button.ReleaseMouseCapture();
            }
        }
    }

    private async void ActionButton_OnLostMouseCapture(object sender, MouseEventArgs e)
    {
        if (sender is Button button)
        {
            await EndJogAsync(button);
        }
    }

    private async void ActionButton_OnPreviewTouchDown(object sender, TouchEventArgs e)
    {
        e.Handled = true;
        if (sender is Button button)
        {
            button.CaptureTouch(e.TouchDevice);
            await BeginJogAsync(button);
        }
    }

    private async void ActionButton_OnPreviewTouchUp(object sender, TouchEventArgs e)
    {
        e.Handled = true;
        if (sender is Button button)
        {
            await EndJogAsync(button);
            button.ReleaseTouchCapture(e.TouchDevice);
        }
    }

    private async void ActionButton_OnLostTouchCapture(object sender, TouchEventArgs e)
    {
        if (sender is Button button)
        {
            await EndJogAsync(button);
        }
    }

    private async void ActionButton_OnPreviewStylusDown(object sender, StylusDownEventArgs e)
    {
        e.Handled = true;
        if (sender is Button button)
        {
            button.CaptureStylus();
            await BeginJogAsync(button);
        }
    }

    private async void ActionButton_OnPreviewStylusUp(object sender, StylusEventArgs e)
    {
        e.Handled = true;
        if (sender is Button button)
        {
            await EndJogAsync(button);
            if (button.IsStylusCaptured)
            {
                button.ReleaseStylusCapture();
            }
        }
    }

    private async void ActionButton_OnLostStylusCapture(object sender, StylusEventArgs e)
    {
        if (sender is Button button)
        {
            await EndJogAsync(button);
        }
    }

    private async Task BeginJogAsync(Button button)
    {
        if (button.DataContext is not OutputActionGroupViewModel group
            || DataContext is not OutputTestViewModel viewModel
            || !activeJogGroups.Add(group))
        {
            return;
        }

        await viewModel.BeginOutputActionGroupCommand.ExecuteAsync(group);
        if (!group.IsOn)
        {
            activeJogGroups.Remove(group);
        }
    }

    private async Task EndJogAsync(Button button)
    {
        if (button.DataContext is not OutputActionGroupViewModel group
            || DataContext is not OutputTestViewModel viewModel
            || !activeJogGroups.Remove(group))
        {
            return;
        }

        await viewModel.EndOutputActionGroupCommand.ExecuteAsync(group);
    }

    private void CommandButton_OnPreviewTouchDown(object sender, TouchEventArgs e)
    {
        e.Handled = true;
    }

    private void CommandButton_OnPreviewTouchUp(object sender, TouchEventArgs e)
    {
        e.Handled = true;
        ExecuteTouchCommand(sender);
    }

    private void CommandButton_OnPreviewStylusDown(object sender, StylusDownEventArgs e)
    {
        e.Handled = true;
    }

    private void CommandButton_OnPreviewStylusUp(object sender, StylusEventArgs e)
    {
        e.Handled = true;
        ExecuteTouchCommand(sender);
    }

    private static void ExecuteTouchCommand(object sender)
    {
        switch (sender)
        {
            case ToggleButton toggleButton:
                toggleButton.IsChecked = toggleButton.IsChecked != true;
                break;
            case ButtonBase button when button.Command?.CanExecute(button.CommandParameter) == true:
                button.Command.Execute(button.CommandParameter);
                break;
        }
    }
}
