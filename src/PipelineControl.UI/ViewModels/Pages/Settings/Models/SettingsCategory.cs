using CommunityToolkit.Mvvm.ComponentModel;

namespace PipelineControl.UI.ViewModels.Pages.Settings.Models;

public sealed partial class SettingsCategory : ObservableObject
{
    public SettingsCategory(string key, string icon, string name, string description)
    {
        Key = key;
        Icon = icon;
        Name = name;
        Description = description;
    }

    public string Key { get; }

    public string Icon { get; }

    public string Name { get; }

    public string Description { get; }

    [ObservableProperty]
    private bool isSelected;
}
