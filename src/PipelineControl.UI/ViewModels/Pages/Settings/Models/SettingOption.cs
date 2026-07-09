using CommunityToolkit.Mvvm.ComponentModel;

namespace PipelineControl.UI.ViewModels.Pages.Settings.Models;

public sealed partial class SettingOption : ObservableObject
{
    private readonly Action<SettingOption, bool>? selectionChanged;

    public SettingOption(string label, string value, Action<SettingOption, bool>? selectionChanged = null)
    {
        Label = label;
        Value = value;
        this.selectionChanged = selectionChanged;
    }

    public string Label { get; }

    public string Value { get; }

    [ObservableProperty]
    private bool isSelected;

    partial void OnIsSelectedChanged(bool value)
    {
        selectionChanged?.Invoke(this, value);
    }
}
