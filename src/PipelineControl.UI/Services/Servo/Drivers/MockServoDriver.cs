using PipelineControl.UI.Services.Io;
using PipelineControl.UI.Services.Servo.Mapping;

namespace PipelineControl.UI.Services.Servo.Drivers;

public sealed class MockServoDriver : IServoDriver, IConfigurableServoDriver
{
    private readonly object syncRoot = new();
    private readonly Dictionary<int, MockAxisRuntime> axesByStation = new();
    private bool connected;

    public string DriverName => "Mock HeChuan Modbus";

    public void Configure(ServoConnectionOptions options)
    {
        // Mock 驱动不需要网络参数，保留该入口以便和真实驱动统一装配。
    }

    public ApiResult Connect(string gatewayIp, int gatewayPort)
    {
        lock (syncRoot)
        {
            connected = true;
        }

        return ApiResult.Ok("Mock 伺服网关已连接");
    }

    public ApiResult Disconnect()
    {
        lock (syncRoot)
        {
            connected = false;
            foreach (var runtime in axesByStation.Values)
            {
                runtime.IsEnabled = false;
                runtime.TargetRpm = 0;
            }
        }

        return ApiResult.Ok("Mock 伺服网关已断开");
    }

    public ApiResult<ServoAxisState> ReadAxis(ServoAxisDefinition axis, ServoRegisterMap registerMap)
    {
        lock (syncRoot)
        {
            if (!connected)
            {
                return ApiResult<ServoAxisState>.Fail(-1, "Mock ReadAxis");
            }

            var runtime = GetOrCreate(axis.Station);
            var actualRpm = runtime.IsEnabled ? runtime.TargetRpm : 0;
            return ApiResult<ServoAxisState>.Ok(new ServoAxisState(
                axis.Axis,
                axis.Station,
                axis.Name,
                runtime.IsEnabled,
                IsOnline: true,
                TargetRpm: runtime.TargetRpm,
                ActualRpm: actualRpm,
                FaultCode: runtime.FaultCode));
        }
    }

    public ApiResult WriteServoOn(ServoAxisDefinition axis, ServoRegisterMap registerMap, bool enable)
    {
        lock (syncRoot)
        {
            if (!connected)
            {
                return ApiResult.Fail(-1, "Mock WriteServoOn");
            }

            if (!registerMap.TryGetRegister("ServoOn", out var register))
            {
                return ApiResult.Fail(7, $"未配置 ServoOn 寄存器");
            }

            var runtime = GetOrCreate(axis.Station);
            runtime.IsEnabled = enable;
            if (!enable)
            {
                runtime.TargetRpm = 0;
            }

            return ApiResult.Ok();
        }
    }

    public ApiResult WriteSpeed(ServoAxisDefinition axis, ServoRegisterMap registerMap, int rpm)
    {
        lock (syncRoot)
        {
            if (!connected)
            {
                return ApiResult.Fail(-1, "Mock WriteSpeed");
            }

            if (!registerMap.TryGetRegister("SpeedCommand", out var register))
            {
                return ApiResult.Fail(7, "未配置 SpeedCommand 寄存器");
            }

            var runtime = GetOrCreate(axis.Station);
            if (!runtime.IsEnabled)
            {
                return ApiResult.Fail(7, $"{axis.Name} 未使能，禁止写转速");
            }

            runtime.TargetRpm = rpm;
            return ApiResult.Ok();
        }
    }

    public void InjectFault(int station, int faultCode)
    {
        lock (syncRoot)
        {
            GetOrCreate(station).FaultCode = faultCode;
        }
    }

    public void ClearFault(int station)
    {
        lock (syncRoot)
        {
            GetOrCreate(station).FaultCode = 0;
        }
    }

    private MockAxisRuntime GetOrCreate(int station)
    {
        if (!axesByStation.TryGetValue(station, out var runtime))
        {
            runtime = new MockAxisRuntime();
            axesByStation[station] = runtime;
        }

        return runtime;
    }

    private sealed class MockAxisRuntime
    {
        public bool IsEnabled { get; set; }

        public int TargetRpm { get; set; }

        public int FaultCode { get; set; }
    }
}
