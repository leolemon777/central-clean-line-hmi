using PipelineControl.UI.Services.Io.Mapping;
using PipelineControl.UI.Services.Settings;

namespace PipelineControl.UI.Services.Io;

public sealed class IoBoardService : IDisposable
{
    private const int FailureUnstableThreshold = 3;
    private const int FailureOfflineThreshold = 5;
    private readonly IIoBoardDriverFactory driverFactory;
    private readonly IIoPointMapProvider mapProvider;
    private readonly ISettingsService settingsService;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly IoPointMap pointMap;
    private readonly Dictionary<int, IoPointDefinition> inputDefinitions;
    private readonly Dictionary<int, IoPointDefinition> outputDefinitions;
    private readonly Dictionary<(IoType Type, int PointNo), bool> states = new();
    private readonly HashSet<int> manualForcedOutputs = new();
    private readonly HashSet<int> automaticOutputs = new();
    private CancellationTokenSource? pollingCts;
    private Task? pollingTask;
    private IIoBoardDriver driver;
    private IoBoardConnectionOptions options = new();
    private bool disposed;
    private bool isConnected;
    private bool isResetting;
    private int consecutiveFailureCount;
    private string lastError = string.Empty;

    public IoBoardService(
        IIoBoardDriverFactory driverFactory,
        IIoPointMapProvider mapProvider,
        ISettingsService settingsService)
    {
        this.driverFactory = driverFactory;
        this.mapProvider = mapProvider;
        this.settingsService = settingsService;
        pointMap = mapProvider.Load();
        inputDefinitions = pointMap.Inputs.ToDictionary(point => point.PointNo);
        outputDefinitions = pointMap.Outputs.ToDictionary(point => point.PointNo);
        driver = driverFactory.Create(options with { UseRealDriver = false });
        InitializeStateCache();
    }

    public event EventHandler<IoBoardSnapshot>? SnapshotChanged;

    public event EventHandler<IoBoardLogEntry>? LogAppended;

    public event EventHandler<IoBoardAlarm>? AlarmRaised;

    public IReadOnlyList<IoPointDefinition> Inputs => pointMap.Inputs;

    public IReadOnlyList<IoPointDefinition> Outputs => pointMap.Outputs;

    public bool IsConnected => isConnected;

    public int ManualForcedOutputCount => manualForcedOutputs.Count;

    public bool HasManualForcedOutputs => manualForcedOutputs.Count > 0;

    public IoBoardSnapshot CurrentSnapshot => CreateSnapshot();

    public async Task<ApiResult> ConnectAsync(CancellationToken cancellationToken = default, bool forceMock = false)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (isResetting)
            {
                return ApiResult.Fail(1, "IO 正在复位，暂不允许重新连接");
            }

            var optionsResult = forceMock
                ? ApiResult<IoBoardConnectionOptions>.Ok(CreateMockOptions())
                : await LoadOptionsAsync(cancellationToken).ConfigureAwait(false);
            if (!optionsResult.IsSuccess || optionsResult.Value is null)
            {
                isConnected = false;
                lastError = optionsResult.Message;
                AppendLog("Error", optionsResult.Message);
                RaiseAlarm("IO-CONNECT-FAILED", optionsResult.Message);
                PublishSnapshot();
                return new ApiResult(optionsResult.Code, optionsResult.Message);
            }

            options = optionsResult.Value;
            driver = driverFactory.Create(options);

            var result = driver.Connect(options.PcIp);
            if (!result.IsSuccess)
            {
                isConnected = false;
                lastError = result.Message;
                RaiseAlarm(result.Code == -6 ? "IO-OPEN-FAILED" : "IO-CONNECT-FAILED", result.Message);
                AppendLog("Error", result.Message);
                PublishSnapshot();
                return result;
            }

            isConnected = true;
            consecutiveFailureCount = 0;
            lastError = string.Empty;
            manualForcedOutputs.Clear();
            automaticOutputs.Clear();
            AppendLog("Info", $"{driver.DriverName} 已连接，PC={options.PcIp}，Card={options.MainCardIp}");

            RefreshAllFromDriver();
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
            AppendLog(result.IsSuccess ? "Info" : "Warn", result.Message);
            PublishSnapshot();
            return result;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<ApiResult> ResetAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!isConnected)
            {
                var result = ApiResult.Fail(-1, "未连接板卡，禁止执行 IO 复位");
                AppendLog("Warn", result.Message);
                return result;
            }

            isResetting = true;
            AppendLog("Info", "IO 软件安全复位开始：禁止新输出，关闭安全默认 OFF 输出。");

            var safeResult = WriteSafeDefaultsCore(cancellationToken);
            if (!safeResult.IsSuccess)
            {
                lastError = safeResult.Message;
                AppendLog("Error", $"IO 复位失败，安全输出未全部关闭: {safeResult.Message}");
                RaiseAlarm("IO-RESET-FAILED", safeResult.Message);
                PublishSnapshot();
                return safeResult;
            }

            var resetResult = driver.Reset();
            if (!resetResult.IsSuccess)
            {
                lastError = resetResult.Message;
                AppendLog("Error", $"IO 复位失败: {resetResult.Message}");
                RaiseAlarm("IO-RESET-FAILED", resetResult.Message);
                PublishSnapshot();
                return resetResult;
            }

            RefreshAllFromDriver();
            manualForcedOutputs.Clear();
            automaticOutputs.Clear();
            consecutiveFailureCount = 0;
            lastError = string.Empty;
            AppendLog("Info", "IO 软件安全复位完成，状态保持 Ready，不自动恢复输出。");
            PublishSnapshot();
            return resetResult;
        }
        finally
        {
            isResetting = false;
            gate.Release();
        }
    }

    public async Task<ApiResult> ResetForcedOutputsAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!isConnected)
            {
                var result = ApiResult.Fail(-1, "未连接板卡，禁止执行 IO 复位");
                AppendLog("Warn", result.Message);
                return result;
            }

            if (isResetting)
            {
                return ApiResult.Fail(1, "IO 正在复位，禁止输出操作");
            }

            var targets = manualForcedOutputs
                .Where(outputDefinitions.ContainsKey)
                .Select(pointNo => outputDefinitions[pointNo])
                .Where(point => point.IsEnabled)
                .ToArray();

            ApiResult? firstFailure = null;
            foreach (var output in targets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = driver.WriteOutputBit(output.ModuleIndex, output.BitIndex, false);
                if (!result.IsSuccess)
                {
                    firstFailure ??= result;
                    AppendLog("Warn", $"手动复位失败: {output.GlobalLabel} {result.Message}");
                    continue;
                }

                states[(IoType.Output, output.PointNo)] = false;
                manualForcedOutputs.Remove(output.PointNo);
            }

            if (firstFailure is not null)
            {
                lastError = firstFailure.Message;
                RaiseAlarm("IO-RESET-FAILED", firstFailure.Message);
                PublishSnapshot();
                return firstFailure;
            }

            consecutiveFailureCount = 0;
            lastError = string.Empty;
            AppendLog("Info", $"手动输出复位完成，关闭 {targets.Length} 点。");
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

    public async Task<ApiResult> WriteOutputAsync(
        int pointNo,
        bool value,
        CancellationToken cancellationToken = default,
        IoOutputWriteSource source = IoOutputWriteSource.Manual)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var guard = ValidateOutput(pointNo);
            if (!guard.IsSuccess)
            {
                AppendLog("Warn", guard.Message);
                PublishSnapshot();
                return guard;
            }

            var definition = outputDefinitions[pointNo];
            var result = driver.WriteOutputBit(definition.ModuleIndex, definition.BitIndex, value);
            if (!result.IsSuccess)
            {
                lastError = result.Message;
                AppendLog("Error", $"输出 {definition.GlobalLabel} 写入失败: {result.Message}");
                RaiseAlarm("IO-OUTPUT-FAILED", result.Message);
                PublishSnapshot();
                return result;
            }

            var feedback = driver.ReadOutputBit(definition.ModuleIndex, definition.BitIndex);
            states[(IoType.Output, pointNo)] = feedback.IsSuccess ? feedback.Value : value;
            ApplyOutputOwnership(pointNo, value, source);

            AppendLog("Info", $"输出 {definition.GlobalLabel} {(value ? "ON" : "OFF")}，module={definition.ModuleIndex}, bit={definition.BitIndex}");
            PublishSnapshot();
            return result;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<ApiResult> WriteOutputsAsync(
        IReadOnlyDictionary<int, bool> values,
        CancellationToken cancellationToken = default,
        IoOutputWriteSource source = IoOutputWriteSource.Manual)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (values.Count == 0)
            {
                return ApiResult.Ok();
            }

            var guard = ValidateOutputBatch(values.Keys);
            if (!guard.IsSuccess)
            {
                AppendLog("Warn", guard.Message);
                PublishSnapshot();
                return guard;
            }

            var definitions = values.Keys.Select(pointNo => outputDefinitions[pointNo]).ToArray();
            var moduleIndex = definitions[0].ModuleIndex;
            var imageResult = driver.ReadAllOutputs(moduleIndex, 1);
            if (!imageResult.IsSuccess || imageResult.Value is null || imageResult.Value.Count == 0)
            {
                var result = ApiResult.Fail(imageResult.Code, $"读取输出模块 {moduleIndex} 失败: {imageResult.Message}");
                lastError = result.Message;
                AppendLog("Error", result.Message);
                PublishSnapshot();
                return result;
            }

            var moduleValue = imageResult.Value[0].Value;
            foreach (var definition in definitions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (values[definition.PointNo])
                {
                    moduleValue |= 1 << definition.BitIndex;
                }
                else
                {
                    moduleValue &= ~(1 << definition.BitIndex);
                }
            }

            var writeResult = driver.WriteOutputModule(moduleIndex, moduleValue);
            if (!writeResult.IsSuccess)
            {
                lastError = writeResult.Message;
                AppendLog("Error", $"输出模块 {moduleIndex} 批量写入失败: {writeResult.Message}");
                RaiseAlarm("IO-OUTPUT-FAILED", writeResult.Message);
                PublishSnapshot();
                return writeResult;
            }

            var feedback = driver.ReadAllOutputs(moduleIndex, 1);
            var feedbackValue = feedback.IsSuccess && feedback.Value is { Count: > 0 }
                ? feedback.Value[0].Value
                : moduleValue;
            foreach (var output in outputDefinitions.Values.Where(point => point.ModuleIndex == moduleIndex))
            {
                states[(IoType.Output, output.PointNo)] = (feedbackValue & (1 << output.BitIndex)) != 0;
            }

            foreach (var definition in definitions)
            {
                ApplyOutputOwnership(definition.PointNo, values[definition.PointNo], source);
            }

            AppendLog("Info", $"输出模块 {moduleIndex} 批量写入 0x{moduleValue:X4}，点位 {string.Join(",", definitions.Select(point => point.GlobalLabel))}");
            PublishSnapshot();
            return writeResult;
        }
        finally
        {
            gate.Release();
        }
    }

    public Task<ApiResult> WriteOutputByTagAsync(string tagAddress, bool value, CancellationToken cancellationToken = default)
    {
        return TryParseOutputTag(tagAddress, out var pointNo)
            ? WriteOutputAsync(pointNo, value, cancellationToken)
            : Task.FromResult(ApiResult.Fail(7, $"无法解析输出点地址 {tagAddress}"));
    }

    public IoPointDefinition? FindOutput(string tagAddress)
    {
        return TryParseOutputTag(tagAddress, out var pointNo) && outputDefinitions.TryGetValue(pointNo, out var definition)
            ? definition
            : null;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        pollingCts?.Cancel();
        pollingCts?.Dispose();
        gate.Dispose();
        disposed = true;
    }

    private async Task PollLoopAsync(CancellationToken cancellationToken)
    {
        var period = TimeSpan.FromMilliseconds(Math.Clamp(options.ScanCycleMs <= 0 ? 200 : options.ScanCycleMs, 50, 1000));
        using var timer = new PeriodicTimer(period);
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            await PollOnceAsync(cancellationToken).ConfigureAwait(false);
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

            var moduleCount = GetModuleCount();
            var inputs = driver.ReadAllInputs(0, moduleCount);
            if (!inputs.IsSuccess || inputs.Value is null)
            {
                HandleCommunicationFailure(inputs.Code, inputs.Message);
                return;
            }

            ApplyImages(IoType.Input, inputDefinitions.Values, inputs.Value);
            consecutiveFailureCount = 0;
            lastError = string.Empty;
            PublishSnapshot();
        }
        finally
        {
            gate.Release();
        }
    }

    private ApiResult WriteSafeDefaultsCore(CancellationToken cancellationToken)
    {
        ApiResult? firstFailure = null;
        foreach (var output in outputDefinitions.Values.Where(point => point.IsEnabled && !point.SafeDefaultValue))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = driver.WriteOutputBit(output.ModuleIndex, output.BitIndex, false);
            if (!result.IsSuccess)
            {
                firstFailure ??= result;
                AppendLog("Warn", $"安全默认 OFF 写入失败: {output.GlobalLabel} {result.Message}");
                continue;
            }

            states[(IoType.Output, output.PointNo)] = false;
            manualForcedOutputs.Remove(output.PointNo);
            automaticOutputs.Remove(output.PointNo);
        }

        return firstFailure ?? ApiResult.Ok();
    }

    private void RefreshOutputsFromDriver()
    {
        if (!isConnected)
        {
            return;
        }

        var outputs = driver.ReadAllOutputs(0, GetModuleCount());
        if (outputs.IsSuccess && outputs.Value is not null)
        {
            ApplyImages(IoType.Output, outputDefinitions.Values, outputs.Value);
        }
    }

    private void RefreshAllFromDriver()
    {
        if (!isConnected)
        {
            return;
        }

        var moduleCount = GetModuleCount();
        var inputs = driver.ReadAllInputs(0, moduleCount);
        if (inputs.IsSuccess && inputs.Value is not null)
        {
            ApplyImages(IoType.Input, inputDefinitions.Values, inputs.Value);
        }

        RefreshOutputsFromDriver();
    }

    private ApiResult ValidateOutput(int pointNo)
    {
        if (!isConnected)
        {
            return ApiResult.Fail(-1, "未连接板卡，禁止输出操作");
        }

        if (isResetting)
        {
            return ApiResult.Fail(1, "IO 正在复位，禁止输出操作");
        }

        if (!outputDefinitions.TryGetValue(pointNo, out var definition))
        {
            return ApiResult.Fail(7, $"未找到输出点 Y{pointNo}");
        }

        if (!definition.IsEnabled)
        {
            return ApiResult.Fail(7, $"输出点 {definition.GlobalLabel} 未启用");
        }

        return ApiResult.Ok();
    }

    private ApiResult ValidateOutputBatch(IEnumerable<int> pointNos)
    {
        if (!isConnected)
        {
            return ApiResult.Fail(-1, "未连接板卡，禁止输出操作");
        }

        if (isResetting)
        {
            return ApiResult.Fail(1, "IO 正在复位，禁止输出操作");
        }

        var moduleIndex = (int?)null;
        foreach (var pointNo in pointNos)
        {
            if (!outputDefinitions.TryGetValue(pointNo, out var definition))
            {
                return ApiResult.Fail(7, $"未找到输出点 Y{pointNo}");
            }

            if (!definition.IsEnabled)
            {
                return ApiResult.Fail(7, $"输出点 {definition.GlobalLabel} 未启用");
            }

            moduleIndex ??= definition.ModuleIndex;
            if (definition.ModuleIndex != moduleIndex.Value)
            {
                return ApiResult.Fail(7, "批量输出只支持同一个 IO 模块内的点位");
            }
        }

        return ApiResult.Ok();
    }

    private void HandleCommunicationFailure(int code, string message)
    {
        consecutiveFailureCount++;
        lastError = message;
        AppendLog("Warn", $"IO 轮询失败 {consecutiveFailureCount}/{FailureOfflineThreshold}: {message}");

        if (consecutiveFailureCount == FailureUnstableThreshold)
        {
            RaiseAlarm("IO-COMM-UNSTABLE", $"IO 板卡通讯不稳定: {message}");
        }

        if (consecutiveFailureCount >= FailureOfflineThreshold)
        {
            isConnected = false;
            StopDangerousOutputsOnCommunicationFailure();
            RaiseAlarm("IO-COMM-OFFLINE", $"IO 板卡通讯连续失败，已标记离线: {message}");
        }

        PublishSnapshot();
    }

    private void StopDangerousOutputsOnCommunicationFailure()
    {
        foreach (var output in outputDefinitions.Values.Where(point => point.IsEnabled && !point.SafeDefaultValue))
        {
            states[(IoType.Output, output.PointNo)] = false;
        }

        manualForcedOutputs.Clear();
        automaticOutputs.Clear();
        AppendLog("Warn", "通讯失败安全预留: 已将本地输出缓存置为 OFF。现场危险输出必须由硬件安全回路兜底。");
    }

    private void ApplyImages(IoType type, IEnumerable<IoPointDefinition> definitions, IReadOnlyList<IoModuleImage> images)
    {
        var imageMap = images.ToDictionary(image => image.ModuleIndex);
        foreach (var definition in definitions)
        {
            if (imageMap.TryGetValue(definition.ModuleIndex, out var image))
            {
                states[(type, definition.PointNo)] = image.GetBit(definition.BitIndex);
            }
        }
    }

    private IoBoardSnapshot CreateSnapshot()
    {
        return new IoBoardSnapshot(
            isConnected,
            driver.DriverName,
            options.PcIp,
            options.MainCardIp,
            options.ScanCycleMs,
            consecutiveFailureCount,
            lastError,
            inputDefinitions.Values.OrderBy(point => point.PointNo).Select(ToState).ToList(),
            outputDefinitions.Values.OrderBy(point => point.PointNo).Select(ToState).ToList());
    }

    private IoPointState ToState(IoPointDefinition definition)
    {
        states.TryGetValue((definition.IoType, definition.PointNo), out var isOn);
        return new IoPointState(
            definition,
            isOn,
            manualForcedOutputs.Contains(definition.PointNo) || automaticOutputs.Contains(definition.PointNo));
    }

    private void ApplyOutputOwnership(int pointNo, bool value, IoOutputWriteSource source)
    {
        var targets = source == IoOutputWriteSource.Automatic ? automaticOutputs : manualForcedOutputs;
        if (value)
        {
            targets.Add(pointNo);
            return;
        }

        targets.Remove(pointNo);
    }

    private void PublishSnapshot()
    {
        SnapshotChanged?.Invoke(this, CreateSnapshot());
    }

    private void AppendLog(string level, string message)
    {
        LogAppended?.Invoke(this, new IoBoardLogEntry(DateTime.Now, level, message));
    }

    private void RaiseAlarm(string code, string message)
    {
        AlarmRaised?.Invoke(this, new IoBoardAlarm(DateTime.Now, code, message));
    }

    private void InitializeStateCache()
    {
        foreach (var point in pointMap.AllPoints)
        {
            states[(point.IoType, point.PointNo)] = false;
        }
    }

    private int GetModuleCount()
    {
        var maxModule = pointMap.AllPoints.Count == 0 ? 0 : pointMap.AllPoints.Max(point => point.ModuleIndex);
        return maxModule + 1;
    }

    private async Task<ApiResult<IoBoardConnectionOptions>> LoadOptionsAsync(CancellationToken cancellationToken)
    {
        var settings = await settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        var cycle = settings.CardComm.ScanCycleMs <= 0 ? 200 : settings.CardComm.ScanCycleMs;

        if (settings.Advanced.SimulationMode)
        {
            return ApiResult<IoBoardConnectionOptions>.Ok(CreateMockOptions() with { ScanCycleMs = cycle });
        }

        if (string.IsNullOrWhiteSpace(settings.CardComm.PcIp) || string.IsNullOrWhiteSpace(settings.CardComm.MainCardIp))
        {
            return ApiResult<IoBoardConnectionOptions>.Fail(7, "真实 IO 模式需要配置 PC IP 和主卡 IP；没有硬件时请开启仿真模式");
        }

        return ApiResult<IoBoardConnectionOptions>.Ok(new IoBoardConnectionOptions
        {
            PcIp = settings.CardComm.PcIp,
            MainCardIp = settings.CardComm.MainCardIp,
            ScanCycleMs = cycle,
            UseRealDriver = true
        });
    }

    private static IoBoardConnectionOptions CreateMockOptions()
    {
        return new IoBoardConnectionOptions
        {
            PcIp = "127.0.0.1",
            MainCardIp = "Mock",
            ScanCycleMs = 200,
            UseRealDriver = false
        };
    }

    private static bool TryParseOutputTag(string tagAddress, out int pointNo)
    {
        pointNo = 0;
        if (string.IsNullOrWhiteSpace(tagAddress))
        {
            return false;
        }

        var token = tagAddress.Trim();
        var lastDot = token.LastIndexOf('.');
        if (lastDot >= 0 && lastDot < token.Length - 1)
        {
            token = token[(lastDot + 1)..];
        }

        if (token.StartsWith("Output", StringComparison.OrdinalIgnoreCase))
        {
            token = token["Output".Length..];
        }

        if (token.StartsWith('Y') || token.StartsWith('y'))
        {
            token = token[1..];
        }

        return int.TryParse(token.TrimStart('0'), out pointNo) && pointNo is >= 1 and <= 64;
    }
}
