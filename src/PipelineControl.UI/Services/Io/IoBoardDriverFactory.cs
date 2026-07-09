using PipelineControl.UI.Services.Io.Drivers;

namespace PipelineControl.UI.Services.Io;

public interface IIoBoardDriverFactory
{
    IIoBoardDriver Create(IoBoardConnectionOptions options);
}

public sealed class IoBoardDriverFactory : IIoBoardDriverFactory
{
    public IIoBoardDriver Create(IoBoardConnectionOptions options)
    {
        IIoBoardDriver driver = options.UseRealDriver
            ? new RealIoBoardDriver()
            : new MockIoBoardDriver();

        if (driver is IConfigurableIoBoardDriver configurable)
        {
            configurable.Configure(options);
        }

        return driver;
    }
}
