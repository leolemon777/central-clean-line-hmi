namespace PipelineControl.UI.Services.Servo;

public sealed record ServoAxisState(
    int Axis,
    int Station,
    string Name,
    bool IsEnabled,
    bool IsOnline,
    int TargetRpm,
    int ActualRpm,
    int FaultCode)
{
    public bool HasFault => FaultCode != 0;

    public string FaultText => FaultCode == 0 ? string.Empty : $"Err.{FaultCode:D3}";
}

public sealed record ServoSnapshot(
    bool IsConnected,
    string DriverName,
    string GatewayIp,
    int ScanCycleMs,
    int ConsecutiveFailureCount,
    string LastError,
    IReadOnlyList<ServoAxisState> Axes)
{
    public IReadOnlyList<ServoAxisState> EnabledAxes => Axes.Where(axis => axis.IsEnabled).ToList();
}

public sealed record ServoLogEntry(DateTime Time, string Level, string Message)
{
    public string TimeText => Time.ToString("HH:mm:ss.fff");
}

public sealed record ServoAlarm(DateTime Time, string Code, string Message);
