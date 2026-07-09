using PipelineControl.UI.Services.Io;
using PipelineControl.UI.Services.Io.Mapping;
using PipelineControl.UI.Services.Line;
using PipelineControl.UI.ViewModels.Pages.IoMonitor;
using PipelineControl.UI.ViewModels.Pages.IoMonitor.Models;
using System.IO;
using Xunit;

namespace PipelineControl.UI.Tests;

public sealed class OutputTestViewModelTests
{
    [Fact]
    public async Task Selecting_output_without_connection_does_not_write_driver()
    {
        var viewModel = CreateViewModel(out var driver, out _, simulationMode: false);
        var point = FirstEnabledOutput(viewModel);

        await viewModel.SelectOutputPointCommand.ExecuteAsync(point);

        Assert.Empty(driver.Writes);
        Assert.False(point.IsSelected);
        Assert.Contains("手动模式", viewModel.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Connect_uses_real_mode_configuration_when_simulation_is_disabled()
    {
        var viewModel = CreateViewModel(out var driver, out var factory, simulationMode: false);

        await viewModel.ConnectCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsConnected);
        Assert.NotEmpty(viewModel.InputModules);
        Assert.NotEmpty(viewModel.OutputModules);
        Assert.NotNull(factory.LastOptions);
        Assert.True(factory.LastOptions!.UseRealDriver);
        Assert.Equal("192.168.0.200", factory.LastOptions.PcIp);
        Assert.Equal("192.168.0.1", factory.LastOptions.MainCardIp);
        Assert.Empty(driver.Writes);
    }

    [Fact]
    public async Task Connected_output_requires_manual_mode_before_write()
    {
        var viewModel = CreateViewModel(out var driver, out _, simulationMode: false);
        await viewModel.ConnectCommand.ExecuteAsync(null);
        driver.Writes.Clear();
        var point = FirstEnabledOutput(viewModel);

        await viewModel.SelectOutputPointCommand.ExecuteAsync(point);

        Assert.Empty(driver.Writes);
        Assert.False(point.IsSelected);
        Assert.Contains("手动模式", viewModel.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public void Manual_mode_toggle_updates_status_feedback()
    {
        var viewModel = CreateViewModel(out _, out _, simulationMode: false);

        viewModel.IsManualMode = true;

        Assert.Contains("手动模式已开启", viewModel.StatusText, StringComparison.Ordinal);

        viewModel.IsManualMode = false;

        Assert.Contains("手动模式已关闭", viewModel.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Offline_manual_mode_tracks_multiple_outputs_and_reset_clears_them()
    {
        var viewModel = CreateViewModel(out var driver, out _, simulationMode: false);
        viewModel.IsManualMode = true;
        var outputs = viewModel.OutputModules.SelectMany(module => module.Points).Where(point => point.IsEnabled).Take(3).ToArray();

        foreach (var output in outputs)
        {
            await viewModel.SelectOutputPointCommand.ExecuteAsync(output);
        }

        Assert.Empty(driver.Writes);
        Assert.Equal(3, viewModel.ManualOperationCount);
        Assert.All(outputs, output =>
        {
            Assert.True(output.IsOn);
            Assert.True(output.IsForced);
        });
        Assert.True(viewModel.ResetAllOutputsCommand.CanExecute(null));

        await viewModel.ResetAllOutputsCommand.ExecuteAsync(null);

        Assert.Equal(0, viewModel.ManualOperationCount);
        Assert.All(outputs, output =>
        {
            Assert.False(output.IsOn);
            Assert.False(output.IsForced);
            Assert.False(output.IsSelected);
        });
    }

    [Fact]
    public async Task Reset_turns_off_touched_outputs_and_clears_manual_count()
    {
        var viewModel = CreateViewModel(out var driver, out _, simulationMode: false);
        await viewModel.ConnectCommand.ExecuteAsync(null);
        viewModel.IsManualMode = true;
        driver.Writes.Clear();
        var point = FirstEnabledOutput(viewModel);

        await viewModel.SelectOutputPointCommand.ExecuteAsync(point);
        Assert.Contains(driver.Writes, write => write.Value);
        Assert.Equal(1, viewModel.ManualOperationCount);
        Assert.True(point.IsOn);
        Assert.True(point.IsForced);
        Assert.True(point.IsSelected);

        viewModel.IsManualMode = false;
        Assert.True(viewModel.ResetAllOutputsCommand.CanExecute(null));

        await viewModel.ResetAllOutputsCommand.ExecuteAsync(null);

        Assert.Equal(0, viewModel.ManualOperationCount);
        Assert.False(point.IsOn);
        Assert.False(point.IsForced);
        Assert.False(point.IsSelected);
        Assert.Equal(0, driver.ResetCount);
        Assert.Equal(2, driver.Writes.Count);
        Assert.Equal((point.ModuleIndex, point.BitIndex, false), driver.Writes.Last());
    }

    [Fact]
    public void Confirmed_input_names_are_visible_in_bit_cells()
    {
        var resourcePath = Path.Combine(AppContext.BaseDirectory, "Resources", "io-points.json");
        var viewModel = CreateViewModel(
            out _,
            out _,
            simulationMode: false,
            provider: new JsonIoPointMapProvider(resourcePath));
        var module = viewModel.InputModules.First();
        var point = module.Points.First(point => point.DisplayLabel == "X0");
        var travelPoint = module.Points.First(point => point.DisplayLabel == "X1");
        var agvPoint = module.Points.First(point => point.DisplayLabel == "X4");
        var unnamedPoint = module.Points.First(point => point.DisplayLabel == "X9");

        Assert.Equal("X0", point.BitText);
        Assert.Equal("线头第一工位光电", point.SignalName);
        Assert.Equal("线头第一工位\n光电", point.SignalDisplayName);
        Assert.Equal("线头升降台\n行程开关", travelPoint.SignalDisplayName);
        Assert.Equal("线头防呆\n光电", module.Points.First(point => point.DisplayLabel == "X8").SignalDisplayName);
        Assert.Equal("线尾\nAGV信号", agvPoint.SignalDisplayName);
        Assert.Equal("X9", unnamedPoint.BitText);
        Assert.Equal(string.Empty, unnamedPoint.SignalName);
        Assert.Equal(4, module.Columns);
        Assert.True(module.CellWidth >= 116);
        Assert.True(module.CellHeight >= 44);
    }

    [Fact]
    public async Task Connected_action_group_turns_on_synchronized_outputs_with_one_module_write()
    {
        var viewModel = CreateViewModel(out var driver, out _, simulationMode: false);
        await viewModel.ConnectCommand.ExecuteAsync(null);
        viewModel.IsManualMode = true;
        driver.ModuleWrites.Clear();
        var group = viewModel.OutputActionGroups.Single(group => group.DisplayName == "线头电缸上升");

        await viewModel.ToggleOutputActionGroupCommand.ExecuteAsync(group);

        Assert.True(group.IsOn);
        Assert.Equal(2, viewModel.ManualOperationCount);
        var write = Assert.Single(driver.ModuleWrites);
        Assert.Equal((1, 0x0003), write);
        Assert.True(OutputPoint(viewModel, 17).IsOn);
        Assert.True(OutputPoint(viewModel, 18).IsOn);
    }

    [Fact]
    public async Task Jog_action_group_turns_on_while_pressed_and_off_when_released()
    {
        var viewModel = CreateViewModel(out var driver, out _, simulationMode: false);
        await viewModel.ConnectCommand.ExecuteAsync(null);
        viewModel.IsManualMode = true;
        driver.ModuleWrites.Clear();
        var group = viewModel.OutputActionGroups.Single(group => group.DisplayName == "线头电缸上升");

        await viewModel.BeginOutputActionGroupCommand.ExecuteAsync(group);

        Assert.True(group.IsOn);
        Assert.Equal(2, viewModel.ManualOperationCount);
        Assert.True(OutputPoint(viewModel, 17).IsOn);
        Assert.True(OutputPoint(viewModel, 18).IsOn);

        await viewModel.EndOutputActionGroupCommand.ExecuteAsync(group);

        Assert.False(group.IsOn);
        Assert.Equal(0, viewModel.ManualOperationCount);
        Assert.False(OutputPoint(viewModel, 17).IsOn);
        Assert.False(OutputPoint(viewModel, 18).IsOn);
        Assert.Equal(new[] { (1, 0x0003), (1, 0x0000) }, driver.ModuleWrites);
    }

    [Fact]
    public async Task Action_group_blocks_opposite_cylinder_direction_until_reset()
    {
        var viewModel = CreateViewModel(out var driver, out _, simulationMode: false);
        await viewModel.ConnectCommand.ExecuteAsync(null);
        viewModel.IsManualMode = true;
        driver.ModuleWrites.Clear();
        var up = viewModel.OutputActionGroups.Single(group => group.DisplayName == "线头电缸上升");
        var down = viewModel.OutputActionGroups.Single(group => group.DisplayName == "线头电缸下降");

        await viewModel.ToggleOutputActionGroupCommand.ExecuteAsync(up);
        await viewModel.ToggleOutputActionGroupCommand.ExecuteAsync(down);

        Assert.True(up.IsOn);
        Assert.False(down.IsOn);
        Assert.Single(driver.ModuleWrites);
        Assert.Contains("请先复位", viewModel.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Offline_action_group_tracks_local_feedback_and_reset_clears_it()
    {
        var viewModel = CreateViewModel(out var driver, out _, simulationMode: false);
        viewModel.IsManualMode = true;
        var group = viewModel.OutputActionGroups.Single(group => group.DisplayName == "线尾气缸伸出");

        await viewModel.ToggleOutputActionGroupCommand.ExecuteAsync(group);

        Assert.Empty(driver.ModuleWrites);
        Assert.True(group.IsOn);
        Assert.True(OutputPoint(viewModel, 26).IsOn);
        Assert.Equal(1, viewModel.ManualOperationCount);

        await viewModel.ResetAllOutputsCommand.ExecuteAsync(null);

        Assert.False(group.IsOn);
        Assert.False(OutputPoint(viewModel, 26).IsOn);
        Assert.Equal(0, viewModel.ManualOperationCount);
    }

    [Fact]
    public async Task Head_foolproof_input_blocks_head_cylinder_output()
    {
        var driver = new RecordingIoBoardDriver();
        driver.SetInput(0, 8, true);
        var ioService = CreateIoService(driver, simulationMode: false);
        var viewModel = new OutputTestViewModel(ioService);
        await viewModel.ConnectCommand.ExecuteAsync(null);
        viewModel.IsManualMode = true;
        driver.ModuleWrites.Clear();
        var group = viewModel.OutputActionGroups.Single(group => group.DisplayName == "线头气缸伸出");

        await viewModel.BeginOutputActionGroupCommand.ExecuteAsync(group);

        Assert.False(group.IsOn);
        Assert.False(OutputPoint(viewModel, 25).IsOn);
        Assert.Empty(driver.ModuleWrites);
        Assert.Contains("线头防呆光电", viewModel.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Head_foolproof_input_turns_off_active_head_cylinder_output()
    {
        var driver = new RecordingIoBoardDriver();
        var ioService = CreateIoService(driver, simulationMode: false);
        var viewModel = new OutputTestViewModel(ioService);
        await viewModel.ConnectCommand.ExecuteAsync(null);
        viewModel.IsManualMode = true;
        var group = viewModel.OutputActionGroups.Single(group => group.DisplayName == "线头气缸伸出");

        await viewModel.BeginOutputActionGroupCommand.ExecuteAsync(group);
        Assert.True(group.IsOn);
        Assert.Contains(driver.ModuleWrites, write => write == (1, 0x0100));
        driver.ModuleWrites.Clear();

        driver.SetInput(0, 8, true);
        await ioService.ConnectAsync();

        await WaitForAsync(() =>
            driver.ModuleWrites.Contains((1, 0x0000)) &&
            !group.IsOn &&
            viewModel.ManualOperationCount == 0);
        Assert.False(OutputPoint(viewModel, 25).IsOn);
        Assert.Equal(0, viewModel.ManualOperationCount);
        Assert.Contains("线头防呆光电", viewModel.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Automatic_running_blocks_manual_output_operations()
    {
        var driver = new RecordingIoBoardDriver();
        driver.SetInput(0, 2, true);
        driver.SetInput(0, 7, true);
        var factory = new RecordingDriverFactory(driver);
        var ioService = new IoBoardService(
            factory,
            new JsonIoPointMapProvider("missing-io-points.json"),
            new TestSettingsService(settings => settings.Advanced.SimulationMode = false));
        var lineControlService = new LineControlService(ioService, new RecordingAppLogService())
        {
            CylinderPulseDuration = TimeSpan.Zero,
            LoopPeriod = TimeSpan.FromMilliseconds(10)
        };
        var viewModel = new OutputTestViewModel(ioService, lineControlService);
        await viewModel.ConnectCommand.ExecuteAsync(null);
        var startResult = await lineControlService.StartAsync();
        Assert.True(startResult.IsSuccess, startResult.Message);
        driver.Writes.Clear();
        driver.ModuleWrites.Clear();
        var point = FirstEnabledOutput(viewModel);

        viewModel.IsManualMode = true;
        await viewModel.SelectOutputPointCommand.ExecuteAsync(point);

        Assert.False(viewModel.IsManualMode);
        Assert.Empty(driver.Writes);
        Assert.Empty(driver.ModuleWrites.ToArray());
        Assert.Contains("自动运行中", viewModel.StatusText, StringComparison.Ordinal);
        await lineControlService.StopAsync();
    }

    private static OutputTestViewModel CreateViewModel(
        out RecordingIoBoardDriver driver,
        out RecordingDriverFactory factory,
        bool simulationMode,
        IIoPointMapProvider? provider = null)
    {
        driver = new RecordingIoBoardDriver();
        factory = new RecordingDriverFactory(driver);
        var service = CreateIoService(factory, simulationMode, provider);
        return new OutputTestViewModel(service);
    }

    private static IoBoardService CreateIoService(
        RecordingIoBoardDriver driver,
        bool simulationMode,
        IIoPointMapProvider? provider = null)
    {
        return CreateIoService(new RecordingDriverFactory(driver), simulationMode, provider);
    }

    private static IoBoardService CreateIoService(
        RecordingDriverFactory factory,
        bool simulationMode,
        IIoPointMapProvider? provider = null)
    {
        return new IoBoardService(
            factory,
            provider ?? new JsonIoPointMapProvider("missing-io-points.json"),
            new TestSettingsService(settings => settings.Advanced.SimulationMode = simulationMode));
    }

    private static IoPointViewModel FirstEnabledOutput(OutputTestViewModel viewModel)
    {
        return viewModel.OutputModules
            .SelectMany(module => module.Points)
            .First(point => point.IsEnabled);
    }

    private static IoPointViewModel OutputPoint(OutputTestViewModel viewModel, int pointNo)
    {
        return viewModel.OutputModules
            .SelectMany(module => module.Points)
            .Single(point => point.PointNo == pointNo);
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
