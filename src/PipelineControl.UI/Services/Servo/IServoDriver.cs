using PipelineControl.UI.Services.Io;
using PipelineControl.UI.Services.Servo.Mapping;

namespace PipelineControl.UI.Services.Servo;

public interface IServoDriver
{
    string DriverName { get; }

    ApiResult Connect(string gatewayIp, int gatewayPort);

    ApiResult Disconnect();

    ApiResult<ServoAxisState> ReadAxis(ServoAxisDefinition axis, ServoRegisterMap registerMap);

    ApiResult WriteServoOn(ServoAxisDefinition axis, ServoRegisterMap registerMap, bool enable);

    ApiResult WriteSpeed(ServoAxisDefinition axis, ServoRegisterMap registerMap, int rpm);
}

public interface IConfigurableServoDriver
{
    void Configure(ServoConnectionOptions options);
}
