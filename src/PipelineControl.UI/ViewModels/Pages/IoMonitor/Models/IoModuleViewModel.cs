using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace PipelineControl.UI.ViewModels.Pages.IoMonitor.Models;

public partial class IoModuleViewModel : ObservableObject
{
    public IoModuleViewModel(
        string moduleLabel,
        int columns,
        bool isOutput,
        IEnumerable<IoPointViewModel> points,
        double cellWidth,
        double cellHeight)
    {
        ModuleLabel = moduleLabel;
        Columns = columns;
        IsOutput = isOutput;
        CellWidth = cellWidth;
        CellHeight = cellHeight;
        Points = new ObservableCollection<IoPointViewModel>(points);
        RefreshRegisterValue();
    }

    public string ModuleLabel { get; }

    public int Columns { get; }

    public bool IsOutput { get; }

    public double CellWidth { get; }

    public double CellHeight { get; }

    public ObservableCollection<IoPointViewModel> Points { get; }

    [ObservableProperty]
    private string registerValueHex = "ON 0 / OFF 0";

    public void RefreshRegisterValue()
    {
        var onCount = Points.Count(point => point.IsOn);
        RegisterValueHex = $"ON {onCount} / OFF {Points.Count - onCount}";
    }
}
