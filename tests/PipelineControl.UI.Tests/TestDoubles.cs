using PipelineControl.UI.Services.Io;
using PipelineControl.UI.Services.Logs;
using PipelineControl.UI.Services.Navigation;
using PipelineControl.UI.Services.Servo;
using PipelineControl.UI.Services.Servo.Drivers;
using PipelineControl.UI.Services.Servo.Mapping;
using PipelineControl.UI.Services.Settings;

namespace PipelineControl.UI.Tests;

internal sealed class RecordingAppLogService : IAppLogService
{
    public List<AppLogEntry> Entries { get; } = new();

    public Task WriteAsync(AppLogEntry entry, CancellationToken cancellationToken = default)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }
}

internal sealed class TestSettingsService : ISettingsService
{
    private readonly Action<SystemSettings>? configure;

    public TestSettingsService(Action<SystemSettings>? configure = null)
    {
        this.configure = configure;
    }

    public SystemSettings SavedSettings { get; private set; } = SystemSettings.CreateDefaults();

    public string AppSettingsPath => string.Empty;

    public string LocalSettingsPath => string.Empty;

    public Task<SystemSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = SystemSettings.CreateDefaults();
        configure?.Invoke(settings);
        return Task.FromResult(settings);
    }

    public Task<SystemSettings> LoadDefaultsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SystemSettings.CreateDefaults());
    }

    public Task SaveLocalAsync(SystemSettings settings, CancellationToken cancellationToken = default)
    {
        SavedSettings = settings;
        return Task.CompletedTask;
    }

    public Task ResetLocalAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<string> BackupAsync(string targetDirectory, CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);

    public Task RestoreAsync(string backupFilePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class RecordingDriverFactory : IIoBoardDriverFactory
{
    private readonly IIoBoardDriver driver;

    public RecordingDriverFactory(IIoBoardDriver? driver = null)
    {
        this.driver = driver ?? new RecordingIoBoardDriver();
    }

    public int CreateCount { get; private set; }

    public IoBoardConnectionOptions? LastOptions { get; private set; }

    public IIoBoardDriver Create(IoBoardConnectionOptions options)
    {
        CreateCount++;
        LastOptions = options;
        return driver;
    }
}

internal sealed class RecordingIoBoardDriver : IIoBoardDriver
{
    private readonly Dictionary<(int Module, int Bit), bool> inputs = new();
    private readonly Dictionary<(int Module, int Bit), bool> outputs = new();

    public string DriverName => "Recording";

    public bool IsConnected { get; private set; }

    public List<(int Module, int Bit, bool Value)> Writes { get; } = new();

    public List<(int Module, int Value)> ModuleWrites { get; } = new();

    public bool FailModuleWrite { get; set; }

    public int ResetCount { get; private set; }

    public void SetInput(int moduleIndex, int bitIndex, bool value)
    {
        inputs[(moduleIndex, bitIndex)] = value;
    }

    public ApiResult Connect(string pcIp)
    {
        IsConnected = true;
        return ApiResult.Ok();
    }

    public ApiResult Disconnect()
    {
        IsConnected = false;
        return ApiResult.Ok();
    }

    public ApiResult Reset()
    {
        ResetCount++;
        foreach (var key in outputs.Keys.ToArray())
        {
            outputs[key] = false;
        }

        return ApiResult.Ok();
    }

    public ApiResult<bool> ReadInputBit(int moduleIndex, int bitIndex)
    {
        inputs.TryGetValue((moduleIndex, bitIndex), out var value);
        return ApiResult<bool>.Ok(value);
    }

    public ApiResult WriteOutputBit(int moduleIndex, int bitIndex, bool value)
    {
        if (!IsConnected)
        {
            return ApiResult.Fail(-1, "Driver is not connected");
        }

        outputs[(moduleIndex, bitIndex)] = value;
        Writes.Add((moduleIndex, bitIndex, value));
        return ApiResult.Ok();
    }

    public ApiResult WriteOutputModule(int moduleIndex, int value)
    {
        if (!IsConnected)
        {
            return ApiResult.Fail(-1, "Driver is not connected");
        }

        if (FailModuleWrite)
        {
            return ApiResult.Fail(1, "Module write failed");
        }

        for (var bit = 0; bit < 16; bit++)
        {
            outputs[(moduleIndex, bit)] = (value & (1 << bit)) != 0;
        }

        ModuleWrites.Add((moduleIndex, value & 0xFFFF));
        return ApiResult.Ok();
    }

    public ApiResult<bool> ReadOutputBit(int moduleIndex, int bitIndex)
    {
        outputs.TryGetValue((moduleIndex, bitIndex), out var value);
        return ApiResult<bool>.Ok(value);
    }

    public ApiResult<IReadOnlyList<IoModuleImage>> ReadAllInputs(int startModuleIndex, int moduleCount)
    {
        var images = new List<IoModuleImage>();
        for (var module = startModuleIndex; module < startModuleIndex + moduleCount; module++)
        {
            var value = 0;
            foreach (var pair in inputs.Where(pair => pair.Key.Module == module && pair.Value))
            {
                value |= 1 << pair.Key.Bit;
            }

            images.Add(new IoModuleImage(module, value));
        }

        return ApiResult<IReadOnlyList<IoModuleImage>>.Ok(images);
    }

    public ApiResult<IReadOnlyList<IoModuleImage>> ReadAllOutputs(int startModuleIndex, int moduleCount)
    {
        var images = new List<IoModuleImage>();
        for (var module = startModuleIndex; module < startModuleIndex + moduleCount; module++)
        {
            var value = 0;
            foreach (var pair in outputs.Where(pair => pair.Key.Module == module && pair.Value))
            {
                value |= 1 << pair.Key.Bit;
            }

            images.Add(new IoModuleImage(module, value));
        }

        return ApiResult<IReadOnlyList<IoModuleImage>>.Ok(images);
    }

    public ApiResult<double> ReadAdcVoltage(int channel) => ApiResult<double>.Ok(0);

    public ApiResult WriteDacVoltage(int channel, double voltage) => ApiResult.Ok();

    private static IReadOnlyList<IoModuleImage> CreateEmptyImages(int startModuleIndex, int moduleCount)
    {
        return Enumerable.Range(startModuleIndex, moduleCount)
            .Select(module => new IoModuleImage(module, 0))
            .ToList();
    }
}

internal sealed class RecordingNavigator : IPageNavigator
{
    public event EventHandler<string>? Navigated;

    public object? CurrentPageView { get; private set; }

    public string? LastPageKey { get; private set; }

    public void NavigateTo(string pageKey)
    {
        LastPageKey = pageKey;
        CurrentPageView = pageKey;
        Navigated?.Invoke(this, pageKey);
    }
}

internal sealed class TestServoDriverFactory : IServoDriverFactory
{
    public TestServoDriverFactory(IServoDriver? driver = null)
    {
        Driver = driver ?? new MockServoDriver();
    }

    public IServoDriver Driver { get; }

    public ServoConnectionOptions? LastOptions { get; private set; }

    public int CreateCount { get; private set; }

    public IServoDriver Create(ServoConnectionOptions options)
    {
        CreateCount++;
        LastOptions = options;
        if (Driver is IConfigurableServoDriver configurable)
        {
            configurable.Configure(options);
        }

        return Driver;
    }
}

internal sealed class StubServoRegisterMapProvider : IServoRegisterMapProvider
{
    private readonly ServoRegisterMap map;

    public StubServoRegisterMapProvider(ServoRegisterMap? map = null)
    {
        this.map = map ?? CreateDefault();
    }

    public ServoRegisterMap Load() => map;

    public static ServoRegisterMap CreateDefault()
    {
        var registers = new Dictionary<string, ServoRegisterDefinition>(StringComparer.Ordinal)
        {
            ["ServoOn"] = new ServoRegisterDefinition("3607", 2, 0, "使能"),
            ["SpeedCommand"] = new ServoRegisterDefinition("0324", null, null, "速度指令")
        };
        var axes = new List<ServoAxisDefinition>
        {
            new(1, "1#伺服", 1),
            new(2, "2#伺服", 2),
            new(3, "3#伺服", 3),
            new(4, "4#伺服", 4)
        };
        return new ServoRegisterMap(new List<string>(), registers, axes);
    }
}
