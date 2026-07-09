namespace PipelineControl.UI.Services.Io.Drivers;

public sealed class MockIoBoardDriver : IIoBoardDriver, IConfigurableIoBoardDriver
{
    private readonly object syncRoot = new();
    private readonly Dictionary<int, int> inputs = new();
    private readonly Dictionary<int, int> outputs = new();
    private bool connected;

    public string DriverName => "Mock";

    public void Configure(IoBoardConnectionOptions options)
    {
        // Mock 驱动不需要网络参数，保留该入口以便和真实驱动统一装配。
    }

    public ApiResult Connect(string pcIp)
    {
        lock (syncRoot)
        {
            connected = true;
            EnsureModules();
        }

        return ApiResult.Ok("Mock IO 板卡已连接");
    }

    public ApiResult Disconnect()
    {
        lock (syncRoot)
        {
            connected = false;
        }

        return ApiResult.Ok("Mock IO 板卡已断开");
    }

    public ApiResult Reset()
    {
        lock (syncRoot)
        {
            if (!connected)
            {
                return ApiResult.Fail(-1, "Mock Reset");
            }

            outputs.Clear();
            EnsureModules();
        }

        return ApiResult.Ok();
    }

    public ApiResult<bool> ReadInputBit(int moduleIndex, int bitIndex)
    {
        lock (syncRoot)
        {
            if (!connected)
            {
                return ApiResult<bool>.Fail(-1, "Mock ReadInputBit");
            }

            return ApiResult<bool>.Ok(GetBit(inputs, moduleIndex, bitIndex));
        }
    }

    public ApiResult WriteOutputBit(int moduleIndex, int bitIndex, bool value)
    {
        lock (syncRoot)
        {
            if (!connected)
            {
                return ApiResult.Fail(-1, "Mock WriteOutputBit");
            }

            SetBit(outputs, moduleIndex, bitIndex, value);
        }

        return ApiResult.Ok();
    }

    public ApiResult WriteOutputModule(int moduleIndex, int value)
    {
        lock (syncRoot)
        {
            if (!connected)
            {
                return ApiResult.Fail(-1, "Mock WriteOutputModule");
            }

            outputs[moduleIndex] = value & 0xFFFF;
        }

        return ApiResult.Ok();
    }

    public ApiResult<bool> ReadOutputBit(int moduleIndex, int bitIndex)
    {
        lock (syncRoot)
        {
            if (!connected)
            {
                return ApiResult<bool>.Fail(-1, "Mock ReadOutputBit");
            }

            return ApiResult<bool>.Ok(GetBit(outputs, moduleIndex, bitIndex));
        }
    }

    public ApiResult<IReadOnlyList<IoModuleImage>> ReadAllInputs(int startModuleIndex, int moduleCount)
    {
        lock (syncRoot)
        {
            if (!connected)
            {
                return ApiResult<IReadOnlyList<IoModuleImage>>.Fail(-1, "Mock ReadAllInputs");
            }

            return ApiResult<IReadOnlyList<IoModuleImage>>.Ok(CreateImages(inputs, startModuleIndex, moduleCount));
        }
    }

    public ApiResult<IReadOnlyList<IoModuleImage>> ReadAllOutputs(int startModuleIndex, int moduleCount)
    {
        lock (syncRoot)
        {
            if (!connected)
            {
                return ApiResult<IReadOnlyList<IoModuleImage>>.Fail(-1, "Mock ReadAllOutputs");
            }

            return ApiResult<IReadOnlyList<IoModuleImage>>.Ok(CreateImages(outputs, startModuleIndex, moduleCount));
        }
    }

    public ApiResult<double> ReadAdcVoltage(int channel)
    {
        lock (syncRoot)
        {
            if (!connected)
            {
                return ApiResult<double>.Fail(-1, "Mock ReadAdcVoltage");
            }

            return ApiResult<double>.Ok(0);
        }
    }

    public ApiResult WriteDacVoltage(int channel, double voltage)
    {
        if (voltage is < 0 or > 10)
        {
            return ApiResult.Fail(7, "Mock WriteDacVoltage");
        }

        lock (syncRoot)
        {
            return connected ? ApiResult.Ok() : ApiResult.Fail(-1, "Mock WriteDacVoltage");
        }
    }

    private void EnsureModules()
    {
        for (var module = 0; module <= 4; module++)
        {
            inputs.TryAdd(module, 0);
            outputs.TryAdd(module, 0);
        }
    }

    private static IReadOnlyList<IoModuleImage> CreateImages(IReadOnlyDictionary<int, int> source, int startModuleIndex, int moduleCount)
    {
        return Enumerable.Range(startModuleIndex, Math.Max(0, moduleCount))
            .Select(module => new IoModuleImage(module, source.TryGetValue(module, out var value) ? value : 0))
            .ToList();
    }

    private static bool GetBit(IReadOnlyDictionary<int, int> source, int moduleIndex, int bitIndex)
    {
        return source.TryGetValue(moduleIndex, out var value) && (value & (1 << bitIndex)) != 0;
    }

    private static void SetBit(IDictionary<int, int> source, int moduleIndex, int bitIndex, bool value)
    {
        source.TryGetValue(moduleIndex, out var current);
        if (value)
        {
            current |= 1 << bitIndex;
        }
        else
        {
            current &= ~(1 << bitIndex);
        }

        source[moduleIndex] = current;
    }
}
