namespace PipelineControl.UI.Services.Io;

public interface IIoBoardDriver
{
    string DriverName { get; }

    ApiResult Connect(string pcIp);

    ApiResult Disconnect();

    ApiResult Reset();

    ApiResult<bool> ReadInputBit(int moduleIndex, int bitIndex);

    ApiResult WriteOutputBit(int moduleIndex, int bitIndex, bool value);

    ApiResult WriteOutputModule(int moduleIndex, int value);

    ApiResult<bool> ReadOutputBit(int moduleIndex, int bitIndex);

    ApiResult<IReadOnlyList<IoModuleImage>> ReadAllInputs(int startModuleIndex, int moduleCount);

    ApiResult<IReadOnlyList<IoModuleImage>> ReadAllOutputs(int startModuleIndex, int moduleCount);

    ApiResult<double> ReadAdcVoltage(int channel);

    ApiResult WriteDacVoltage(int channel, double voltage);
}

public interface IConfigurableIoBoardDriver
{
    void Configure(IoBoardConnectionOptions options);
}
