namespace PipelineControl.Shared.Configuration;

public sealed class AppOptions
{
    public const string SectionName = "App";

    public string ApplicationName { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string StationName { get; set; } = string.Empty;

    public string LineName { get; set; } = string.Empty;

    public int IdleTimeoutMinutes { get; set; }

    public int DataRetentionDays { get; set; }
}
