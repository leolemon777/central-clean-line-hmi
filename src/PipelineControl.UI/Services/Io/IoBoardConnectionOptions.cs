namespace PipelineControl.UI.Services.Io;

public sealed record IoBoardConnectionOptions
{
    public string PcIp { get; init; } = "192.168.0.200";

    public string MainCardIp { get; init; } = "192.168.0.1";

    public ushort PcPort { get; init; } = 60000;

    public ushort MainCardPort { get; init; } = 60000;

    public int ScanCycleMs { get; init; } = 200;

    public bool UseRealDriver { get; init; }
}
