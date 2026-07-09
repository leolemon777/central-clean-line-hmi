using System.IO;
using PipelineControl.UI.Services.Io;
using PipelineControl.UI.Services.Io.Mapping;
using PipelineControl.UI.Services.Logs;
using PipelineControl.UI.Services.Settings;
using Xunit;

namespace PipelineControl.UI.Tests;

public sealed class IoBoardServiceTests
{
    [Fact]
    public async Task Write_output_is_blocked_when_not_connected()
    {
        var service = CreateService();

        var result = await service.WriteOutputAsync(1, true);

        Assert.False(result.IsSuccess);
        Assert.Equal(-1, result.Code);
    }

    [Fact]
    public async Task Write_output_updates_snapshot_after_connect()
    {
        var service = CreateService();
        await service.ConnectAsync(forceMock: true);

        var result = await service.WriteOutputAsync(1, true);

        Assert.True(result.IsSuccess);
        Assert.True(service.CurrentSnapshot.Outputs.Single(point => point.Definition.PointNo == 1).IsOn);
    }

    [Fact]
    public async Task Connect_does_not_write_any_output_bits()
    {
        var driver = new RecordingIoBoardDriver();
        var service = CreateService(driver: driver);

        var result = await service.ConnectAsync(forceMock: true);

        Assert.True(result.IsSuccess);
        Assert.Empty(driver.Writes);
    }

    [Fact]
    public async Task Disabled_output_is_blocked()
    {
        var map = JsonIoPointMapProvider.CreateDefaultDocument();
        map.Outputs[0] = map.Outputs[0] with { IsEnabled = false };
        var service = CreateService(new InMemoryMapProvider(map));
        await service.ConnectAsync(forceMock: true);

        var result = await service.WriteOutputAsync(1, true);

        Assert.False(result.IsSuccess);
        Assert.Equal(7, result.Code);
    }

    [Fact]
    public async Task Consecutive_poll_failures_mark_board_offline()
    {
        var driver = new FailingInputDriver();
        var service = CreateService(driver: driver);
        await service.ConnectAsync(forceMock: true);
        await service.StartPollingAsync();

        for (var i = 0; i < 20 && service.CurrentSnapshot.IsConnected; i++)
        {
            await Task.Delay(75);
        }

        await service.StopPollingAsync();

        Assert.False(service.CurrentSnapshot.IsConnected);
        Assert.True(service.CurrentSnapshot.ConsecutiveFailureCount >= 5);
    }

    [Fact]
    public async Task Simulation_settings_connect_to_mock_driver_without_real_dll()
    {
        var factory = new TestDriverFactory();
        var service = CreateService(factory: factory);

        var result = await service.ConnectAsync();

        Assert.True(result.IsSuccess);
        Assert.False(factory.LastOptions!.UseRealDriver);
        Assert.Equal("Mock", service.CurrentSnapshot.MainCardIp);
    }

    [Fact]
    public async Task Real_mode_requires_configured_ip_addresses()
    {
        var factory = new TestDriverFactory();
        var service = CreateService(factory: factory, settingsService: new TestSettingsService(settings =>
        {
            settings.Advanced.SimulationMode = false;
            settings.CardComm.PcIp = string.Empty;
            settings.CardComm.MainCardIp = string.Empty;
        }));

        var result = await service.ConnectAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(7, result.Code);
        Assert.False(service.CurrentSnapshot.IsConnected);
        Assert.Equal(1, factory.CreateCount);
    }

    [Fact]
    public async Task Reset_turns_outputs_off_and_does_not_restore_previous_state()
    {
        var service = CreateService();
        await service.ConnectAsync(forceMock: true);
        await service.WriteOutputAsync(1, true);

        var result = await service.ResetAsync();

        Assert.True(result.IsSuccess);
        Assert.False(service.CurrentSnapshot.Outputs.Single(point => point.Definition.PointNo == 1).IsOn);
    }

    [Fact]
    public async Task Manual_forced_reset_only_closes_outputs_that_were_forced_on()
    {
        var driver = new RecordingIoBoardDriver();
        var service = CreateService(driver: driver);
        await service.ConnectAsync(forceMock: true);
        await service.WriteOutputAsync(1, true);
        await service.WriteOutputAsync(2, true);
        await service.WriteOutputAsync(2, false);
        driver.Writes.Clear();

        var result = await service.ResetForcedOutputsAsync();

        Assert.True(result.IsSuccess);
        var write = Assert.Single(driver.Writes);
        Assert.Equal((0, 0, false), write);
        Assert.Equal(0, driver.ResetCount);
        Assert.False(service.CurrentSnapshot.Outputs.Single(point => point.Definition.PointNo == 1).IsOn);
    }

    [Fact]
    public async Task Batch_output_write_uses_single_module_write_and_updates_snapshot()
    {
        var driver = new RecordingIoBoardDriver();
        var service = CreateService(driver: driver);
        await service.ConnectAsync(forceMock: true);

        var result = await service.WriteOutputsAsync(new Dictionary<int, bool>
        {
            [17] = true,
            [18] = true
        });

        Assert.True(result.IsSuccess);
        var write = Assert.Single(driver.ModuleWrites);
        Assert.Equal((1, 0x0003), write);
        Assert.Empty(driver.Writes);
        Assert.True(service.CurrentSnapshot.Outputs.Single(point => point.Definition.PointNo == 17).IsOn);
        Assert.True(service.CurrentSnapshot.Outputs.Single(point => point.Definition.PointNo == 18).IsOn);
    }

    [Fact]
    public async Task Batch_output_write_failure_does_not_update_snapshot()
    {
        var driver = new RecordingIoBoardDriver { FailModuleWrite = true };
        var service = CreateService(driver: driver);
        await service.ConnectAsync(forceMock: true);

        var result = await service.WriteOutputsAsync(new Dictionary<int, bool>
        {
            [17] = true,
            [18] = true
        });

        Assert.False(result.IsSuccess);
        Assert.Empty(driver.ModuleWrites);
        Assert.False(service.CurrentSnapshot.Outputs.Single(point => point.Definition.PointNo == 17).IsOn);
        Assert.False(service.CurrentSnapshot.Outputs.Single(point => point.Definition.PointNo == 18).IsOn);
    }

    [Fact]
    public async Task Event_bridge_writes_io_logs_without_alarm_page_service()
    {
        var service = CreateService(settingsService: new TestSettingsService(settings =>
        {
            settings.Advanced.SimulationMode = false;
            settings.CardComm.PcIp = string.Empty;
            settings.CardComm.MainCardIp = string.Empty;
        }));
        var logWriter = new RecordingAppLogService();
        using var bridge = new IoBoardEventBridge(service, logWriter);

        await service.ConnectAsync();

        await WaitForAsync(() => logWriter.Entries.Any(entry => entry.StatusCode == "IO-CONNECT-FAILED"));
        Assert.Contains(logWriter.Entries, entry => entry.Level == LogLevelKind.Error && entry.Category == LogCategory.Runtime);
    }

    [Fact]
    public async Task Manual_adapter_does_not_silently_succeed_when_io_write_fails()
    {
        var service = CreateService();
        var adapter = new MockIoCardService(service);

        await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.SetOutputAsync("Y1", true));
    }

    private static IoBoardService CreateService(
        IIoPointMapProvider? provider = null,
        IIoBoardDriver? driver = null,
        TestDriverFactory? factory = null,
        ISettingsService? settingsService = null)
    {
        return new IoBoardService(
            factory ?? new TestDriverFactory(driver),
            provider ?? new JsonIoPointMapProvider("missing-io-points.json"),
            settingsService ?? new TestSettingsService());
    }

    private sealed class TestDriverFactory : IIoBoardDriverFactory
    {
        private readonly IIoBoardDriver? driver;

        public int CreateCount { get; private set; }

        public IoBoardConnectionOptions? LastOptions { get; private set; }

        public TestDriverFactory(IIoBoardDriver? driver)
        {
            this.driver = driver;
        }

        public TestDriverFactory()
        {
        }

        public IIoBoardDriver Create(IoBoardConnectionOptions options)
        {
            CreateCount++;
            LastOptions = options;
            return driver ?? new PipelineControl.UI.Services.Io.Drivers.MockIoBoardDriver();
        }
    }

    private sealed class InMemoryMapProvider : IIoPointMapProvider
    {
        private readonly IoPointMap map;

        public InMemoryMapProvider(IoPointMapDocument document)
        {
            map = new IoPointMap(document.Notes, document.Inputs, document.Outputs);
        }

        public IoPointMap Load() => map;
    }

    private sealed class TestSettingsService : ISettingsService
    {
        private readonly Action<SystemSettings>? configure;

        public TestSettingsService(Action<SystemSettings>? configure = null)
        {
            this.configure = configure;
        }

        public string AppSettingsPath => string.Empty;

        public string LocalSettingsPath => string.Empty;

        public Task<SystemSettings> LoadAsync(CancellationToken cancellationToken = default)
        {
            var settings = SystemSettings.CreateDefaults();
            settings.Advanced.SimulationMode = true;
            settings.CardComm.ScanCycleMs = 50;
            configure?.Invoke(settings);
            return Task.FromResult(settings);
        }

        public Task<SystemSettings> LoadDefaultsAsync(CancellationToken cancellationToken = default) => LoadAsync(cancellationToken);

        public Task SaveLocalAsync(SystemSettings settings, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ResetLocalAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<string> BackupAsync(string targetDirectory, CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);

        public Task RestoreAsync(string backupFilePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (var i = 0; i < 40; i++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(50);
        }

        Assert.True(condition(), "Condition was not met before timeout.");
    }

    private static async Task WaitForAsync(Func<Task<bool>> condition)
    {
        for (var i = 0; i < 40; i++)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(50);
        }

        Assert.True(await condition(), "Condition was not met before timeout.");
    }

    private sealed class FailingInputDriver : IIoBoardDriver
    {
        public string DriverName => "Failing";

        public ApiResult Connect(string pcIp) => ApiResult.Ok();

        public ApiResult Disconnect() => ApiResult.Ok();

        public ApiResult Reset() => ApiResult.Ok();

        public ApiResult<bool> ReadInputBit(int moduleIndex, int bitIndex) => ApiResult<bool>.Fail(-1, "ReadInputBit");

        public ApiResult WriteOutputBit(int moduleIndex, int bitIndex, bool value) => ApiResult.Ok();

        public ApiResult WriteOutputModule(int moduleIndex, int value) => ApiResult.Ok();

        public ApiResult<bool> ReadOutputBit(int moduleIndex, int bitIndex) => ApiResult<bool>.Ok(false);

        public ApiResult<IReadOnlyList<IoModuleImage>> ReadAllInputs(int startModuleIndex, int moduleCount) => ApiResult<IReadOnlyList<IoModuleImage>>.Fail(-1, "ReadAllInputs");

        public ApiResult<IReadOnlyList<IoModuleImage>> ReadAllOutputs(int startModuleIndex, int moduleCount)
        {
            return ApiResult<IReadOnlyList<IoModuleImage>>.Ok(Enumerable.Range(startModuleIndex, moduleCount).Select(module => new IoModuleImage(module, 0)).ToList());
        }

        public ApiResult<double> ReadAdcVoltage(int channel) => ApiResult<double>.Ok(0);

        public ApiResult WriteDacVoltage(int channel, double voltage) => ApiResult.Ok();
    }
}
