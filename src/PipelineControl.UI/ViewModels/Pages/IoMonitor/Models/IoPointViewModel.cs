using CommunityToolkit.Mvvm.ComponentModel;

namespace PipelineControl.UI.ViewModels.Pages.IoMonitor.Models;

public partial class IoPointViewModel : ObservableObject
{
    public int PointNo { get; init; }

    public int ModuleIndex { get; init; }

    public int BitIndex { get; init; }

    public int LocalIndex { get; init; }

    public string ModuleName { get; init; } = string.Empty;

    public string GlobalLabel { get; init; } = string.Empty;

    public string DisplayLabel { get; init; } = string.Empty;

    public string SignalName { get; init; } = string.Empty;

    public bool HasSignalName => !string.IsNullOrWhiteSpace(SignalName);

    public string SignalDisplayName => FormatSignalName(SignalName);

    public string CellSignalText => IsOutput ? string.Empty : SignalDisplayName;

    public string QualifiedLabel => string.IsNullOrWhiteSpace(ModuleName) ? DisplayLabel : $"{ModuleName} {DisplayLabel}";

    public string TagName { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool IsOutput { get; init; }

    public bool IsEnabled { get; init; } = true;

    public bool SafeDefaultValue { get; init; }

    public bool ShowBitText { get; init; }

    public string BitText => string.IsNullOrWhiteSpace(DisplayLabel)
        ? BitIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)
        : DisplayLabel;

    public string TooltipText => HasSignalName
        ? $"{ModuleName} · {DisplayLabel} · {SignalName}\nmodule={ModuleIndex}, bit={BitIndex}\n全局序号 {PointNo} = {(IsOn ? "开" : "关")}"
        : $"{ModuleName} · {DisplayLabel}\n{Description}\nmodule={ModuleIndex}, bit={BitIndex}\n全局序号 {PointNo} = {(IsOn ? "开" : "关")}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TooltipText))]
    private bool isOn;

    [ObservableProperty]
    private bool isForced;

    [ObservableProperty]
    private bool isSelected;

    private static string FormatSignalName(string signalName)
    {
        if (string.IsNullOrWhiteSpace(signalName))
        {
            return string.Empty;
        }

        return signalName switch
        {
            "线头第一工位光电" => "线头第一工位\n光电",
            "线头防呆光电" => "线头防呆\n光电",
            "线头升降台行程开关" => "线头升降台\n行程开关",
            "线尾升降台行程开关" => "线尾升降台\n行程开关",
            "线头下限位开关" => "线头下限位\n开关",
            "线头上限位开关" => "线头上限位\n开关",
            "线尾下限开关" => "线尾下限\n开关",
            "线尾上限开关" => "线尾上限\n开关",
            "线尾AGV信号" => "线尾\nAGV信号",
            _ => signalName.Length > 6
                ? $"{signalName[..6]}\n{signalName[6..]}"
                : signalName
        };
    }
}

