using CommunityToolkit.Mvvm.ComponentModel;

namespace PipelineControl.UI.ViewModels.Shell.Models;

public partial class SideNavItem : ObservableObject
{
    public SideNavItem(string title, string icon, string pageKey)
    {
        Title = title;
        Icon = icon;
        PageKey = pageKey;
    }

    public string Title { get; }

    public string Icon { get; }

    public string PageKey { get; }

    [ObservableProperty]
    private bool isSelected;
}
