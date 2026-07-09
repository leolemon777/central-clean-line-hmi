using CommunityToolkit.Mvvm.ComponentModel;

namespace PipelineControl.UI.ViewModels.Pages.Servo.Models;

public enum ServoRunDirection
{
    Stopped,
    Forward,
    Reverse
}

public sealed partial class ServoAxisViewModel : ObservableObject
{
    public ServoAxisViewModel(int axis, string name, int station)
    {
        Axis = axis;
        Name = name;
        Station = station;
    }

    public int Axis { get; }

    public string Name { get; }

    public int Station { get; }

    [ObservableProperty]
    private bool isEnabled;

    [ObservableProperty]
    private bool isOnline;

    [ObservableProperty]
    private int targetRpm;

    [ObservableProperty]
    private int actualRpm;

    [ObservableProperty]
    private int faultCode;

    [ObservableProperty]
    private string speedInputText = "100";

    [ObservableProperty]
    private bool isBusy;

    // 是否为当前选中轴（左侧列表高亮用），由 ServoViewModel.SelectAxis 维护
    [ObservableProperty]
    private bool isSelected;

    // 当前运行方向，由上位机命令维护：正转/反转/停止。负号转速=反转。
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsForward))]
    [NotifyPropertyChangedFor(nameof(IsReverse))]
    [NotifyPropertyChangedFor(nameof(IsStopped))]
    [NotifyPropertyChangedFor(nameof(DirectionText))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private ServoRunDirection direction = ServoRunDirection.Stopped;

    public bool HasFault => FaultCode != 0;

    public bool CanToggle => !IsBusy;

    public bool IsForward => Direction == ServoRunDirection.Forward;

    public bool IsReverse => Direction == ServoRunDirection.Reverse;

    public bool IsStopped => Direction == ServoRunDirection.Stopped;

    public string FaultText => FaultCode == 0 ? string.Empty : $"Err.{FaultCode:D3}";

    public string DirectionText => Direction switch
    {
        ServoRunDirection.Forward => "正转",
        ServoRunDirection.Reverse => "反转",
        _ => "停止"
    };

    public string StatusText => HasFault
        ? FaultText
        : !IsOnline ? "离线"
        : !IsEnabled ? "已停用"
        : Direction == ServoRunDirection.Stopped ? "已使能"
        : $"{DirectionText} {ActualRpm} rpm";

    partial void OnIsEnabledChanged(bool value) => OnPropertyChanged(nameof(StatusText));

    partial void OnIsOnlineChanged(bool value) => OnPropertyChanged(nameof(StatusText));

    partial void OnFaultCodeChanged(int value)
    {
        OnPropertyChanged(nameof(HasFault));
        OnPropertyChanged(nameof(FaultText));
        OnPropertyChanged(nameof(StatusText));
    }

    partial void OnActualRpmChanged(int value) => OnPropertyChanged(nameof(StatusText));

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(CanToggle));

    public void ApplyState(bool enabled, bool online, int targetRpm, int actualRpm, int faultCode)
    {
        IsEnabled = enabled;
        IsOnline = online;
        TargetRpm = targetRpm;
        ActualRpm = actualRpm;
        FaultCode = faultCode;
        // 离线/未使能/故障 → 方向回停止
        if (!enabled || !online || faultCode != 0)
        {
            Direction = ServoRunDirection.Stopped;
        }
    }
}
