using PipelineControl.UI.Services.Io;
using PipelineControl.UI.Services.Logs;
using PipelineControl.UI.Services.Servo.Mapping;
using PipelineControl.UI.Services.Settings;

namespace PipelineControl.UI.Services.Servo;

public sealed class ServoService : IDisposable
{
    private const int FailureUnstableThreshold = 3;
    private const int FailureOfflineThreshold = 5;

    private readonly IServoDriverFactory driverFactory;
    private readonly IServoRegisterMapProvider registerMapProvider;
    private readonly ISettingsService settingsService;
    private readonly IAppLogService appLogService;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly ServoRegisterMap registerMap;
    private readonly Dictionary<int, ServoAxisDefinition> axisDefinitions;
    private readonly Dictionary<int, bool> servoOnRequests = new();

    private IServoDriver driver;
    private ServoConnectionOptions options = new();
    private CancellationTokenSource? pollingCts;
    private Task? pollingTask;
    private bool isConnected;
    private int consecutiveFailureCount;
    private string lastError = string.Empty;
    private bool disposed;

    public ServoService(
        IServoDriverFactory driverFactory,
        IServoRegisterMapProvider registerMapProvider,
        ISettingsService settingsService,
        IAppLogService appLogService)
    {
        this.driverFactory = driverFactory;
        this.registerMapProvider = registerMapProvider;
        this.settingsService = settingsService;
        this.appLogService = appLogService;
        registerMap = registerMapProvider.Load();
        axisDefinitions = registerMap.Axes.ToDictionary(axis => axis.Axis);
        driver = driverFactory.Create(options with { UseRealDriver = false });
    }

    public event EventHandler<ServoSnapshot>? SnapshotChanged;

    public event EventHandler<ServoLogEntry>? LogAppended;

    public event EventHandler<ServoAlarm>? AlarmRaised;

    public ServoRegisterMap RegisterMap => registerMap;

    public bool IsConnected => isConnected;

    public ServoSnapshot CurrentSnapshot => CreateSnapshot();

    public async Task<ApiResult> ConnectAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var optionsResult = await LoadOptionsAsync(cancellationToken).ConfigureAwait(false);
            if (!optionsResult.IsSuccess || optionsResult.Value is null)
            {
                isConnected = false;
                lastError = optionsResult.Message;
                AppendLog("Error", optionsResult.Message);
                RaiseAlarm("SERVO-CONNECT-FAILED", optionsResult.Message);
                PublishSnapshot();
                return new ApiResult(optionsResult.Code, optionsResult.Message);
            }

            options = optionsResult.Value;
            driver = driverFactory.Create(options);

            var result = driver.Connect(options.GatewayIp, options.GatewayPort);
            if (!result.IsSuccess)
            {
                isConnected = false;
                lastError = result.Message;
                AppendLog("Error", result.Message);
                RaiseAlarm("SERVO-CONNECT-FAILED", result.Message);
                PublishSnapshot();
                return result;
            }

            isConnected = true;
            consecutiveFailureCount = 0;
            lastError = string.Empty;
            servoOnRequests.Clear();
            AppendLog("Info", $"{driver.DriverName} 已连接，网关={options.GatewayIp}:{options.GatewayPort}");

            await StartPollingAsync(cancellationToken).ConfigureAwait(false);
            PublishSnapshot();
            return result;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<ApiResult> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await StopPollingAsync().ConfigureAwait(false);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var result = driver.Disconnect();
            isConnected = false;
            lastError = result.IsSuccess ? string.Empty : result.Message;
            servoOnRequests.Clear();
            AppendLog(result.IsSuccess ? "Info" : "Warn", result.Message);
            PublishSnapshot();
            return result;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<ApiResult> ServoOnAsync(int axis, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!IsReadyForCommand(axis, out var definition, out var guard))
            {
                AppendLog("Warn", guard.Message);
                PublishSnapshot();
                return guard;
            }

            var result = driver.WriteServoOn(definition, registerMap, enable: true);
            await LogAxisResultAsync(axis, "ServoOn", "使能 ON", result, cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                servoOnRequests[axis] = true;
                UpdateAxisStateCache(axis, ToState(definition) with { IsEnabled = true, IsOnline = true });
                PublishSnapshot();
            }

            return result;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<ApiResult> ServoOffAsync(int axis, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!IsReadyForCommand(axis, out var definition, out var guard))
            {
                AppendLog("Warn", guard.Message);
                PublishSnapshot();
                return guard;
            }

            var result = driver.WriteServoOn(definition, registerMap, enable: false);
            await LogAxisResultAsync(axis, "ServoOff", "使能 OFF", result, cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                servoOnRequests[axis] = false;
                UpdateAxisStateCache(axis, ToState(definition) with { IsEnabled = false, IsOnline = true, TargetRpm = 0, ActualRpm = 0 });
                PublishSnapshot();
            }

            return result;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<ApiResult> SetSpeedAsync(int axis, int rpm, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!IsReadyForCommand(axis, out var definition, out var guard))
            {
                AppendLog("Warn", guard.Message);
                PublishSnapshot();
                return guard;
            }

            var clamped = Math.Clamp(rpm, -options.MaxSpeedRpm, options.MaxSpeedRpm);
            var result = driver.WriteSpeed(definition, registerMap, clamped);
            await LogAxisResultAsync(axis, "SetSpeed", $"转速 {clamped} rpm", result, cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                var current = ToState(definition);
                UpdateAxisStateCache(axis, current with
                {
                    IsOnline = true,
                    TargetRpm = clamped,
                    ActualRpm = current.IsEnabled ? clamped : 0
                });
                PublishSnapshot();
            }

            return result;
        }
        finally
        {
            gate.Release();
        }
    }

    // 点动开始：使能 + 写速度（按住期间持续转）
    public async Task<ApiResult> BeginJogAsync(int axis, int rpm, CancellationToken cancellationToken = default)
    {
        var onResult = await ServoOnAsync(axis, cancellationToken).ConfigureAwait(false);
        if (!onResult.IsSuccess)
        {
            return onResult;
        }

        return await SetSpeedAsync(axis, rpm, cancellationToken).ConfigureAwait(false);
    }

    // 点动结束：写 0 转速（不断使能，松开即停）
    public async Task<ApiResult> EndJogAsync(int axis, CancellationToken cancellationToken = default)
    {
        return await SetSpeedAsync(axis, 0, cancellationToken).ConfigureAwait(false);
    }

    // 软启动：从 0 分步逼近目标转速，减小机械冲击（fire-and-forget，不阻塞调用方）
    public async Task SetSpeedRampedAsync(int axis, int targetRpm, int stepRpm, int intervalMs, CancellationToken cancellationToken = default)
    {
        var clampedTarget = Math.Clamp(targetRpm, -options.MaxSpeedRpm, options.MaxSpeedRpm);
        var current = axisStates.TryGetValue(axis, out var state) ? state.TargetRpm : 0;
        var direction = clampedTarget >= current ? 1 : -1;
        var step = Math.Max(1, Math.Abs(stepRpm)) * direction;

        while (current != clampedTarget)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var next = direction > 0 ? Math.Min(clampedTarget, current + step) : Math.Max(clampedTarget, current + step);
            var result = await SetSpeedAsync(axis, next, cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                return;
            }

            current = next;
            if (current != clampedTarget)
            {
                await Task.Delay(Math.Max(10, intervalMs), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    // 多轴同步：对所有轴执行同一个动作（使能+同速正转/反转/停止/停用）
    public async Task<ApiResult> SyncAllAsync(int rpm, bool reverse, bool stopOnly, CancellationToken cancellationToken = default)
    {
        ApiResult? firstFailure = null;
        var target = stopOnly ? 0 : (reverse ? -Math.Abs(rpm) : Math.Abs(rpm));
        foreach (var axisDef in registerMap.Axes)
        {
            var axis = axisDef.Axis;
            if (stopOnly)
            {
                var r = await SetSpeedAsync(axis, 0, cancellationToken).ConfigureAwait(false);
                if (!r.IsSuccess)
                {
                    firstFailure ??= r;
                }

                continue;
            }

            var on = await ServoOnAsync(axis, cancellationToken).ConfigureAwait(false);
            if (!on.IsSuccess)
            {
                firstFailure ??= on;
                continue;
            }

            var sp = await SetSpeedAsync(axis, target, cancellationToken).ConfigureAwait(false);
            if (!sp.IsSuccess)
            {
                firstFailure ??= sp;
            }
        }

        await WriteOperationLogAsync(
            "SERVO_SYNC",
            stopOnly ? "同步停止所有轴" : $"同步{(reverse ? "反转" : "正转")} {Math.Abs(target)} rpm",
            firstFailure is null ? LogLevelKind.Info : LogLevelKind.Warn,
            cancellationToken).ConfigureAwait(false);
        return firstFailure ?? ApiResult.Ok("同步完成");
    }

    public async Task<ApiResult> EmergencyStopAllAsync(CancellationToken cancellationToken = default)
    {
        ApiResult? firstFailure = null;
        foreach (var axis in registerMap.Axes)
        {
            var result = await ServoOffAsync(axis.Axis, cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                firstFailure ??= result;
            }
        }

        await WriteOperationLogAsync("SERVO_ESTOP", "全局急停：已断开所有轴使能", firstFailure is null ? LogLevelKind.Warn : LogLevelKind.Error, cancellationToken)
            .ConfigureAwait(false);
        return firstFailure ?? ApiResult.Ok("全局急停完成");
    }

    public async Task<ApiResult> ResetFaultAsync(int axis, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!axisDefinitions.TryGetValue(axis, out var definition))
            {
                return ApiResult.Fail(7, $"未找到轴号 {axis}");
            }

            // 报警复位：目前手册未给 X4EA 的通信复位地址，先做日志记录并刷新状态。
            // 现场拿到复位地址后，回填 servo-registers.json 并在此调用。
            AppendLog("Info", $"{definition.Name} 故障复位（通信复位地址待手册回填）");
            await WriteOperationLogAsync("SERVO_RESET", $"{definition.Name} 故障复位", LogLevelKind.Info, cancellationToken)
                .ConfigureAwait(false);
            PublishSnapshot();
            return ApiResult.Ok();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task StartPollingAsync(CancellationToken cancellationToken = default)
    {
        if (pollingTask is { IsCompleted: false })
        {
            return;
        }

        pollingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        pollingTask = Task.Run(() => PollLoopAsync(pollingCts.Token), CancellationToken.None);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task StopPollingAsync()
    {
        var cts = pollingCts;
        var task = pollingTask;
        if (cts is null || task is null)
        {
            return;
        }

        cts.Cancel();
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            cts.Dispose();
            pollingCts = null;
            pollingTask = null;
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        pollingCts?.Cancel();
        pollingCts?.Dispose();
        if (driver is IDisposable disposable)
        {
            disposable.Dispose();
        }

        gate.Dispose();
        disposed = true;
    }

    private async Task PollLoopAsync(CancellationToken cancellationToken)
    {
        var period = TimeSpan.FromMilliseconds(Math.Clamp(options.ScanCycleMs <= 0 ? 100 : options.ScanCycleMs, 50, 2000));
        var heartbeatPeriod = TimeSpan.FromMilliseconds(Math.Clamp(options.HeartbeatCycleMs <= 0 ? 3000 : options.HeartbeatCycleMs, 500, 5000));
        using var timer = new PeriodicTimer(period);
        var lastHeartbeat = DateTimeOffset.MinValue;
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            await PollOnceAsync(cancellationToken).ConfigureAwait(false);

            // 使能心跳续写：docx 要求持续写入间隔不超过 P09.11（出厂 5s），按心跳周期续写已使能的轴。
            var now = DateTimeOffset.Now;
            if (now - lastHeartbeat >= heartbeatPeriod)
            {
                lastHeartbeat = now;
                await HeartbeatEnabledAxesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!isConnected)
            {
                return;
            }

            var allSuccess = true;
            foreach (var axis in registerMap.Axes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = driver.ReadAxis(axis, registerMap);
                if (!result.IsSuccess || result.Value is null)
                {
                    allSuccess = false;
                    break;
                }

                UpdateAxisStateCache(axis.Axis, result.Value);
            }

            if (allSuccess)
            {
                consecutiveFailureCount = 0;
                lastError = string.Empty;
            }
            else
            {
                HandleCommunicationFailure("轮询读取失败");
            }

            PublishSnapshot();
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task HeartbeatEnabledAxesAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!isConnected)
            {
                return;
            }

            foreach (var (axis, requestedOn) in servoOnRequests)
            {
                if (!requestedOn || !axisDefinitions.TryGetValue(axis, out var definition))
                {
                    continue;
                }

                var result = driver.WriteServoOn(definition, registerMap, enable: true);
                if (!result.IsSuccess)
                {
                    AppendLog("Warn", $"{definition.Name} 使能心跳续写失败：{result.Message}");
                }
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private bool IsReadyForCommand(int axis, out ServoAxisDefinition definition, out ApiResult guard)
    {
        if (!isConnected)
        {
            definition = null!;
            guard = ApiResult.Fail(-1, "伺服网关未连接，禁止操作");
            return false;
        }

        if (!axisDefinitions.TryGetValue(axis, out definition!))
        {
            guard = ApiResult.Fail(7, $"未找到轴号 {axis}");
            return false;
        }

        guard = ApiResult.Ok();
        return true;
    }

    private void HandleCommunicationFailure(string message)
    {
        consecutiveFailureCount++;
        lastError = message;
        AppendLog("Warn", $"伺服轮询失败 {consecutiveFailureCount}/{FailureOfflineThreshold}：{message}");

        if (consecutiveFailureCount == FailureUnstableThreshold)
        {
            RaiseAlarm("SERVO-COMM-UNSTABLE", $"伺服通讯不稳定：{message}");
        }

        if (consecutiveFailureCount >= FailureOfflineThreshold)
        {
            isConnected = false;
            servoOnRequests.Clear();
            RaiseAlarm("SERVO-COMM-OFFLINE", $"伺服通讯连续失败，已标记离线：{message}");
        }
    }

    private void AppendLog(string level, string message)
    {
        LogAppended?.Invoke(this, new ServoLogEntry(DateTime.Now, level, message));
    }

    private void RaiseAlarm(string code, string message)
    {
        AlarmRaised?.Invoke(this, new ServoAlarm(DateTime.Now, code, message));
    }

    private void PublishSnapshot()
    {
        SnapshotChanged?.Invoke(this, CreateSnapshot());
    }

    private ServoSnapshot CreateSnapshot()
    {
        return new ServoSnapshot(
            isConnected,
            driver.DriverName,
            options.GatewayIp,
            options.ScanCycleMs,
            consecutiveFailureCount,
            lastError,
            registerMap.Axes.OrderBy(axis => axis.Axis).Select(ToState).ToList());
    }

    private ServoAxisState ToState(ServoAxisDefinition definition)
    {
        return axisStates.TryGetValue(definition.Axis, out var state)
            ? state
            : new ServoAxisState(definition.Axis, definition.Station, definition.Name, false, false, 0, 0, 0);
    }

    private readonly Dictionary<int, ServoAxisState> axisStates = new();

    private void UpdateAxisStateCache(int axis, ServoAxisState state)
    {
        axisStates[axis] = state;
    }

    private async Task<ApiResult<ServoConnectionOptions>> LoadOptionsAsync(CancellationToken cancellationToken)
    {
        var settings = await settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        var cycle = settings.ServoComm.ScanCycleMs <= 0 ? 100 : settings.ServoComm.ScanCycleMs;

        if (settings.Advanced.SimulationMode)
        {
            return ApiResult<ServoConnectionOptions>.Ok(CreateMockOptions() with { ScanCycleMs = cycle });
        }

        if (string.IsNullOrWhiteSpace(settings.ServoComm.GatewayIp))
        {
            return ApiResult<ServoConnectionOptions>.Fail(7, "真实伺服模式需要配置网关 IP；没有硬件时请开启仿真模式");
        }

        return ApiResult<ServoConnectionOptions>.Ok(new ServoConnectionOptions
        {
            GatewayIp = settings.ServoComm.GatewayIp,
            GatewayPort = settings.ServoComm.GatewayPort,
            AxisStations = new[]
            {
                settings.ServoComm.Axis1Station,
                settings.ServoComm.Axis2Station,
                settings.ServoComm.Axis3Station,
                settings.ServoComm.Axis4Station
            },
            ScanCycleMs = cycle,
            HeartbeatCycleMs = settings.ServoComm.HeartbeatCycleMs,
            DefaultSpeedRpm = settings.ServoComm.DefaultSpeedRpm,
            MaxSpeedRpm = settings.ServoComm.MaxSpeedRpm,
            UseRealDriver = true
        });
    }

    private static ServoConnectionOptions CreateMockOptions()
    {
        return new ServoConnectionOptions
        {
            GatewayIp = "127.0.0.1",
            GatewayPort = 502,
            ScanCycleMs = 100,
            HeartbeatCycleMs = 3000,
            UseRealDriver = false
        };
    }

    private Task LogAxisResultAsync(int axis, string command, string description, ApiResult result, CancellationToken cancellationToken)
    {
        var definition = axisDefinitions.TryGetValue(axis, out var def) ? def.Name : $"轴{axis}";
        var message = result.IsSuccess ? $"{definition} {description}" : $"{description}失败：{result.Message}";
        if (!result.IsSuccess)
        {
            AppendLog("Warn", message);
        }

        return WriteOperationLogAsync(command, message, result.IsSuccess ? LogLevelKind.Info : LogLevelKind.Warn, cancellationToken);
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
            Category = LogCategory.Communication,
            Message = message,
            Source = nameof(ServoService),
            Target = "Servo",
            Details = message,
            Command = command
        }, cancellationToken);
    }
}
