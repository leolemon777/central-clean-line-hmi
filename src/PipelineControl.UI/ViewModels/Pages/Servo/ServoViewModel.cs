using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PipelineControl.UI.Services.Io;
using PipelineControl.UI.Services.Servo;
using PipelineControl.UI.Services.Servo.Mapping;
using PipelineControl.UI.ViewModels.Pages.Servo.Models;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace PipelineControl.UI.ViewModels.Pages.Servo;

public sealed partial class ServoViewModel : ObservableObject
{
    private const int SpeedStepRpm = 10;
    private const int UiMaxSpeedRpm = 9000;

    private readonly ServoService servoService;
    private readonly Dispatcher dispatcher;
    private readonly Dictionary<int, ServoAxisViewModel> axisLookup = new();

    public ServoViewModel(ServoService servoService)
    {
        this.servoService = servoService;
        var appDispatcher = System.Windows.Application.Current?.Dispatcher;
        dispatcher = appDispatcher is { HasShutdownStarted: false, HasShutdownFinished: false }
                     && appDispatcher.CheckAccess()
            ? appDispatcher
            : Dispatcher.CurrentDispatcher;

        Axes = new ObservableCollection<ServoAxisViewModel>(CreateAxes(servoService.RegisterMap));
        foreach (var axis in Axes)
        {
            axisLookup[axis.Axis] = axis;
        }

        StatusText = "伺服网关未连接";

        servoService.SnapshotChanged += OnSnapshotChanged;
        servoService.AlarmRaised += OnAlarmRaised;
        ApplySnapshot(servoService.CurrentSnapshot);
        SelectAxis(Axes.FirstOrDefault());
    }

    public ObservableCollection<ServoAxisViewModel> Axes { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyPropertyChangedFor(nameof(ConnectionStatusText))]
    private bool isConnected;

    // 当前选中的轴，右侧大仪表盘只显示这一轴。左侧列表点击切换。
    [ObservableProperty]
    private ServoAxisViewModel? selectedAxis;

    // 手动/自动模式开关。自动模式下不允许单轴手动操作（与 LineControl 解耦，但保留互斥入口）。
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsManualMode))]
    [NotifyPropertyChangedFor(nameof(ManualModeText))]
    private bool isAutoMode;

    public bool IsManualMode => !IsAutoMode;

    public string ManualModeText => IsAutoMode ? "自动" : "手动";

    // 连接状态徽章文字：在线/离线，颜色由 IsConnected 触发器控制
    public string ConnectionStatusText => IsConnected ? "在线" : "离线";

    // 点动模式：开启后正转/反转按钮变成"按住转、松开停"
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(JogModeText))]
    private bool isJogMode = true;

    // 多轴同步：开启后正转/反转/停止作用于全部 4 轴
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SyncAllText))]
    private bool isSyncAll;

    public string JogModeText => IsJogMode ? "点动" : "连续";

    public string SyncAllText => IsSyncAll ? "同步开" : "单轴";

    [ObservableProperty]
    private string statusText = string.Empty;

    [ObservableProperty]
    private string gatewayText = string.Empty;

    private bool CanConnect() => !IsConnected;

    [RelayCommand]
    private void SelectAxis(ServoAxisViewModel? axis)
    {
        if (axis is null)
        {
            return;
        }

        foreach (var item in Axes)
        {
            item.IsSelected = ReferenceEquals(item, axis);
        }

        SelectedAxis = axis;
    }

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        var result = await servoService.ConnectAsync();
        await RunOnUiThreadAsync(() =>
        {
            StatusText = result.IsSuccess ? "已连接 · 手动模式可操作" : result.Message;
        });
    }

    [RelayCommand]
    private async Task ServoOnAsync(ServoAxisViewModel? axis)
    {
        if (!EnsureManualMode(axis, out _))
        {
            return;
        }

        await ExecuteAxisAsync(axis, async p =>
        {
            var r = await servoService.ServoOnAsync(p.Axis);
            return (r, r.IsSuccess ? $"{p.Name} 已使能" : r.Message);
        });
    }

    private bool EnsureManualMode(ServoAxisViewModel? axis, out Task? result)
    {
        result = null;
        if (axis is null)
        {
            return false;
        }

        if (IsAutoMode)
        {
            StatusText = "自动模式下禁止单轴手动操作";
            return false;
        }

        return true;
    }

    private async Task ExecuteAxisAsync(ServoAxisViewModel? axis, Func<ServoAxisViewModel, Task<(ApiResult Result, string Message)>> action)
    {
        if (axis is null)
        {
            return;
        }

        axis.IsBusy = true;
        try
        {
            var (result, message) = await action(axis);
            await RunOnUiThreadAsync(() =>
            {
                StatusText = message;
                if (!result.IsSuccess)
                {
                    return;
                }

                // 命令成功的乐观状态刷新（轮询快照会随后校正）
            });
        }
        finally
        {
            await RunOnUiThreadAsync(() => axis.IsBusy = false);
        }
    }

    [RelayCommand]
    private async Task ServoOffAsync(ServoAxisViewModel? axis)
    {
        if (!EnsureManualMode(axis, out _))
        {
            return;
        }

        await ExecuteAxisAsync(axis, async p =>
        {
            var r = await servoService.ServoOffAsync(p.Axis);
            p.Direction = ServoRunDirection.Stopped;
            return (r, r.IsSuccess ? $"{p.Name} 已停用" : r.Message);
        });
    }

    // 正转：点动模式下为"按下转"，连续模式下为"持续转"。同步模式下作用于全部轴。
    [RelayCommand]
    private async Task ForwardAsync(ServoAxisViewModel? axis)
    {
        if (!EnsureManualMode(axis, out _))
        {
            return;
        }

        if (IsSyncAll)
        {
            await SyncRunAsync(Math.Abs(ParseSpeed(axis)), reverse: false);
            return;
        }

        await ExecuteAxisAsync(axis, async p =>
        {
            var rpm = Math.Abs(ParseSpeed(p));
            var r = IsJogMode
                ? await servoService.BeginJogAsync(p.Axis, rpm)
                : await RunEnabledSpeedAsync(p, rpm);
            if (r.IsSuccess)
            {
                p.Direction = ServoRunDirection.Forward;
            }

            return (r, r.IsSuccess ? $"{p.Name} 正转 {rpm} rpm" : r.Message);
        });
    }

    // 反转
    [RelayCommand]
    private async Task ReverseAsync(ServoAxisViewModel? axis)
    {
        if (!EnsureManualMode(axis, out _))
        {
            return;
        }

        if (IsSyncAll)
        {
            await SyncRunAsync(Math.Abs(ParseSpeed(axis)), reverse: true);
            return;
        }

        await ExecuteAxisAsync(axis, async p =>
        {
            var rpm = Math.Abs(ParseSpeed(p));
            var r = IsJogMode
                ? await servoService.BeginJogAsync(p.Axis, -rpm)
                : await RunEnabledSpeedAsync(p, -rpm);
            if (r.IsSuccess)
            {
                p.Direction = ServoRunDirection.Reverse;
            }

            return (r, r.IsSuccess ? $"{p.Name} 反转 {rpm} rpm" : r.Message);
        });
    }

    // 停止：写 0 转速（不断使能，便于再次启动）。同步模式下停全部轴。
    [RelayCommand]
    private async Task StopAxisAsync(ServoAxisViewModel? axis)
    {
        if (!EnsureManualMode(axis, out _))
        {
            return;
        }

        if (IsSyncAll)
        {
            var r = await servoService.SyncAllAsync(0, reverse: false, stopOnly: true);
            await RunOnUiThreadAsync(() =>
            {
                foreach (var a in Axes)
                {
                    a.Direction = ServoRunDirection.Stopped;
                }

                StatusText = r.Message;
            });
            return;
        }

        await ExecuteAxisAsync(axis, async p =>
        {
            var r = await servoService.SetSpeedAsync(p.Axis, 0);
            if (r.IsSuccess)
            {
                p.Direction = ServoRunDirection.Stopped;
            }

            return (r, r.IsSuccess ? $"{p.Name} 已停止" : r.Message);
        });
    }

    // 点动结束：松开按钮时调用，写 0 停转（仅点动模式有效）
    [RelayCommand]
    private async Task EndJogAsync(ServoAxisViewModel? axis)
    {
        if (!IsJogMode || axis is null)
        {
            return;
        }

        if (IsSyncAll)
        {
            await servoService.SyncAllAsync(0, reverse: false, stopOnly: true);
            await RunOnUiThreadAsync(() =>
            {
                foreach (var a in Axes)
                {
                    a.Direction = ServoRunDirection.Stopped;
                }
            });
            return;
        }

        await ExecuteAxisAsync(axis, async p =>
        {
            var r = await servoService.EndJogAsync(p.Axis);
            if (r.IsSuccess)
            {
                p.Direction = ServoRunDirection.Stopped;
            }

            return (r, r.IsSuccess ? $"{p.Name} 点动停止" : r.Message);
        });
    }

    private async Task<ApiResult> RunEnabledSpeedAsync(ServoAxisViewModel p, int rpm)
    {
        var onResult = await EnsureEnabledAsync(p);
        if (!onResult.IsSuccess)
        {
            return onResult;
        }

        return await servoService.SetSpeedAsync(p.Axis, rpm);
    }

    private async Task SyncRunAsync(int rpm, bool reverse)
    {
        var r = await servoService.SyncAllAsync(rpm, reverse, stopOnly: false);
        await RunOnUiThreadAsync(() =>
        {
            var dir = reverse ? ServoRunDirection.Reverse : ServoRunDirection.Forward;
            foreach (var a in Axes)
            {
                a.Direction = r.IsSuccess ? dir : ServoRunDirection.Stopped;
            }

            StatusText = r.Message;
        });
    }

    private async Task<ApiResult> EnsureEnabledAsync(ServoAxisViewModel axis)
    {
        if (axis.IsEnabled)
        {
            return ApiResult.Ok();
        }

        var on = await servoService.ServoOnAsync(axis.Axis);
        return on;
    }

    private static int ParseSpeed(ServoAxisViewModel? axis)
    {
        return axis is not null && int.TryParse(axis.SpeedInputText, out var rpm) ? rpm : 0;
    }

    [RelayCommand]
    private async Task IncreaseSpeedAsync(ServoAxisViewModel? axis)
    {
        await AdjustSpeedAsync(axis, SpeedStepRpm);
    }

    [RelayCommand]
    private async Task DecreaseSpeedAsync(ServoAxisViewModel? axis)
    {
        await AdjustSpeedAsync(axis, -SpeedStepRpm);
    }

    [RelayCommand]
    private async Task ApplySpeedAsync(ServoAxisViewModel? axis)
    {
        if (!EnsureManualMode(axis, out _))
        {
            return;
        }

        await ApplySpeedToRunningAxisAsync(axis!, sanitizeInput: true);
    }

    private async Task AdjustSpeedAsync(ServoAxisViewModel? axis, int deltaRpm)
    {
        if (!EnsureManualMode(axis, out _))
        {
            return;
        }

        var current = Math.Abs(ParseSpeed(axis));
        var next = Math.Clamp(current + deltaRpm, 0, UiMaxSpeedRpm);
        axis!.SpeedInputText = next.ToString();

        await ApplySpeedToRunningAxisAsync(axis, sanitizeInput: false);
    }

    private async Task ApplySpeedToRunningAxisAsync(ServoAxisViewModel axis, bool sanitizeInput)
    {
        var rpm = Math.Clamp(Math.Abs(ParseSpeed(axis)), 0, UiMaxSpeedRpm);
        if (sanitizeInput)
        {
            axis.SpeedInputText = rpm.ToString();
        }

        if (axis.Direction == ServoRunDirection.Stopped)
        {
            StatusText = $"{axis.Name} 速度设定 {rpm} rpm";
            return;
        }

        var signedRpm = axis.Direction == ServoRunDirection.Reverse ? -rpm : rpm;
        if (IsSyncAll)
        {
            var sync = await servoService.SyncAllAsync(rpm, axis.Direction == ServoRunDirection.Reverse, stopOnly: rpm == 0);
            await RunOnUiThreadAsync(() =>
            {
                foreach (var item in Axes)
                {
                    item.SpeedInputText = rpm.ToString();
                    if (rpm == 0)
                    {
                        item.Direction = ServoRunDirection.Stopped;
                    }
                }

                StatusText = sync.IsSuccess ? $"同步调速 {rpm} rpm" : sync.Message;
            });
            return;
        }

        var result = await servoService.SetSpeedAsync(axis.Axis, signedRpm);
        await RunOnUiThreadAsync(() =>
        {
            if (result.IsSuccess)
            {
                axis.Direction = rpm == 0 ? ServoRunDirection.Stopped : axis.Direction;
                StatusText = rpm == 0 ? $"{axis.Name} 已停止" : $"{axis.Name} 调速 {rpm} rpm";
            }
            else
            {
                StatusText = result.Message;
            }
        });
    }

    [RelayCommand]
    private async Task EmergencyStopAsync()
    {
        var result = await servoService.EmergencyStopAllAsync();
        await RunOnUiThreadAsync(() =>
        {
            foreach (var axis in Axes)
            {
                axis.Direction = ServoRunDirection.Stopped;
            }

            StatusText = result.Message;
        });
    }

    [RelayCommand]
    private async Task ResetFaultAsync(ServoAxisViewModel? axis)
    {
        if (axis is null)
        {
            return;
        }

        var result = await servoService.ResetFaultAsync(axis.Axis);
        await RunOnUiThreadAsync(() => StatusText = result.Message);
    }

    [RelayCommand]
    private void ToggleAutoMode()
    {
        if (!IsConnected && !IsAutoMode)
        {
            StatusText = "未连接，暂不可切换模式";
            return;
        }

        IsAutoMode = !IsAutoMode;
        StatusText = IsAutoMode ? "已切换自动模式（单轴手动已锁定）" : "已切换手动模式";
    }

    [RelayCommand]
    private void ToggleJogMode()
    {
        IsJogMode = !IsJogMode;
        StatusText = IsJogMode ? "已切换点动：按住转，松开停" : "已切换连续：点击转，需手动停止";
    }

    [RelayCommand]
    private void ToggleSyncAll()
    {
        IsSyncAll = !IsSyncAll;
        StatusText = IsSyncAll ? "已切换同步：方向和停止作用于全部轴" : "已切换单轴：只操作选中轴";
    }

    private void OnSnapshotChanged(object? sender, ServoSnapshot snapshot)
    {
        _ = RunOnUiThreadAsync(() => ApplySnapshot(snapshot));
    }

    private void OnAlarmRaised(object? sender, ServoAlarm alarm)
    {
        _ = RunOnUiThreadAsync(() => StatusText = alarm.Message);
    }

    private void ApplySnapshot(ServoSnapshot snapshot)
    {
        IsConnected = snapshot.IsConnected;
        GatewayText = string.IsNullOrWhiteSpace(snapshot.GatewayIp) ? "--" : $"{snapshot.GatewayIp}";
        if (!string.IsNullOrWhiteSpace(snapshot.LastError))
        {
            StatusText = snapshot.LastError;
        }
        else if (snapshot.IsConnected && string.IsNullOrWhiteSpace(snapshot.LastError))
        {
            // 不覆盖命令返回的操作提示
        }

        foreach (var state in snapshot.Axes)
        {
            if (axisLookup.TryGetValue(state.Axis, out var axis))
            {
                axis.ApplyState(state.IsEnabled, state.IsOnline, state.TargetRpm, state.ActualRpm, state.FaultCode);
            }
        }
    }

    private Task RunOnUiThreadAsync(Action action)
    {
        if (dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
        {
            return Task.CompletedTask;
        }

        return dispatcher.InvokeAsync(action).Task;
    }

    private static IEnumerable<ServoAxisViewModel> CreateAxes(ServoRegisterMap registerMap)
    {
        foreach (var definition in registerMap.Axes)
        {
            yield return new ServoAxisViewModel(definition.Axis, definition.Name, definition.Station);
        }
    }
}
