using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PipelineControl.UI.Services.Io;
using PipelineControl.UI.Services.Io.Mapping;
using PipelineControl.UI.Services.Line;
using PipelineControl.UI.Services.Logs;
using PipelineControl.UI.Services.Settings;
using PipelineControl.UI.ViewModels.Pages.IoMonitor.Models;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace PipelineControl.UI.ViewModels.Pages.IoMonitor;

public partial class OutputTestViewModel : ObservableObject
{
    private const int HeadFoolproofInputPointNo = 9;
    private const int HeadCylinderOutputPointNo = 25;
    private const string HeadCylinderGroupKey = "HeadCylinder";

    private readonly IoBoardService ioBoardService;
    private readonly LineControlService lineControlService;
    private readonly Dispatcher dispatcher;
    private readonly Dictionary<(IoType Type, int PointNo), IoPointViewModel> pointLookup = new();
    private readonly Dictionary<int, OutputActionGroupViewModel> outputActionPointLookup = new();
    private readonly Dictionary<string, OutputActionGroupViewModel> outputActionLookup = new(StringComparer.Ordinal);
    private readonly HashSet<int> touchedOutputs = new();

    public OutputTestViewModel()
        : this(CreateDefaultService())
    {
    }

    public OutputTestViewModel(IoBoardService ioBoardService)
        : this(ioBoardService, new LineControlService(ioBoardService, new JsonAppLogService()))
    {
    }

    public OutputTestViewModel(IoBoardService ioBoardService, LineControlService lineControlService)
    {
        this.ioBoardService = ioBoardService;
        this.lineControlService = lineControlService;
        var appDispatcher = System.Windows.Application.Current?.Dispatcher;
        dispatcher = appDispatcher is { HasShutdownStarted: false, HasShutdownFinished: false }
                     && appDispatcher.CheckAccess()
            ? appDispatcher
            : Dispatcher.CurrentDispatcher;

        InputModules = new ObservableCollection<IoModuleViewModel>(CreateModules(ioBoardService.Inputs, isOutput: false));
        OutputModules = new ObservableCollection<IoModuleViewModel>(CreateModules(ioBoardService.Outputs, isOutput: true));
        OutputActionGroups = new ObservableCollection<OutputActionGroupViewModel>(CreateOutputActionGroups());
        StatusText = "板卡未连接";
        SelectedPointText = "未选择";

        ioBoardService.SnapshotChanged += OnSnapshotChanged;
        ioBoardService.AlarmRaised += OnAlarmRaised;
        lineControlService.StateChanged += OnLineControlStateChanged;
        ApplySnapshot(ioBoardService.CurrentSnapshot);
        ApplyLineState(lineControlService.State);
    }

    public ObservableCollection<IoModuleViewModel> InputModules { get; }

    public ObservableCollection<IoModuleViewModel> OutputModules { get; }

    public ObservableCollection<OutputActionGroupViewModel> OutputActionGroups { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetAllOutputsCommand))]
    private bool isConnected;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ResetAllOutputsCommand))]
    private bool isManualMode;

    partial void OnIsManualModeChanged(bool value)
    {
        if (value && IsAutomaticBlockingManual)
        {
            IsManualMode = false;
            StatusText = CreateAutomaticBlockMessage();
            return;
        }

        StatusText = value
            ? "手动模式已开启 · 点动按钮需按住输出，松开断开"
            : "手动模式已关闭";
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ResetAllOutputsCommand))]
    private LineRunState lineState;

    [ObservableProperty]
    private string statusText = string.Empty;

    [ObservableProperty]
    private string selectedPointText = string.Empty;

    [ObservableProperty]
    private int outputOnCount;

    [ObservableProperty]
    private int outputOffCount;

    [ObservableProperty]
    private int inputOnCount;

    [ObservableProperty]
    private int inputOffCount;

    [ObservableProperty]
    private int manualOperationCount;

    private bool CanConnect() => !IsConnected;

    private bool IsAutomaticBlockingManual => LineState != LineRunState.Idle;

    private bool CanResetAllOutputs() => ManualOperationCount > 0 && !IsAutomaticBlockingManual;

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        var result = await ioBoardService.ConnectAsync();
        if (result.IsSuccess)
        {
            await ioBoardService.StartPollingAsync();
        }

        await RunOnUiThreadAsync(() =>
        {
            StatusText = result.IsSuccess ? "已连接 · 手动模式可操作 Y 点" : result.Message;
        });
    }

    [RelayCommand(CanExecute = nameof(CanResetAllOutputs))]
    private async Task ResetAllOutputsAsync()
    {
        if (IsAutomaticBlockingManual)
        {
            StatusText = CreateAutomaticBlockMessage();
            await Task.CompletedTask;
            return;
        }

        var resetCount = ManualOperationCount;
        if (!IsConnected)
        {
            ResetLocalOutputs(resetCount, "本地复位");
            await Task.CompletedTask;
            return;
        }

        var result = await ioBoardService.ResetForcedOutputsAsync();
        await RunOnUiThreadAsync(() =>
        {
            StatusText = result.IsSuccess ? $"复位完成 · 清除 {resetCount} 点" : result.Message;
            if (result.IsSuccess)
            {
                ClearTouchedOutputs();
                StatusText = $"复位完成 · 清除 {resetCount} 点";
                ResetAllOutputsCommand.NotifyCanExecuteChanged();
            }
        });
    }

    [RelayCommand]
    private async Task SelectOutputPointAsync(IoPointViewModel? point)
    {
        if (point is null)
        {
            return;
        }

        SelectedPointText = $"{point.QualifiedLabel} · {point.Description}";
        if (IsAutomaticBlockingManual)
        {
            ClearOutputSelection();
            SelectedPointText = $"{point.QualifiedLabel} 未输出";
            StatusText = CreateAutomaticBlockMessage();
            return;
        }

        if (!IsManualMode)
        {
            ClearOutputSelection();
            SelectedPointText = $"{point.QualifiedLabel} 未输出";
            StatusText = "手动模式未开启";
            return;
        }

        if (!point.IsEnabled)
        {
            ClearOutputSelection();
            SelectedPointText = $"{point.QualifiedLabel} 未输出";
            StatusText = $"{point.QualifiedLabel} 未启用";
            return;
        }

        if (outputActionPointLookup.TryGetValue(point.PointNo, out var actionGroup))
        {
            ClearOutputSelection();
            SelectedPointText = $"{point.QualifiedLabel} 属于 {actionGroup.DisplayName}";
            StatusText = $"请使用“{actionGroup.DisplayName}”动作按钮";
            return;
        }

        if (!IsConnected)
        {
            ToggleLocalOutput(point);
            await Task.CompletedTask;
            return;
        }

        var target = !point.IsOn;
        var result = await ioBoardService.WriteOutputAsync(point.PointNo, target);
        await RunOnUiThreadAsync(() =>
        {
            if (!result.IsSuccess)
            {
                ClearOutputSelection();
                SelectedPointText = $"{point.QualifiedLabel} 写入失败";
                StatusText = result.Message;
                return;
            }

            SelectOutputPoint(point);
            if (target)
            {
                touchedOutputs.Add(point.PointNo);
            }
            else
            {
                touchedOutputs.Remove(point.PointNo);
            }

            ManualOperationCount = touchedOutputs.Count;
            StatusText = $"{point.QualifiedLabel} {(target ? "ON" : "OFF")} · 手动 {ManualOperationCount}";
            SelectedPointText = $"{point.QualifiedLabel} {(target ? "输出" : "关闭")}";
            ResetAllOutputsCommand.NotifyCanExecuteChanged();
        });
    }

    [RelayCommand]
    private async Task ToggleOutputActionGroupAsync(OutputActionGroupViewModel? group)
    {
        if (group is null)
        {
            return;
        }

        await SetOutputActionGroupAsync(group, !group.IsOn, "手动");
    }

    [RelayCommand]
    private async Task BeginOutputActionGroupAsync(OutputActionGroupViewModel? group)
    {
        if (group is null || group.IsOn)
        {
            return;
        }

        await SetOutputActionGroupAsync(group, true, "点动");
    }

    [RelayCommand]
    private async Task EndOutputActionGroupAsync(OutputActionGroupViewModel? group)
    {
        if (group is null || !group.IsOn)
        {
            return;
        }

        await SetOutputActionGroupAsync(group, false, "点动");
    }

    private async Task SetOutputActionGroupAsync(
        OutputActionGroupViewModel group,
        bool target,
        string modeText,
        string? successStatusText = null,
        string? successSelectedPointText = null)
    {
        if (group is null)
        {
            return;
        }

        if (IsAutomaticBlockingManual)
        {
            ClearOutputSelection();
            StatusText = CreateAutomaticBlockMessage();
            SelectedPointText = $"{group.DisplayName} 未输出";
            return;
        }

        if (target && !IsManualMode)
        {
            ClearOutputSelection();
            StatusText = "手动模式未开启";
            SelectedPointText = $"{group.DisplayName} 未输出";
            return;
        }

        if (target && TryFindActiveConflict(group, out var conflict))
        {
            StatusText = $"{conflict.DisplayName} 已输出，请先复位当前方向";
            SelectedPointText = $"{group.DisplayName} 互锁禁止";
            return;
        }

        if (target && IsHeadCylinderBlockedByFoolproof(group))
        {
            ClearOutputSelection();
            StatusText = "线头防呆光电已感应，禁止线头气缸伸出";
            SelectedPointText = $"{group.DisplayName} 防呆禁止";
            return;
        }

        if (!IsConnected)
        {
            ApplyLocalActionGroup(group, target, modeText);
            await Task.CompletedTask;
            return;
        }

        var values = group.PointNos.ToDictionary(pointNo => pointNo, _ => target);
        var result = await ioBoardService.WriteOutputsAsync(values);
        await RunOnUiThreadAsync(() =>
        {
            if (!result.IsSuccess)
            {
                ClearOutputSelection();
                StatusText = result.Message;
                SelectedPointText = $"{group.DisplayName} 写入失败";
                return;
            }

            ApplyActionGroupState(group, target);
            StatusText = successStatusText ?? $"{group.DisplayName} {(target ? "ON" : "OFF")} · {modeText} {ManualOperationCount}";
            SelectedPointText = successSelectedPointText ?? $"{group.DisplayName} {(target ? "输出" : "关闭")}";
        });
    }

    private void OnSnapshotChanged(object? sender, IoBoardSnapshot snapshot)
    {
        _ = HandleSnapshotChangedAsync(snapshot);
    }

    private void OnAlarmRaised(object? sender, IoBoardAlarm alarm)
    {
        _ = RunOnUiThreadAsync(() => StatusText = alarm.Message);
    }

    private void OnLineControlStateChanged(object? sender, LineStateChangedEventArgs e)
    {
        _ = RunOnUiThreadAsync(() => ApplyLineState(e.State, e.Message));
    }

    private void ApplyLineState(LineRunState state, string? message = null)
    {
        LineState = state;
        if (IsAutomaticBlockingManual)
        {
            if (IsManualMode)
            {
                IsManualMode = false;
            }

            StatusText = LineState == LineRunState.Fault && !string.IsNullOrWhiteSpace(message)
                ? message
                : CreateAutomaticBlockMessage();
        }
        else if (!string.IsNullOrWhiteSpace(message))
        {
            StatusText = message;
        }

        ResetAllOutputsCommand.NotifyCanExecuteChanged();
    }

    private void ApplySnapshot(IoBoardSnapshot snapshot)
    {
        IsConnected = snapshot.IsConnected;
        if (!string.IsNullOrWhiteSpace(snapshot.LastError))
        {
            StatusText = snapshot.LastError;
        }

        foreach (var pointState in snapshot.Inputs)
        {
            if (pointLookup.TryGetValue((pointState.Definition.IoType, pointState.Definition.PointNo), out var point))
            {
                point.IsOn = pointState.IsOn;
                point.IsForced = false;
            }
        }

        foreach (var pointState in snapshot.Outputs)
        {
            if (pointLookup.TryGetValue((pointState.Definition.IoType, pointState.Definition.PointNo), out var point))
            {
                point.IsOn = pointState.IsOn;
                point.IsForced = pointState.IsForced;
            }
        }

        foreach (var module in InputModules)
        {
            module.RefreshRegisterValue();
        }

        foreach (var module in OutputModules)
        {
            module.RefreshRegisterValue();
        }

        RefreshOutputActionGroups();
        RecalculateCounts();
    }

    private async Task HandleSnapshotChangedAsync(IoBoardSnapshot snapshot)
    {
        await RunOnUiThreadAsync(() => ApplySnapshot(snapshot));
        await EnforceHeadCylinderFoolproofAsync();
    }

    private void RecalculateCounts()
    {
        var inputs = InputModules.SelectMany(module => module.Points).ToArray();
        var outputs = OutputModules.SelectMany(module => module.Points).ToArray();
        InputOnCount = inputs.Count(point => point.IsOn);
        InputOffCount = inputs.Length - InputOnCount;
        OutputOnCount = outputs.Count(point => point.IsOn);
        OutputOffCount = outputs.Length - OutputOnCount;
        ManualOperationCount = touchedOutputs.Count;
        ResetAllOutputsCommand.NotifyCanExecuteChanged();
    }

    private void ToggleLocalOutput(IoPointViewModel point)
    {
        var target = !point.IsOn;
        point.IsOn = target;
        point.IsForced = target;
        SelectOutputPoint(point);

        if (target)
        {
            touchedOutputs.Add(point.PointNo);
        }
        else
        {
            touchedOutputs.Remove(point.PointNo);
        }

        foreach (var module in OutputModules)
        {
            module.RefreshRegisterValue();
        }

        RefreshOutputActionGroups();
        RecalculateCounts();
        StatusText = $"{point.QualifiedLabel} {(target ? "ON" : "OFF")} · 手动 {ManualOperationCount}";
        SelectedPointText = $"{point.QualifiedLabel} {(target ? "输出" : "关闭")}";
    }

    private void ApplyLocalActionGroup(OutputActionGroupViewModel group, bool target, string source)
    {
        ApplyActionGroupState(group, target);
        StatusText = $"{source} {group.DisplayName} {(target ? "ON" : "OFF")} · 手动 {ManualOperationCount}";
        SelectedPointText = $"{group.DisplayName} {(target ? "输出" : "关闭")}";
    }

    private void ApplyActionGroupState(OutputActionGroupViewModel group, bool target)
    {
        foreach (var pointNo in group.PointNos)
        {
            if (!pointLookup.TryGetValue((IoType.Output, pointNo), out var point))
            {
                continue;
            }

            point.IsOn = target;
            point.IsForced = target;
            if (target)
            {
                touchedOutputs.Add(pointNo);
            }
            else
            {
                touchedOutputs.Remove(pointNo);
            }
        }

        SelectActionGroup(group);
        foreach (var module in OutputModules)
        {
            module.RefreshRegisterValue();
        }

        RefreshOutputActionGroups();
        RecalculateCounts();
    }

    private void ResetLocalOutputs(int resetCount, string source)
    {
        ClearTouchedOutputs();
        StatusText = $"{source}完成 · 清除 {resetCount} 点";
        ResetAllOutputsCommand.NotifyCanExecuteChanged();
    }

    private void ClearTouchedOutputs()
    {
        foreach (var pointNo in touchedOutputs.ToArray())
        {
            if (pointLookup.TryGetValue((IoType.Output, pointNo), out var point))
            {
                point.IsOn = false;
                point.IsForced = false;
            }
        }

        touchedOutputs.Clear();
        ManualOperationCount = 0;
        ClearOutputSelection();
        foreach (var module in OutputModules)
        {
            module.RefreshRegisterValue();
        }

        RefreshOutputActionGroups();
        RecalculateCounts();
    }

    private void SelectOutputPoint(IoPointViewModel selectedPoint)
    {
        foreach (var current in OutputModules.SelectMany(module => module.Points))
        {
            current.IsSelected = ReferenceEquals(current, selectedPoint);
        }
    }

    private void ClearOutputSelection()
    {
        foreach (var current in OutputModules.SelectMany(module => module.Points))
        {
            current.IsSelected = false;
        }
    }

    private void SelectActionGroup(OutputActionGroupViewModel group)
    {
        foreach (var current in OutputModules.SelectMany(module => module.Points))
        {
            current.IsSelected = group.PointNos.Contains(current.PointNo);
        }
    }

    private bool TryFindActiveConflict(OutputActionGroupViewModel group, out OutputActionGroupViewModel conflict)
    {
        foreach (var conflictKey in group.ConflictKeys)
        {
            if (outputActionLookup.TryGetValue(conflictKey, out conflict!) && conflict.IsOn)
            {
                return true;
            }
        }

        conflict = null!;
        return false;
    }

    private bool IsHeadCylinderBlockedByFoolproof(OutputActionGroupViewModel group)
    {
        return string.Equals(group.Key, HeadCylinderGroupKey, StringComparison.Ordinal) &&
            pointLookup.TryGetValue((IoType.Input, HeadFoolproofInputPointNo), out var point) &&
            point.IsOn;
    }

    private async Task EnforceHeadCylinderFoolproofAsync()
    {
        if (!outputActionLookup.TryGetValue(HeadCylinderGroupKey, out var group) ||
            !group.IsOn ||
            !IsHeadCylinderBlockedByFoolproof(group))
        {
            return;
        }

        await SetOutputActionGroupAsync(
            group,
            false,
            "防呆",
            "线头防呆光电已感应，线头气缸伸出已关闭",
            $"{group.DisplayName} 防呆关闭");
    }

    private void RefreshOutputActionGroups()
    {
        foreach (var group in OutputActionGroups)
        {
            group.IsOn = group.PointNos.All(pointNo =>
                pointLookup.TryGetValue((IoType.Output, pointNo), out var point) && point.IsOn);
        }
    }

    private IEnumerable<OutputActionGroupViewModel> CreateOutputActionGroups()
    {
        var groups = new[]
        {
            new OutputActionGroupViewModel("HeadUp", "线头电缸上升", new[] { 17, 18 }, new[] { "HeadDown" }),
            new OutputActionGroupViewModel("HeadDown", "线头电缸下降", new[] { 19, 20 }, new[] { "HeadUp" }),
            new OutputActionGroupViewModel("TailUp", "线尾电缸上升", new[] { 21, 22 }, new[] { "TailDown" }),
            new OutputActionGroupViewModel("TailDown", "线尾电缸下降", new[] { 23, 24 }, new[] { "TailUp" }),
            new OutputActionGroupViewModel(HeadCylinderGroupKey, "线头气缸伸出", new[] { HeadCylinderOutputPointNo }),
            new OutputActionGroupViewModel("TailCylinder", "线尾气缸伸出", new[] { 26 })
        };

        foreach (var group in groups)
        {
            outputActionLookup[group.Key] = group;
            foreach (var pointNo in group.PointNos)
            {
                outputActionPointLookup[pointNo] = group;
            }
        }

        return groups;
    }

    private IEnumerable<IoModuleViewModel> CreateModules(IEnumerable<IoPointDefinition> definitions, bool isOutput)
    {
        foreach (var group in definitions.GroupBy(point => GetPhysicalCardIndex(point.ModuleIndex)).OrderBy(group => group.Key))
        {
            var points = group
                .OrderBy(point => GetLocalPointIndex(point.ModuleIndex, point.BitIndex))
                .Select(CreatePoint)
                .ToList();
            var hasSignalNames = !isOutput && points.Any(point => point.HasSignalName);
            var columns = hasSignalNames
                ? 4
                : (points.Count == 16 ? 4 : 6);
            var cellWidth = hasSignalNames ? 116D : 50D;
            var cellHeight = hasSignalNames ? 44D : 32D;
            yield return new IoModuleViewModel(CreateModuleLabel(group.Key, points, isOutput), columns, isOutput, points, cellWidth, cellHeight);
        }
    }

    private IoPointViewModel CreatePoint(IoPointDefinition definition)
    {
        var moduleName = CreateModuleName(GetPhysicalCardIndex(definition.ModuleIndex));
        var localIndex = GetLocalPointIndex(definition.ModuleIndex, definition.BitIndex);
        var prefix = definition.IoType == IoType.Output ? "Y" : "X";
        var defaultLabel = $"{prefix}{localIndex}";
        var point = new IoPointViewModel
        {
            PointNo = definition.PointNo,
            ModuleIndex = definition.ModuleIndex,
            BitIndex = definition.BitIndex,
            LocalIndex = localIndex,
            ModuleName = moduleName,
            GlobalLabel = definition.GlobalLabel,
            DisplayLabel = defaultLabel,
            SignalName = string.Equals(definition.Name, defaultLabel, StringComparison.Ordinal)
                ? string.Empty
                : definition.Name,
            TagName = definition.TagAddress,
            Description = definition.Description,
            IsOutput = definition.IoType == IoType.Output,
            IsEnabled = definition.IsEnabled,
            SafeDefaultValue = definition.SafeDefaultValue,
            ShowBitText = true,
            IsOn = false
        };
        pointLookup[(definition.IoType, definition.PointNo)] = point;
        return point;
    }

    private static string CreateModuleLabel(int moduleIndex, IReadOnlyList<IoPointViewModel> points, bool isOutput)
    {
        var prefix = isOutput ? "Y" : "X";
        var first = points.Count == 0 ? $"{prefix}?" : points.First().DisplayLabel;
        var last = points.Count == 0 ? $"{prefix}?" : points.Last().DisplayLabel;
        return $"{CreateModuleName(moduleIndex)} · {first}-{last} · {points.Count}点";
    }

    private static string CreateModuleName(int moduleIndex)
    {
        return moduleIndex switch
        {
            0 => "IO控制卡",
            1 => "扩展卡1",
            2 => "扩展卡2",
            _ => $"扩展卡{moduleIndex}"
        };
    }

    private static int GetPhysicalCardIndex(int moduleIndex)
    {
        return moduleIndex switch
        {
            0 => 0,
            1 or 2 => 1,
            3 or 4 => 2,
            _ => moduleIndex
        };
    }

    private static int GetLocalPointIndex(int moduleIndex, int bitIndex)
    {
        return moduleIndex switch
        {
            0 or 1 or 3 => bitIndex,
            2 or 4 => 16 + bitIndex,
            _ => bitIndex
        };
    }

    private Task RunOnUiThreadAsync(Action action)
    {
        var appDispatcher = System.Windows.Application.Current?.Dispatcher;
        if (appDispatcher is null || !ReferenceEquals(dispatcher, appDispatcher))
        {
            action();
            return Task.CompletedTask;
        }

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

    private static IoBoardService CreateDefaultService()
    {
        return new IoBoardService(
            new IoBoardDriverFactory(),
            new JsonIoPointMapProvider(),
            new JsonSettingsService());
    }

    private string CreateAutomaticBlockMessage()
    {
        return LineState == LineRunState.Fault
            ? "自动异常中，禁止手动输出"
            : "自动运行中，禁止手动输出";
    }
}
