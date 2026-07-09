namespace PipelineControl.UI.Services.Settings;

public interface ISettingsService
{
    string AppSettingsPath { get; }

    string LocalSettingsPath { get; }

    Task<SystemSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task<SystemSettings> LoadDefaultsAsync(CancellationToken cancellationToken = default);

    Task SaveLocalAsync(SystemSettings settings, CancellationToken cancellationToken = default);

    Task ResetLocalAsync(CancellationToken cancellationToken = default);

    Task<string> BackupAsync(string targetDirectory, CancellationToken cancellationToken = default);

    Task RestoreAsync(string backupFilePath, CancellationToken cancellationToken = default);
}
