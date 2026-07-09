namespace PipelineControl.Shared.Configuration;

public sealed class BopaiCardOptions
{
    public const string SectionName = "BopaiCard";

    public bool UseSimulator { get; set; }

    public string PcIp { get; set; } = string.Empty;

    public string MainCardIp { get; set; } = string.Empty;

    public int ExpansionCardCount { get; set; }

    public int ScanCycleMs { get; set; }

    public int KeepAliveMs { get; set; }
}
