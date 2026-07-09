namespace PipelineControl.UI.Services.Io;

public sealed record IoBoardLogEntry(DateTime Time, string Level, string Message)
{
    public string TimeText => Time.ToString("HH:mm:ss.fff");
}

public sealed record IoBoardAlarm(DateTime Time, string Code, string Message);

public sealed record IoPointState(IoPointDefinition Definition, bool IsOn, bool IsForced = false);

public sealed record IoBoardSnapshot(
    bool IsConnected,
    string DriverName,
    string PcIp,
    string MainCardIp,
    int ScanCycleMs,
    int ConsecutiveFailureCount,
    string LastError,
    IReadOnlyList<IoPointState> Inputs,
    IReadOnlyList<IoPointState> Outputs);
