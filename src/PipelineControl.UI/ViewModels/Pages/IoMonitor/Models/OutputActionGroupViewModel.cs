using CommunityToolkit.Mvvm.ComponentModel;

namespace PipelineControl.UI.ViewModels.Pages.IoMonitor.Models;

public partial class OutputActionGroupViewModel : ObservableObject
{
    public OutputActionGroupViewModel(string key, string displayName, IReadOnlyList<int> pointNos, IReadOnlyList<string>? conflictKeys = null)
    {
        Key = key;
        DisplayName = displayName;
        PointNos = pointNos;
        ConflictKeys = conflictKeys ?? Array.Empty<string>();
    }

    public string Key { get; }

    public string DisplayName { get; }

    public IReadOnlyList<int> PointNos { get; }

    public IReadOnlyList<string> ConflictKeys { get; }

    public string PointText => string.Join(" + ", PointNos.Select(ToLocalOutputLabel));

    [ObservableProperty]
    private bool isOn;

    private static string ToLocalOutputLabel(int pointNo) => $"Y{pointNo - 17}";
}
