namespace PipelineControl.UI.Services.Servo;

public sealed record ServoConnectionOptions
{
    public string GatewayIp { get; init; } = string.Empty;

    public int GatewayPort { get; init; } = 502;

    public IReadOnlyList<int> AxisStations { get; init; } = new[] { 1, 2, 3, 4 };

    public int ScanCycleMs { get; init; } = 100;

    public int HeartbeatCycleMs { get; init; } = 3000;

    public int DefaultSpeedRpm { get; init; }

    public int MaxSpeedRpm { get; init; } = 3000;

    public bool UseRealDriver { get; init; }
}
