using PipelineControl.UI.Services.Settings;
using System.IO;
using Xunit;

namespace PipelineControl.UI.Tests;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string tempRoot = Path.Combine(Path.GetTempPath(), $"PipelineSettings-{Guid.NewGuid():N}");

    public SettingsServiceTests()
    {
        Directory.CreateDirectory(tempRoot);
    }

    [Fact]
    public async Task Load_creates_default_appsettings_and_save_writes_local_override()
    {
        var service = new JsonSettingsService(tempRoot);

        var settings = await service.LoadAsync();
        settings.CardComm.ScanCycleMs = 40;
        await service.SaveLocalAsync(settings);
        var reloaded = await service.LoadAsync();

        Assert.True(File.Exists(service.AppSettingsPath));
        Assert.True(File.Exists(service.LocalSettingsPath));
        Assert.Equal(40, reloaded.CardComm.ScanCycleMs);
    }

    [Fact]
    public async Task Defaults_match_vendor_demo_card_connection()
    {
        var service = new JsonSettingsService(tempRoot);

        var settings = await service.LoadAsync();

        Assert.Equal("192.168.0.200", settings.CardComm.PcIp);
        Assert.Equal("192.168.0.1", settings.CardComm.MainCardIp);
        Assert.Equal(2, settings.CardComm.ExtensionCardCount);
        Assert.Equal(200, settings.CardComm.ScanCycleMs);
        Assert.Equal(1000, settings.CardComm.HeartbeatMs);
        Assert.False(settings.Advanced.SimulationMode);
    }

    [Fact]
    public async Task Backup_and_restore_roundtrip_local_settings()
    {
        var service = new JsonSettingsService(tempRoot);
        var settings = await service.LoadAsync();
        settings.General.ApplicationName = "测试控制台";
        await service.SaveLocalAsync(settings);
        var backupPath = await service.BackupAsync(Path.Combine(tempRoot, "backup"));

        settings.General.ApplicationName = "临时名称";
        await service.SaveLocalAsync(settings);
        await service.RestoreAsync(backupPath);
        var restored = await service.LoadAsync();

        Assert.True(File.Exists(backupPath));
        Assert.Equal("测试控制台", restored.General.ApplicationName);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
