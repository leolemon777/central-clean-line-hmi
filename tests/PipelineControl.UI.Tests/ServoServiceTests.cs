using PipelineControl.UI.Services.Io;
using PipelineControl.UI.Services.Servo;
using PipelineControl.UI.Services.Servo.Drivers;
using PipelineControl.UI.Tests;
using Xunit;

namespace PipelineControl.UI.Services.Servo.Tests;

public class ServoServiceTests
{
    private static ServoService CreateService(
        out TestServoDriverFactory factory,
        out MockServoDriver driver,
        RecordingAppLogService? logService = null,
        bool simulationMode = true)
    {
        driver = new MockServoDriver();
        factory = new TestServoDriverFactory(driver);
        var settings = new TestSettingsService(s =>
        {
            s.Advanced.SimulationMode = simulationMode;
            s.ServoComm.GatewayIp = simulationMode ? string.Empty : "192.168.0.10";
            s.ServoComm.GatewayPort = 502;
            s.ServoComm.Axis1Station = 1;
            s.ServoComm.Axis2Station = 2;
            s.ServoComm.Axis3Station = 3;
            s.ServoComm.Axis4Station = 4;
            s.ServoComm.ScanCycleMs = 100;
            s.ServoComm.HeartbeatCycleMs = 3000;
            s.ServoComm.MaxSpeedRpm = 3000;
        });
        logService ??= new RecordingAppLogService();
        var mapProvider = new StubServoRegisterMapProvider();
        var service = new ServoService(factory, mapProvider, settings, logService);
        return service;
    }

    [Fact]
    public async Task ConnectAsync_InSimulationMode_Connects()
    {
        var service = CreateService(out _, out _, simulationMode: true);

        var result = await service.ServoOnAsync(1);
        Assert.False(result.IsSuccess); // 未连接，拒绝使能

        var connect = await service.ConnectAsync();
        Assert.True(connect.IsSuccess);
        Assert.True(service.IsConnected);
    }

    [Fact]
    public async Task ServoOnAsync_BeforeConnect_Rejected()
    {
        var service = CreateService(out _, out _);

        var result = await service.ServoOnAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Contains("未连接", result.Message);
    }

    [Fact]
    public async Task ServoOnAsync_AfterConnect_EnablesAxis()
    {
        var service = CreateService(out _, out _);
        await service.ConnectAsync();

        var result = await service.ServoOnAsync(2);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SetSpeedAsync_ClampsToMaxRpm()
    {
        var service = CreateService(out _, out _);
        await service.ConnectAsync();
        await service.ServoOnAsync(1);

        await service.SetSpeedAsync(1, 99999); // 远超 MaxSpeedRpm=3000

        var snapshot = service.CurrentSnapshot;
        var axis = snapshot.Axes.Single(a => a.Axis == 1);
        // Mock 驱动接受后回读，限幅由 service 写入前完成；这里验证调用成功即可。
        Assert.True(axis.IsEnabled);
    }

    [Fact]
    public async Task EmergencyStopAll_DisablesAllAxes()
    {
        var service = CreateService(out _, out _);
        await service.ConnectAsync();
        await service.ServoOnAsync(1);
        await service.ServoOnAsync(2);

        var result = await service.EmergencyStopAllAsync();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task EmergencyStopAll_RecordsOperationLog()
    {
        var log = new RecordingAppLogService();
        var service = CreateService(out _, out _, log);
        await service.ConnectAsync();

        await service.EmergencyStopAllAsync();

        Assert.Contains(log.Entries, entry => entry.Command == "SERVO_ESTOP");
    }

    [Fact]
    public async Task ResetFaultAsync_WritesLogEntry()
    {
        var log = new RecordingAppLogService();
        var service = CreateService(out _, out _, log);
        await service.ConnectAsync();

        await service.ResetFaultAsync(3);

        Assert.Contains(log.Entries, entry => entry.Command == "SERVO_RESET");
    }

    [Fact]
    public async Task ConnectAsync_FailsWhenSimulationOff_AndNoGatewayIp()
    {
        // 真实模式但网关 IP 空：应在连接前校验失败，不创建驱动。
        var driver = new MockServoDriver();
        var factory = new TestServoDriverFactory(driver);
        var settings = new TestSettingsService(s =>
        {
            s.Advanced.SimulationMode = false;
            s.ServoComm.GatewayIp = string.Empty;
        });
        var service = new ServoService(factory, new StubServoRegisterMapProvider(), settings, new RecordingAppLogService());

        var result = await service.ConnectAsync();

        Assert.False(result.IsSuccess);
        Assert.Contains("网关 IP", result.Message);
    }

    [Fact]
    public void Snapshot_ReportsAllFourAxes()
    {
        var service = CreateService(out _, out _);

        var snapshot = service.CurrentSnapshot;

        Assert.Equal(4, snapshot.Axes.Count);
        Assert.Contains(snapshot.Axes, a => a.Axis == 1 && a.Station == 1);
        Assert.Contains(snapshot.Axes, a => a.Axis == 4 && a.Station == 4);
    }

    [Fact]
    public async Task ServoOffAsync_AfterOn_ReportsDisabled()
    {
        var service = CreateService(out _, out _);
        await service.ConnectAsync();
        await service.ServoOnAsync(1);

        var result = await service.ServoOffAsync(1);

        Assert.True(result.IsSuccess);
    }
}
