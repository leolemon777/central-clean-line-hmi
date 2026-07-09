using PipelineControl.UI.Services.Io;
using PipelineControl.UI.Services.Io.Mapping;
using PipelineControl.UI.Services.Line;
using Xunit;

namespace PipelineControl.UI.Tests;

public sealed class LineControlServiceTests
{
    [Fact]
    public async Task Start_requires_connected_io_board()
    {
        var line = CreateService(out _, out _, out var logs, connect: false);

        var result = await line.StartAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(LineRunState.Idle, line.State);
        Assert.Contains("请先连接板卡", line.StatusMessage, StringComparison.Ordinal);
        Assert.Contains(logs.Entries, entry => entry.Command == "AUTO_START_BLOCKED");
    }

    [Fact]
    public async Task Start_is_blocked_when_manual_outputs_are_not_reset()
    {
        var line = CreateService(out var ioService, out var driver, out _, connect: true);
        await ioService.WriteOutputAsync(1, true);
        driver.Writes.Clear();
        driver.ModuleWrites.Clear();

        var result = await line.StartAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(LineRunState.Idle, line.State);
        Assert.Contains("请先复位手动输出", line.StatusMessage, StringComparison.Ordinal);
        Assert.Empty(driver.Writes);
        Assert.Empty(driver.ModuleWrites);
    }

    [Fact]
    public async Task Head_travel_on_raises_until_head_upper_limit()
    {
        var line = CreateService(out var ioService, out var driver, out _, connect: false);
        driver.SetInput(0, 1, true);
        driver.SetInput(0, 7, true);
        await ConnectAndStartAsync(ioService, line);

        await WaitForAsync(() => driver.ModuleWrites.Contains((1, 0x0003)));

        driver.SetInput(0, 3, true);

        await WaitForAsync(() => driver.ModuleWrites.Contains((1, 0x0000)));
        await line.StopAsync();
    }

    [Fact]
    public async Task Auto_start_keeps_main_card_y2_to_y9_on_until_stop()
    {
        var line = CreateService(out var ioService, out var driver, out _, connect: false);
        await ConnectAndStartAsync(ioService, line);

        await WaitForAsync(() => driver.ModuleWrites.Contains((0, 0x03FC)));

        await line.StopAsync();

        Assert.Contains(driver.ModuleWrites, write => write == (0, 0x0000));
        Assert.Contains(driver.ModuleWrites, write => write == (1, 0x0000));
        Assert.Equal(0, driver.ResetCount);
    }

    [Fact]
    public async Task Head_lower_limit_without_travel_turns_on_main_card_y0_y1_until_travel_returns()
    {
        var line = CreateService(out var ioService, out var driver, out _, connect: false);
        driver.SetInput(0, 2, true);
        driver.SetInput(0, 7, true);
        await ConnectAndStartAsync(ioService, line);

        await WaitForAsync(() => driver.ModuleWrites.Contains((0, 0x03FF)));

        driver.SetInput(0, 1, true);

        await WaitForAsync(() => driver.ModuleWrites.Contains((0, 0x03FC)));
        await line.StopAsync();
    }

    [Fact]
    public async Task Head_first_station_photo_blocks_main_card_y1_only()
    {
        var line = CreateService(out var ioService, out var driver, out _, connect: false);
        driver.SetInput(0, 0, true);
        driver.SetInput(0, 2, true);
        driver.SetInput(0, 7, true);
        await ConnectAndStartAsync(ioService, line);

        await WaitForAsync(() => driver.ModuleWrites.Contains((0, 0x03FD)));
        Assert.DoesNotContain(driver.ModuleWrites.ToArray(), write => write == (0, 0x03FF));

        driver.SetInput(0, 0, false);

        await WaitForAsync(() => driver.ModuleWrites.Contains((0, 0x03FF)));
        await line.StopAsync();
    }

    [Fact]
    public async Task Auto_pulses_head_and_tail_cylinders_after_lift_reaches_work_limit()
    {
        var line = CreateService(out var ioService, out var driver, out _, connect: false);
        line.CylinderPulseDuration = TimeSpan.FromMilliseconds(40);
        line.LoopPeriod = TimeSpan.FromMilliseconds(5);
        driver.SetInput(0, 1, true);
        driver.SetInput(0, 5, true);
        await ConnectAndStartAsync(ioService, line);

        await WaitForAsync(() => driver.ModuleWrites.Contains((1, 0x00C3)));
        Assert.DoesNotContain(driver.ModuleWrites.ToArray(), write => write.Module == 1 && (write.Value & 0x0300) != 0);

        driver.SetInput(0, 3, true);
        driver.SetInput(0, 6, true);

        await WaitForAsync(() => driver.ModuleWrites.Contains((1, 0x0300)));
        await WaitForAsync(() => driver.ModuleWrites.Contains((1, 0x0000)));
        await line.StopAsync();
    }

    [Fact]
    public async Task Auto_does_not_pulse_cylinders_without_travel_inputs()
    {
        var line = CreateService(out var ioService, out var driver, out _, connect: false);
        line.CylinderPulseDuration = TimeSpan.FromMilliseconds(40);
        line.LoopPeriod = TimeSpan.FromMilliseconds(5);
        await ConnectAndStartAsync(ioService, line);

        await Task.Delay(80);

        Assert.DoesNotContain(driver.ModuleWrites.ToArray(), write => write.Module == 1 && (write.Value & 0x0300) != 0);
        await line.StopAsync();
    }

    [Fact]
    public async Task Auto_does_not_pulse_cylinders_after_return_direction_reaches_limit()
    {
        var line = CreateService(out var ioService, out var driver, out _, connect: false);
        line.CylinderPulseDuration = TimeSpan.FromMilliseconds(40);
        line.TravelOffDelay = TimeSpan.FromMilliseconds(20);
        line.LoopPeriod = TimeSpan.FromMilliseconds(5);
        await ConnectAndStartAsync(ioService, line);

        await WaitForAsync(() => driver.ModuleWrites.Contains((1, 0x003C)));

        driver.SetInput(0, 2, true);
        driver.SetInput(0, 7, true);
        await Task.Delay(80);

        Assert.DoesNotContain(driver.ModuleWrites.ToArray(), write => write.Module == 1 && (write.Value & 0x0300) != 0);
        await line.StopAsync();
    }

    [Fact]
    public async Task Auto_head_cylinder_pulse_is_blocked_by_head_foolproof_input()
    {
        var line = CreateService(out var ioService, out var driver, out _, connect: false);
        line.CylinderPulseDuration = TimeSpan.FromMilliseconds(80);
        line.LoopPeriod = TimeSpan.FromMilliseconds(5);
        driver.SetInput(0, 1, true);
        driver.SetInput(0, 8, true);
        await ConnectAndStartAsync(ioService, line);

        await WaitForAsync(() => driver.ModuleWrites.Contains((1, 0x0003)));
        Assert.DoesNotContain(driver.ModuleWrites.ToArray(), write => write.Module == 1 && (write.Value & 0x0100) != 0);

        driver.SetInput(0, 3, true);
        await Task.Delay(100);
        Assert.DoesNotContain(driver.ModuleWrites.ToArray(), write => write.Module == 1 && (write.Value & 0x0100) != 0);

        driver.SetInput(0, 8, false);

        await Task.Delay(100);
        Assert.DoesNotContain(driver.ModuleWrites.ToArray(), write => write.Module == 1 && (write.Value & 0x0100) != 0);
        await line.StopAsync();
    }

    [Fact]
    public async Task Head_travel_off_lowers_until_head_lower_limit()
    {
        var line = CreateService(out var ioService, out var driver, out _, connect: false);
        line.TravelOffDelay = TimeSpan.FromMilliseconds(40);
        driver.SetInput(0, 7, true);
        await ConnectAndStartAsync(ioService, line);

        await Task.Delay(20);
        Assert.DoesNotContain(driver.ModuleWrites, write => write == (1, 0x000C));

        await WaitForAsync(() => driver.ModuleWrites.Contains((1, 0x000C)));

        driver.SetInput(0, 2, true);

        await WaitForAsync(() => driver.ModuleWrites.Contains((1, 0x0000)));
        await line.StopAsync();
    }

    [Fact]
    public async Task Tail_travel_on_lowers_until_tail_lower_limit()
    {
        var line = CreateService(out var ioService, out var driver, out _, connect: false);
        driver.SetInput(0, 1, true);
        driver.SetInput(0, 3, true);
        driver.SetInput(0, 5, true);
        await ConnectAndStartAsync(ioService, line);

        await WaitForAsync(() => driver.ModuleWrites.Contains((1, 0x00C0)));

        driver.SetInput(0, 6, true);

        await WaitForAsync(() => driver.ModuleWrites.Contains((1, 0x0000)));
        await line.StopAsync();
    }

    [Fact]
    public async Task Tail_travel_off_raises_until_tail_upper_limit()
    {
        var line = CreateService(out var ioService, out var driver, out _, connect: false);
        line.TravelOffDelay = TimeSpan.FromMilliseconds(40);
        driver.SetInput(0, 1, true);
        driver.SetInput(0, 3, true);
        await ConnectAndStartAsync(ioService, line);

        await Task.Delay(20);
        Assert.DoesNotContain(driver.ModuleWrites, write => write == (1, 0x0030));

        await WaitForAsync(() => driver.ModuleWrites.Contains((1, 0x0030)));

        driver.SetInput(0, 7, true);

        await WaitForAsync(() => driver.ModuleWrites.Contains((1, 0x0000)));
        await line.StopAsync();
    }

    [Fact]
    public async Task Head_and_tail_actions_are_merged_into_one_module_write()
    {
        var line = CreateService(out var ioService, out var driver, out _, connect: false);
        driver.SetInput(0, 1, true);
        driver.SetInput(0, 5, true);
        await ConnectAndStartAsync(ioService, line);

        await WaitForAsync(() => driver.ModuleWrites.Contains((1, 0x00C3)));

        Assert.DoesNotContain(driver.Writes, write => write.Value);
        await line.StopAsync();
    }

    [Fact]
    public async Task Travel_off_delay_cancels_when_travel_signal_returns()
    {
        var line = CreateService(out var ioService, out var driver, out _, connect: false);
        line.TravelOffDelay = TimeSpan.FromMilliseconds(80);
        driver.SetInput(0, 7, true);
        await ConnectAndStartAsync(ioService, line);

        await Task.Delay(30);
        driver.SetInput(0, 1, true);

        await WaitForAsync(() => driver.ModuleWrites.Contains((1, 0x0003)));
        Assert.DoesNotContain(driver.ModuleWrites, write => write == (1, 0x000C));
        await line.StopAsync();
    }

    [Fact]
    public async Task Action_timeout_enters_fault_and_turns_auto_outputs_off()
    {
        var line = CreateService(out var ioService, out var driver, out _, connect: false);
        line.ActionTimeout = TimeSpan.FromMilliseconds(30);
        line.LoopPeriod = TimeSpan.FromMilliseconds(5);
        driver.SetInput(0, 1, true);
        driver.SetInput(0, 7, true);
        await ConnectAndStartAsync(ioService, line);

        await WaitForAsync(() => line.State == LineRunState.Fault);

        Assert.Contains("超时", line.StatusMessage, StringComparison.Ordinal);
        Assert.Contains(driver.ModuleWrites, write => write == (1, 0x0000));
        await line.StopAsync();
    }

    [Fact]
    public async Task Upper_and_lower_limit_conflict_enters_fault()
    {
        var line = CreateService(out var ioService, out var driver, out _, connect: false);
        driver.SetInput(0, 2, true);
        driver.SetInput(0, 3, true);
        driver.SetInput(0, 7, true);
        await ConnectAndStartAsync(ioService, line);

        await WaitForAsync(() => line.State == LineRunState.Fault);

        Assert.Contains("线头上下限同时触发", line.StatusMessage, StringComparison.Ordinal);
        Assert.Contains(driver.ModuleWrites, write => write == (1, 0x0000));
        await line.StopAsync();
    }

    [Fact]
    public async Task Stop_turns_off_auto_outputs_without_board_reset()
    {
        var line = CreateService(out var ioService, out var driver, out _, connect: false);
        driver.SetInput(0, 1, true);
        driver.SetInput(0, 7, true);
        await ConnectAndStartAsync(ioService, line);
        await WaitForAsync(() => driver.ModuleWrites.Contains((1, 0x0003)));

        await line.StopAsync();

        Assert.Equal(LineRunState.Idle, line.State);
        Assert.Contains(driver.ModuleWrites, write => write == (1, 0x0000));
        Assert.Equal(0, driver.ResetCount);
    }

    private static LineControlService CreateService(
        out IoBoardService ioService,
        out RecordingIoBoardDriver driver,
        out RecordingAppLogService logs,
        bool connect)
    {
        driver = new RecordingIoBoardDriver();
        logs = new RecordingAppLogService();
        ioService = new IoBoardService(
            new RecordingDriverFactory(driver),
            new JsonIoPointMapProvider("missing-io-points.json"),
            new TestSettingsService(settings =>
            {
                settings.Advanced.SimulationMode = true;
                settings.CardComm.ScanCycleMs = 10;
            }));
        var line = new LineControlService(ioService, logs)
        {
            CylinderPulseDuration = TimeSpan.Zero,
            LoopPeriod = TimeSpan.FromMilliseconds(10)
        };

        if (connect)
        {
            ioService.ConnectAsync().GetAwaiter().GetResult();
        }

        return line;
    }

    private static async Task ConnectAndStartAsync(IoBoardService ioService, LineControlService line)
    {
        await ioService.ConnectAsync();
        var result = await line.StartAsync();
        Assert.True(result.IsSuccess, result.Message);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (var i = 0; i < 200; i++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }

        Assert.True(condition(), "Condition was not met before timeout.");
    }
}
