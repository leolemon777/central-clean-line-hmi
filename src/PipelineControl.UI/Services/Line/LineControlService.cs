using PipelineControl.UI.Services.Io;
using PipelineControl.UI.Services.Logs;

namespace PipelineControl.UI.Services.Line;

public sealed class LineControlService : IDisposable
{
    private const int HeadFirstStationPhotoPointNo = 1;
    private const int HeadTravelPointNo = 2;
    private const int HeadLowerLimitPointNo = 3;
    private const int HeadUpperLimitPointNo = 4;
    private const int HeadFoolproofPointNo = 9;
    private const int TailTravelPointNo = 6;
    private const int TailLowerLimitPointNo = 7;
    private const int TailUpperLimitPointNo = 8;

    private static readonly int[] HeadUpOutputs = { 17, 18 };
    private static readonly int[] HeadDownOutputs = { 19, 20 };
    private static readonly int[] TailUpOutputs = { 21, 22 };
    private static readonly int[] TailDownOutputs = { 23, 24 };
    private const int HeadCylinderOutput = 25;
    private const int TailCylinderOutput = 26;
    private const int MainCardHeadLowerNoTravelOutputY0 = 1;
    private const int MainCardHeadLowerNoTravelOutputY1 = 2;
    private static readonly int[] MainCardAlwaysOnOutputs = { 3, 4, 5, 6, 7, 8, 9, 10 };
    private static readonly int[] AutoOutputPointNos =
    {
        1, 2,
        3, 4, 5, 6, 7, 8, 9, 10,
        17, 18, 19, 20, 21, 22, 23, 24, 25, 26
    };

    private readonly IoBoardService ioBoardService;
    private readonly IAppLogService appLogService;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly StationRuntime headRuntime = new("线头");
    private readonly StationRuntime tailRuntime = new("线尾");
    private readonly TravelOffDelayRuntime headTravelOffDelay = new();
    private readonly TravelOffDelayRuntime tailTravelOffDelay = new();
    private readonly CylinderPulseRuntime headCylinderPulse = new();
    private readonly CylinderPulseRuntime tailCylinderPulse = new();
    private CancellationTokenSource? runCts;
    private Task? runTask;
    private Dictionary<int, bool>? lastAutoOutputTargets;
    private bool disposed;

    public LineControlService(IoBoardService ioBoardService, IAppLogService appLogService)
    {
        this.ioBoardService = ioBoardService;
        this.appLogService = appLogService;
    }

    public event EventHandler<LineStateChangedEventArgs>? StateChanged;

    public LineRunState State { get; private set; } = LineRunState.Idle;

    public string StatusMessage { get; private set; } = "待机";

    public TimeSpan ActionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan TravelOffDelay { get; set; } = TimeSpan.FromSeconds(3);

    public TimeSpan CylinderPulseDuration { get; set; } = TimeSpan.FromSeconds(3);

    public TimeSpan LoopPeriod { get; set; } = TimeSpan.FromMilliseconds(100);

    public async Task<ApiResult> StartAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (State == LineRunState.Running)
            {
                return ApiResult.Ok(StatusMessage);
            }

            if (State == LineRunState.Fault)
            {
                StatusMessage = "自动异常，请先按自动关闭";
                PublishState();
                return new ApiResult(1, StatusMessage);
            }

            if (!ioBoardService.CurrentSnapshot.IsConnected)
            {
                StatusMessage = "请先连接板卡";
                await WriteOperationLogAsync("AUTO_START_BLOCKED", StatusMessage, LogLevelKind.Warn, cancellationToken)
                    .ConfigureAwait(false);
                PublishState();
                return new ApiResult(-1, StatusMessage);
            }

            if (ioBoardService.HasManualForcedOutputs)
            {
                StatusMessage = "请先复位手动输出";
                await WriteOperationLogAsync("AUTO_START_BLOCKED", StatusMessage, LogLevelKind.Warn, cancellationToken)
                    .ConfigureAwait(false);
                PublishState();
                return new ApiResult(1, StatusMessage);
            }

            await ioBoardService.StartPollingAsync(cancellationToken).ConfigureAwait(false);

            headRuntime.Reset();
            tailRuntime.Reset();
            headTravelOffDelay.Reset();
            tailTravelOffDelay.Reset();
            headCylinderPulse.Reset();
            tailCylinderPulse.Reset();
            lastAutoOutputTargets = null;
            runCts?.Dispose();
            runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var runToken = runCts.Token;
            State = LineRunState.Running;
            StatusMessage = "自动运行中";

            await WriteOperationLogAsync("AUTO_START", "自动开启", LogLevelKind.Info, cancellationToken)
                .ConfigureAwait(false);
            PublishState();

            runTask = Task.Run(() => RunAutoLoopAsync(runToken), CancellationToken.None);
            return ApiResult.Ok(StatusMessage);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<ApiResult> StopAsync(CancellationToken cancellationToken = default)
    {
        CancellationTokenSource? cts;
        Task? task;

        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cts = runCts;
            task = runTask;
            runCts = null;
            runTask = null;
            cts?.Cancel();
        }
        finally
        {
            gate.Release();
        }

        if (task is not null)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        cts?.Dispose();

        var stopResult = await WriteAllAutoOutputsOffAsync(cancellationToken).ConfigureAwait(false);

        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            State = LineRunState.Idle;
            StatusMessage = stopResult.IsSuccess
                ? "待机"
                : $"待机 · 自动输出关闭失败：{stopResult.Message}";
            headRuntime.Reset();
            tailRuntime.Reset();
            headTravelOffDelay.Reset();
            tailTravelOffDelay.Reset();
            headCylinderPulse.Reset();
            tailCylinderPulse.Reset();
            lastAutoOutputTargets = null;

            await WriteOperationLogAsync("AUTO_STOP", "自动关闭", LogLevelKind.Info, cancellationToken)
                .ConfigureAwait(false);
            PublishState();
            return stopResult.IsSuccess ? ApiResult.Ok(StatusMessage) : stopResult;
        }
        finally
        {
            gate.Release();
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        runCts?.Cancel();
        runCts?.Dispose();
        gate.Dispose();
        disposed = true;
    }

    private async Task RunAutoLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var snapshot = ioBoardService.CurrentSnapshot;
                if (!snapshot.IsConnected)
                {
                    await EnterFaultAsync("IO 通讯断开", CancellationToken.None).ConfigureAwait(false);
                    return;
                }

                var now = DateTimeOffset.Now;
                var head = DecideHead(snapshot, now);
                var tail = DecideTail(snapshot, now);
                if (!string.IsNullOrWhiteSpace(head.FaultMessage))
                {
                    await EnterFaultAsync(head.FaultMessage, CancellationToken.None).ConfigureAwait(false);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(tail.FaultMessage))
                {
                    await EnterFaultAsync(tail.FaultMessage, CancellationToken.None).ConfigureAwait(false);
                    return;
                }

                var headUpdate = headRuntime.Update(head.Action, now, ActionTimeout);
                if (!string.IsNullOrWhiteSpace(headUpdate.TimeoutMessage))
                {
                    await EnterFaultAsync(headUpdate.TimeoutMessage, CancellationToken.None).ConfigureAwait(false);
                    return;
                }

                var tailUpdate = tailRuntime.Update(tail.Action, now, ActionTimeout);
                if (!string.IsNullOrWhiteSpace(tailUpdate.TimeoutMessage))
                {
                    await EnterFaultAsync(tailUpdate.TimeoutMessage, CancellationToken.None).ConfigureAwait(false);
                    return;
                }

                var mainCardResult = await ApplyAutoOutputsAsync(CreateMainCardOutputTargets(snapshot), cancellationToken)
                    .ConfigureAwait(false);
                if (!mainCardResult.IsSuccess)
                {
                    await EnterFaultAsync($"本体自动输出写入失败：{mainCardResult.Message}", CancellationToken.None)
                        .ConfigureAwait(false);
                    return;
                }

                var liftTargets = CreateExtensionCardOutputTargets(
                    head.Action,
                    tail.Action,
                    snapshot,
                    now,
                    headUpdate.CompletedAction == LiftAction.Up,
                    tailUpdate.CompletedAction == LiftAction.Down);
                var liftResult = await ApplyAutoOutputsAsync(liftTargets, cancellationToken).ConfigureAwait(false);
                if (!liftResult.IsSuccess)
                {
                    await EnterFaultAsync($"自动输出写入失败：{liftResult.Message}", CancellationToken.None)
                        .ConfigureAwait(false);
                    return;
                }

                PublishRunningStatus($"{head.StatusText} / {tail.StatusText}");
                await Task.Delay(LoopPeriod, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await EnterFaultAsync($"自动流程异常：{ex.Message}", CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task EnterFaultAsync(string reason, CancellationToken cancellationToken)
    {
        var stopResult = await WriteAllAutoOutputsOffAsync(cancellationToken).ConfigureAwait(false);

        await gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            if (State == LineRunState.Idle)
            {
                return;
            }

            State = LineRunState.Fault;
            StatusMessage = stopResult.IsSuccess
                ? $"自动异常 · {reason}"
                : $"自动异常 · {reason} · 输出关闭失败：{stopResult.Message}";
            await WriteOperationLogAsync("AUTO_FAULT", StatusMessage, LogLevelKind.Error, CancellationToken.None)
                .ConfigureAwait(false);
            PublishState();
        }
        finally
        {
            gate.Release();
        }
    }

    private StationDecision DecideHead(IoBoardSnapshot snapshot, DateTimeOffset now)
    {
        var travel = IsInputOn(snapshot, HeadTravelPointNo);
        var lower = IsInputOn(snapshot, HeadLowerLimitPointNo);
        var upper = IsInputOn(snapshot, HeadUpperLimitPointNo);
        if (lower && upper)
        {
            return StationDecision.Fault("线头上下限同时触发");
        }

        if (travel)
        {
            headTravelOffDelay.Reset();
            return upper
                ? StationDecision.Stop("线头上限到位")
                : StationDecision.Run(LiftAction.Up, "线头上升中");
        }

        if (lower)
        {
            headTravelOffDelay.Reset();
            return StationDecision.Stop("线头下限到位");
        }

        return headTravelOffDelay.HasElapsed(now, TravelOffDelay)
            ? StationDecision.Run(LiftAction.Down, "线头下降中")
            : StationDecision.Stop($"线头离开等待 {TravelOffDelay.TotalSeconds:F0} 秒");
    }

    private StationDecision DecideTail(IoBoardSnapshot snapshot, DateTimeOffset now)
    {
        var travel = IsInputOn(snapshot, TailTravelPointNo);
        var lower = IsInputOn(snapshot, TailLowerLimitPointNo);
        var upper = IsInputOn(snapshot, TailUpperLimitPointNo);
        if (lower && upper)
        {
            return StationDecision.Fault("线尾上下限同时触发");
        }

        if (travel)
        {
            tailTravelOffDelay.Reset();
            return lower
                ? StationDecision.Stop("线尾下限到位")
                : StationDecision.Run(LiftAction.Down, "线尾下降中");
        }

        if (upper)
        {
            tailTravelOffDelay.Reset();
            return StationDecision.Stop("线尾上限到位");
        }

        return tailTravelOffDelay.HasElapsed(now, TravelOffDelay)
            ? StationDecision.Run(LiftAction.Up, "线尾上升中")
            : StationDecision.Stop($"线尾离开等待 {TravelOffDelay.TotalSeconds:F0} 秒");
    }

    private async Task<ApiResult> WriteAllAutoOutputsOffAsync(CancellationToken cancellationToken)
    {
        ApiResult? firstFailure = null;
        foreach (var group in AutoOutputPointNos.GroupBy(GetOutputModuleIndex))
        {
            var targets = group.ToDictionary(pointNo => pointNo, _ => false);
            var result = await ApplyAutoOutputsAsync(targets, cancellationToken, force: true).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                firstFailure ??= result;
            }
        }

        return firstFailure ?? ApiResult.Ok();
    }

    private async Task<ApiResult> ApplyAutoOutputsAsync(
        IReadOnlyDictionary<int, bool> targets,
        CancellationToken cancellationToken,
        bool force = false)
    {
        if (!force && lastAutoOutputTargets is not null && targets.Keys.All(pointNo =>
                lastAutoOutputTargets.TryGetValue(pointNo, out var current) &&
                targets.TryGetValue(pointNo, out var target) &&
                current == target))
        {
            return ApiResult.Ok();
        }

        var result = await ioBoardService.WriteOutputsAsync(
            targets,
            cancellationToken,
            IoOutputWriteSource.Automatic).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            lastAutoOutputTargets ??= new Dictionary<int, bool>();
            foreach (var target in targets)
            {
                lastAutoOutputTargets[target.Key] = target.Value;
            }
        }

        return result;
    }

    private Dictionary<int, bool> CreateExtensionCardOutputTargets(
        LiftAction headAction,
        LiftAction tailAction,
        IoBoardSnapshot snapshot,
        DateTimeOffset now,
        bool triggerHeadCylinder,
        bool triggerTailCylinder)
    {
        var targets = AutoOutputPointNos
            .Where(pointNo => GetOutputModuleIndex(pointNo) == 1)
            .ToDictionary(pointNo => pointNo, _ => false);
        ApplyAction(targets, HeadUpOutputs, headAction == LiftAction.Up);
        ApplyAction(targets, HeadDownOutputs, headAction == LiftAction.Down);
        ApplyAction(targets, TailUpOutputs, tailAction == LiftAction.Up);
        ApplyAction(targets, TailDownOutputs, tailAction == LiftAction.Down);
        if (IsInputOn(snapshot, HeadFoolproofPointNo))
        {
            headCylinderPulse.Reset();
            targets[HeadCylinderOutput] = false;
        }
        else
        {
            targets[HeadCylinderOutput] = headCylinderPulse.IsActive(
                triggerHeadCylinder,
                now,
                CylinderPulseDuration);
        }

        targets[TailCylinderOutput] = tailCylinderPulse.IsActive(
            triggerTailCylinder,
            now,
            CylinderPulseDuration);
        return targets;
    }

    private static Dictionary<int, bool> CreateMainCardOutputTargets(IoBoardSnapshot snapshot)
    {
        var targets = AutoOutputPointNos
            .Where(pointNo => GetOutputModuleIndex(pointNo) == 0)
            .ToDictionary(pointNo => pointNo, _ => false);
        ApplyAction(targets, MainCardAlwaysOnOutputs, true);

        var headLowerNoTravel = IsInputOn(snapshot, HeadLowerLimitPointNo) &&
            !IsInputOn(snapshot, HeadTravelPointNo);
        var headFirstStationPhoto = IsInputOn(snapshot, HeadFirstStationPhotoPointNo);
        targets[MainCardHeadLowerNoTravelOutputY0] = headLowerNoTravel;
        targets[MainCardHeadLowerNoTravelOutputY1] = headLowerNoTravel && !headFirstStationPhoto;
        return targets;
    }

    private static int GetOutputModuleIndex(int pointNo)
    {
        return pointNo <= 16 ? 0 : 1;
    }

    private static void ApplyAction(Dictionary<int, bool> targets, IEnumerable<int> pointNos, bool value)
    {
        foreach (var pointNo in pointNos)
        {
            targets[pointNo] = value;
        }
    }

    private void PublishRunningStatus(string message)
    {
        if (State != LineRunState.Running || string.Equals(StatusMessage, message, StringComparison.Ordinal))
        {
            return;
        }

        StatusMessage = message;
        PublishState();
    }

    private void PublishState()
    {
        StateChanged?.Invoke(this, new LineStateChangedEventArgs(State, StatusMessage));
    }

    private Task WriteOperationLogAsync(
        string command,
        string message,
        LogLevelKind level,
        CancellationToken cancellationToken)
    {
        return appLogService.WriteAsync(new AppLogEntry
        {
            Level = level,
            Category = LogCategory.Operation,
            Message = message,
            Source = nameof(LineControlService),
            Target = "Line",
            Details = message,
            Command = command
        }, cancellationToken);
    }

    private static bool IsInputOn(IoBoardSnapshot snapshot, int pointNo)
    {
        return snapshot.Inputs.FirstOrDefault(point => point.Definition.PointNo == pointNo)?.IsOn == true;
    }

    private enum LiftAction
    {
        Stop,
        Up,
        Down
    }

    private sealed record StationDecision(LiftAction Action, string StatusText, string FaultMessage)
    {
        public static StationDecision Stop(string statusText) => new(LiftAction.Stop, statusText, string.Empty);

        public static StationDecision Run(LiftAction action, string statusText) => new(action, statusText, string.Empty);

        public static StationDecision Fault(string message) => new(LiftAction.Stop, string.Empty, message);
    }

    private sealed class StationRuntime
    {
        private readonly string name;
        private LiftAction currentAction = LiftAction.Stop;
        private DateTimeOffset? actionStartedAt;

        public StationRuntime(string name)
        {
            this.name = name;
        }

        public void Reset()
        {
            currentAction = LiftAction.Stop;
            actionStartedAt = null;
        }

        public StationUpdate Update(LiftAction nextAction, DateTimeOffset now, TimeSpan timeout)
        {
            if (nextAction == LiftAction.Stop)
            {
                var completedAction = currentAction;
                Reset();
                return new StationUpdate(string.Empty, completedAction);
            }

            if (currentAction != nextAction || actionStartedAt is null)
            {
                currentAction = nextAction;
                actionStartedAt = now;
                return new StationUpdate(string.Empty, LiftAction.Stop);
            }

            var timeoutMessage = now - actionStartedAt.Value > timeout
                ? $"{name}{ToActionText(nextAction)}超时，超过 {timeout.TotalSeconds:F0} 秒未到位"
                : string.Empty;
            return new StationUpdate(timeoutMessage, LiftAction.Stop);
        }

        private static string ToActionText(LiftAction action)
        {
            return action switch
            {
                LiftAction.Up => "上升",
                LiftAction.Down => "下降",
                _ => "动作"
            };
        }
    }

    private sealed record StationUpdate(string TimeoutMessage, LiftAction CompletedAction);

    private sealed class TravelOffDelayRuntime
    {
        private DateTimeOffset? startedAt;

        public void Reset()
        {
            startedAt = null;
        }

        public bool HasElapsed(DateTimeOffset now, TimeSpan delay)
        {
            startedAt ??= now;
            return now - startedAt.Value >= delay;
        }
    }

    private sealed class CylinderPulseRuntime
    {
        private DateTimeOffset? startedAt;

        public void Reset()
        {
            startedAt = null;
        }

        public bool IsActive(bool trigger, DateTimeOffset now, TimeSpan duration)
        {
            if (trigger)
            {
                startedAt = now;
            }

            return duration > TimeSpan.Zero &&
                startedAt is not null &&
                now - startedAt.Value < duration;
        }
    }
}

public enum LineRunState
{
    Idle,
    Running,
    Fault
}

public sealed record LineStateChangedEventArgs(LineRunState State, string Message);
