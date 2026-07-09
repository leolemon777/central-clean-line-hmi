using PipelineControl.UI.Services.Servo.Drivers;

namespace PipelineControl.UI.Services.Servo;

public interface IServoDriverFactory
{
    IServoDriver Create(ServoConnectionOptions options);
}

public sealed class ServoDriverFactory : IServoDriverFactory
{
    public IServoDriver Create(ServoConnectionOptions options)
    {
        IServoDriver driver = options.UseRealDriver
            ? new RealServoDriver()
            : new MockServoDriver();

        if (driver is IConfigurableServoDriver configurable)
        {
            configurable.Configure(options);
        }

        return driver;
    }
}
