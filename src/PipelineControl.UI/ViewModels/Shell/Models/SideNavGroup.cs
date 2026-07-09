using System.Collections.ObjectModel;

namespace PipelineControl.UI.ViewModels.Shell.Models;

public class SideNavGroup
{
    public SideNavGroup(string title, IEnumerable<SideNavItem> items, bool hasTopSeparator = false)
    {
        Title = title;
        Items = new ObservableCollection<SideNavItem>(items);
        HasTopSeparator = hasTopSeparator;
    }

    public string Title { get; }

    public ObservableCollection<SideNavItem> Items { get; }

    public bool HasTopSeparator { get; }
}
