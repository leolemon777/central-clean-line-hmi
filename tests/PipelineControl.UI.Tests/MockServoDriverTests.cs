using PipelineControl.UI.Services.Servo.Drivers;
using PipelineControl.UI.Services.Servo.Mapping;
using PipelineControl.UI.Tests;
using Xunit;

namespace PipelineControl.UI.Services.Servo.Mapping.Tests;

public class ServoRegisterMapTests
{
    [Fact]
    public void TryGetRegister_ReturnsDefinition_WhenKeyExists()
    {
        var map = StubServoRegisterMapProvider.CreateDefault();

        var found = map.TryGetRegister("ServoOn", out var definition);

        Assert.True(found);
        Assert.Equal("3607", definition!.Address);
        Assert.Equal(2, definition.OnValue);
        Assert.Equal(0, definition.OffValue);
    }

    [Fact]
    public void TryGetRegister_ReturnsFalse_WhenKeyMissing()
    {
        var map = StubServoRegisterMapProvider.CreateDefault();

        Assert.False(map.TryGetRegister("Missing", out _));
    }

    [Fact]
    public void AddressValue_ParsesHexAddress()
    {
        var definition = new ServoRegisterDefinition("0324", null, null, string.Empty);

        Assert.Equal(0x0324, definition.AddressValue);
    }

    [Fact]
    public void AddressValue_ParsesPrefixedHexAddress()
    {
        var definition = new ServoRegisterDefinition("0x3607", null, null, string.Empty);

        Assert.Equal(0x3607, definition.AddressValue);
    }

    [Fact]
    public void HasWritableValue_TrueWhenOnValueSet()
    {
        Assert.True(new ServoRegisterDefinition("3607", 2, null, string.Empty).HasWritableValue);
        Assert.True(new ServoRegisterDefinition("3607", null, 0, string.Empty).HasWritableValue);
        Assert.False(new ServoRegisterDefinition("3607", null, null, string.Empty).HasWritableValue);
    }
}

public class MockServoDriverTests
{
    private static ServoRegisterMap DefaultMap => StubServoRegisterMapProvider.CreateDefault();

    [Fact]
    public void Connect_ThenReadAxis_ReportsOnline()
    {
        var driver = new MockServoDriver();
        var axis = new ServoAxisDefinition(1, "1#伺服", 1);
        driver.Connect("127.0.0.1", 502);

        var state = driver.ReadAxis(axis, DefaultMap);

        Assert.True(state.IsSuccess);
        Assert.True(state.Value!.IsOnline);
        Assert.False(state.Value.IsEnabled);
    }

    [Fact]
    public void ReadAxis_FailsBeforeConnect()
    {
        var driver = new MockServoDriver();
        var axis = new ServoAxisDefinition(1, "1#伺服", 1);

        var result = driver.ReadAxis(axis, DefaultMap);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void WriteServoOn_EnablesAxis()
    {
        var driver = new MockServoDriver();
        var axis = new ServoAxisDefinition(1, "1#伺服", 1);
        driver.Connect("127.0.0.1", 502);

        var writeResult = driver.WriteServoOn(axis, DefaultMap, enable: true);
        var state = driver.ReadAxis(axis, DefaultMap);

        Assert.True(writeResult.IsSuccess);
        Assert.True(state.Value!.IsEnabled);
    }

    [Fact]
    public void WriteSpeed_RejectedBeforeEnable()
    {
        var driver = new MockServoDriver();
        var axis = new ServoAxisDefinition(1, "1#伺服", 1);
        driver.Connect("127.0.0.1", 502);

        var result = driver.WriteSpeed(axis, DefaultMap, 300);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void WriteSpeed_AcceptedAfterEnable()
    {
        var driver = new MockServoDriver();
        var axis = new ServoAxisDefinition(1, "1#伺服", 1);
        driver.Connect("127.0.0.1", 502);
        driver.WriteServoOn(axis, DefaultMap, enable: true);

        var result = driver.WriteSpeed(axis, DefaultMap, 500);

        Assert.True(result.IsSuccess);
        var state = driver.ReadAxis(axis, DefaultMap);
        Assert.Equal(500, state.Value!.TargetRpm);
    }

    [Fact]
    public void Disconnect_TurnsOffAllAxes()
    {
        var driver = new MockServoDriver();
        var axis = new ServoAxisDefinition(1, "1#伺服", 1);
        driver.Connect("127.0.0.1", 502);
        driver.WriteServoOn(axis, DefaultMap, enable: true);

        driver.Disconnect();
        driver.Connect("127.0.0.1", 502);

        var state = driver.ReadAxis(axis, DefaultMap);
        Assert.False(state.Value!.IsEnabled);
    }
}
