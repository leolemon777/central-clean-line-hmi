namespace PipelineControl.UI.Services.Settings;

public sealed class SystemSettings
{
    public GeneralSettings General { get; set; } = new();

    public CardCommSettings CardComm { get; set; } = new();

    public DatabaseSettings Database { get; set; } = new();

    public ThemeSettings Theme { get; set; } = new();

    public BackupRestoreSettings BackupRestore { get; set; } = new();

    public ServoCommSettings ServoComm { get; set; } = new();

    public AdvancedSettings Advanced { get; set; } = new();

    public static SystemSettings CreateDefaults()
    {
        return new SystemSettings
        {
            General = new GeneralSettings
            {
                ApplicationName = "中央净软线控制台",
                IdleTimeoutMinutes = 15,
                Language = "简体中文"
            },
            CardComm = new CardCommSettings
            {
                PcIp = "192.168.0.200",
                MainCardIp = "192.168.0.1",
                ExtensionCardCount = 2,
                ScanCycleMs = 200,
                HeartbeatMs = 1000
            },
            Database = new DatabaseSettings
            {
                RetentionDays = 0,
                BackupPeriod = string.Empty,
                BackupPath = string.Empty
            },
            Theme = new ThemeSettings
            {
                ThemeMode = "亮色",
                FontScale = "标准"
            },
            BackupRestore = new BackupRestoreSettings
            {
                ManualBackupPath = string.Empty,
                RestoreFilePath = string.Empty,
                ConfigExportPath = string.Empty
            },
            ServoComm = new ServoCommSettings
            {
                GatewayIp = string.Empty,
                GatewayPort = 502,
                Axis1Station = 1,
                Axis2Station = 2,
                Axis3Station = 3,
                Axis4Station = 4,
                ScanCycleMs = 100,
                HeartbeatCycleMs = 3000,
                DefaultSpeedRpm = 0,
                MaxSpeedRpm = 3000
            },
            Advanced = new AdvancedSettings
            {
                SimulationMode = false,
                LogLevel = "Info"
            }
        };
    }
}

public sealed class GeneralSettings
{
    public string ApplicationName { get; set; } = string.Empty;

    public int IdleTimeoutMinutes { get; set; }

    public string Language { get; set; } = string.Empty;
}

public sealed class CardCommSettings
{
    public string PcIp { get; set; } = string.Empty;

    public string MainCardIp { get; set; } = string.Empty;

    public int ExtensionCardCount { get; set; }

    public int ScanCycleMs { get; set; }

    public int HeartbeatMs { get; set; }
}

public sealed class DatabaseSettings
{
    public int RetentionDays { get; set; }

    public string BackupPeriod { get; set; } = string.Empty;

    public string BackupPath { get; set; } = string.Empty;
}

public sealed class ThemeSettings
{
    public string ThemeMode { get; set; } = string.Empty;

    public string FontScale { get; set; } = string.Empty;
}

public sealed class BackupRestoreSettings
{
    public string ManualBackupPath { get; set; } = string.Empty;

    public string RestoreFilePath { get; set; } = string.Empty;

    public string ConfigExportPath { get; set; } = string.Empty;
}

public sealed class ServoCommSettings
{
    public string GatewayIp { get; set; } = string.Empty;

    public int GatewayPort { get; set; } = 502;

    public int Axis1Station { get; set; } = 1;

    public int Axis2Station { get; set; } = 2;

    public int Axis3Station { get; set; } = 3;

    public int Axis4Station { get; set; } = 4;

    public int ScanCycleMs { get; set; } = 100;

    public int HeartbeatCycleMs { get; set; } = 3000;

    public int DefaultSpeedRpm { get; set; }

    public int MaxSpeedRpm { get; set; } = 3000;
}

public sealed class AdvancedSettings
{
    public bool SimulationMode { get; set; }

    public string LogLevel { get; set; } = string.Empty;
}


