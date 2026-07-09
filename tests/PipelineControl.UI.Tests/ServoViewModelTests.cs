using PipelineControl.UI.Services.Io;
using PipelineControl.UI.Services.Servo;
using PipelineControl.UI.Services.Servo.Drivers;
using PipelineControl.UI.ViewModels.Pages.Servo;
using PipelineControl.UI.Tests;
using Xunit;

namespace PipelineControl.UI.Tests;

public class ServoViewModelTests
{
    [Fact]
    public void Constructor_CreatesFourAxes()
    {
        var viewModel = CreateViewModel(out _, out _);

        Assert.Equal(4, viewModel.Axes.Count);
        Assert.Equal("1#伺服", viewModel.Axes[0].Name);
        Assert.Equal("4#伺服", viewModel.Axes[3].Name);
    }

    [Fact]
    public void Initial_State_Unconnected()
    {
        var viewModel = CreateViewModel(out _, out _);

        Assert.False(viewModel.IsConnected);
        Assert.Contains("未连接", viewModel.StatusText);
        Assert.True(viewModel.IsJogMode);
        Assert.False(viewModel.IsSyncAll);
        Assert.All(viewModel.Axes, axis => Assert.Equal("100", axis.SpeedInputText));
    }

    [Fact]
    public void ToggleJogMode_UpdatesModeText()
    {
        var viewModel = CreateViewModel(out _, out _);

        Assert.Equal("点动", viewModel.JogModeText);

        viewModel.ToggleJogModeCommand.Execute(null);

        Assert.False(viewModel.IsJogMode);
        Assert.Equal("连续", viewModel.JogModeText);
        Assert.Contains("连续", viewModel.StatusText);
    }

    [Fact]
    public void ToggleSyncAll_UpdatesModeText()
    {
        var viewModel = CreateViewModel(out _, out _);

        Assert.Equal("单轴", viewModel.SyncAllText);

        viewModel.ToggleSyncAllCommand.Execute(null);

        Assert.True(viewModel.IsSyncAll);
        Assert.Equal("同步开", viewModel.SyncAllText);
        Assert.Contains("同步", viewModel.StatusText);
    }

    [Fact]
    public async Task ConnectAsync_UpdatesConnectedState()
    {
        var viewModel = CreateViewModel(out _, out _);

        await viewModel.ConnectCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsConnected);
    }

    [Fact]
    public async Task ServoOnAsync_UpdatesStatus()
    {
        var viewModel = CreateViewModel(out _, out _);
        await viewModel.ConnectCommand.ExecuteAsync(null);
        var axis = viewModel.Axes[1];

        await viewModel.ServoOnCommand.ExecuteAsync(axis);

        // 命令成功后 StatusText 同步更新；axis.IsEnabled 由轮询快照异步刷新，不在此断言。
        Assert.Contains("已使能", viewModel.StatusText);
    }

    [Fact]
    public async Task EmergencyStop_DoesNotThrow()
    {
        var viewModel = CreateViewModel(out _, out _);
        await viewModel.ConnectCommand.ExecuteAsync(null);

        await viewModel.EmergencyStopCommand.ExecuteAsync(null);

        Assert.NotEmpty(viewModel.StatusText);
    }

    [Fact]
    public async Task ForwardCommand_RunsForward_WithPositiveSpeed()
    {
        var viewModel = CreateViewModel(out _, out _);
        await viewModel.ConnectCommand.ExecuteAsync(null);
        var axis = viewModel.Axes[0];
        axis.SpeedInputText = "300";

        await viewModel.ForwardCommand.ExecuteAsync(axis);

        Assert.Contains("正转", viewModel.StatusText);
        Assert.Equal(ViewModels.Pages.Servo.Models.ServoRunDirection.Forward, axis.Direction);
    }

    [Fact]
    public async Task ReverseCommand_RunsReverse_WithNegativeSpeed()
    {
        var viewModel = CreateViewModel(out _, out _);
        await viewModel.ConnectCommand.ExecuteAsync(null);
        var axis = viewModel.Axes[0];
        axis.SpeedInputText = "300";

        await viewModel.ReverseCommand.ExecuteAsync(axis);

        Assert.Contains("反转", viewModel.StatusText);
        Assert.Equal(ViewModels.Pages.Servo.Models.ServoRunDirection.Reverse, axis.Direction);
    }

    [Fact]
    public async Task StopCommand_ClearsDirection()
    {
        var viewModel = CreateViewModel(out _, out _);
        await viewModel.ConnectCommand.ExecuteAsync(null);
        var axis = viewModel.Axes[0];
        axis.SpeedInputText = "300";
        await viewModel.ForwardCommand.ExecuteAsync(axis);

        await viewModel.StopAxisCommand.ExecuteAsync(axis);

        Assert.Equal(ViewModels.Pages.Servo.Models.ServoRunDirection.Stopped, axis.Direction);
    }

    [Fact]
    public async Task AutoMode_BlocksManualAxisCommand()
    {
        var viewModel = CreateViewModel(out _, out _);
        await viewModel.ConnectCommand.ExecuteAsync(null);
        viewModel.ToggleAutoModeCommand.Execute(null);
        var axis = viewModel.Axes[0];

        await viewModel.ForwardCommand.ExecuteAsync(axis);

        Assert.Contains("自动模式", viewModel.StatusText);
        Assert.Equal(ViewModels.Pages.Servo.Models.ServoRunDirection.Stopped, axis.Direction);
    }

    [Fact]
    public async Task EmergencyStop_ClearsAllDirections()
    {
        var viewModel = CreateViewModel(out _, out _);
        await viewModel.ConnectCommand.ExecuteAsync(null);
        var axis = viewModel.Axes[0];
        axis.SpeedInputText = "300";
        await viewModel.ForwardCommand.ExecuteAsync(axis);

        await viewModel.EmergencyStopCommand.ExecuteAsync(null);

        Assert.All(viewModel.Axes, a => Assert.Equal(ViewModels.Pages.Servo.Models.ServoRunDirection.Stopped, a.Direction));
    }

    [Fact]
    public void AxisViewModel_HasFault_ReflectsFaultCode()
    {
        var axis = new ViewModels.Pages.Servo.Models.ServoAxisViewModel(1, "1#伺服", 1);

        Assert.False(axis.HasFault);

        axis.ApplyState(true, true, 0, 0, faultCode: 23);

        Assert.True(axis.HasFault);
        Assert.Equal("Err.023", axis.FaultText);
        Assert.Equal("Err.023", axis.StatusText);
    }

    [Fact]
    public void AxisViewModel_CanToggle_ReflectsBusy()
    {
        var axis = new ViewModels.Pages.Servo.Models.ServoAxisViewModel(1, "1#伺服", 1);

        Assert.True(axis.CanToggle);

        axis.IsBusy = true;

        Assert.False(axis.CanToggle);
    }

    private static ServoViewModel CreateViewModel(out MockServoDriver driver, out RecordingAppLogService log)
    {
        driver = new MockServoDriver();
        log = new RecordingAppLogService();
        var factory = new TestServoDriverFactory(driver);
        var settings = new TestSettingsService(s =>
        {
            s.Advanced.SimulationMode = true;
            s.ServoComm.ScanCycleMs = 100;
            s.ServoComm.MaxSpeedRpm = 3000;
        });
        var mapProvider = new StubServoRegisterMapProvider();
        var service = new ServoService(factory, mapProvider, settings, log);
        return new ServoViewModel(service);
    }
}
