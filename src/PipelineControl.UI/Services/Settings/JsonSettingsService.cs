using System.IO;
using System.Text.Json;

namespace PipelineControl.UI.Services.Settings;

public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly SemaphoreSlim gate = new(1, 1);

    public JsonSettingsService()
        : this(AppContext.BaseDirectory)
    {
    }

    public JsonSettingsService(string rootDirectory)
    {
        AppSettingsPath = Path.Combine(rootDirectory, "appsettings.json");
        LocalSettingsPath = Path.Combine(rootDirectory, "appsettings.local.json");
    }

    public string AppSettingsPath { get; }

    public string LocalSettingsPath { get; }

    public async Task<SystemSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var defaults = await ReadOrCreateDefaultsAsync(cancellationToken).ConfigureAwait(false);
            if (!File.Exists(LocalSettingsPath))
            {
                return Clone(defaults);
            }

            var local = await ReadDocumentAsync(LocalSettingsPath, cancellationToken).ConfigureAwait(false);
            return local.SystemSettings ?? Clone(defaults);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<SystemSettings> LoadDefaultsAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return Clone(await ReadOrCreateDefaultsAsync(cancellationToken).ConfigureAwait(false));
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task SaveLocalAsync(SystemSettings settings, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await ReadOrCreateDefaultsAsync(cancellationToken).ConfigureAwait(false);
            await WriteDocumentAtomicAsync(LocalSettingsPath, new SettingsDocument { SystemSettings = Clone(settings) }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task ResetLocalAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (File.Exists(LocalSettingsPath))
            {
                File.Delete(LocalSettingsPath);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<string> BackupAsync(string targetDirectory, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var resolvedDirectory = ExpandPath(targetDirectory);
            Directory.CreateDirectory(resolvedDirectory);

            var settings = File.Exists(LocalSettingsPath)
                ? await ReadDocumentAsync(LocalSettingsPath, cancellationToken).ConfigureAwait(false)
                : new SettingsDocument { SystemSettings = await ReadOrCreateDefaultsAsync(cancellationToken).ConfigureAwait(false) };

            var filePath = Path.Combine(resolvedDirectory, $"appsettings.local-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            await WriteDocumentAtomicAsync(filePath, settings, cancellationToken).ConfigureAwait(false);
            return filePath;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task RestoreAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(ExpandPath(backupFilePath)))
            {
                throw new FileNotFoundException("未找到要还原的配置文件。", backupFilePath);
            }

            var backup = await ReadDocumentAsync(ExpandPath(backupFilePath), cancellationToken).ConfigureAwait(false);
            if (backup.SystemSettings is null)
            {
                throw new InvalidDataException("配置文件缺少 SystemSettings 节点。");
            }

            await WriteDocumentAtomicAsync(LocalSettingsPath, backup, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<SystemSettings> ReadOrCreateDefaultsAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(AppSettingsPath))
        {
            var document = await ReadDocumentAsync(AppSettingsPath, cancellationToken).ConfigureAwait(false);
            return document.SystemSettings ?? SystemSettings.CreateDefaults();
        }

        var defaults = SystemSettings.CreateDefaults();
        await WriteDocumentAtomicAsync(AppSettingsPath, new SettingsDocument { SystemSettings = defaults }, cancellationToken).ConfigureAwait(false);
        return defaults;
    }

    private static async Task<SettingsDocument> ReadDocumentAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<SettingsDocument>(stream, JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? new SettingsDocument();
    }

    private static async Task WriteDocumentAtomicAsync(string filePath, SettingsDocument document, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(filePath);
        Directory.CreateDirectory(string.IsNullOrWhiteSpace(directory) ? AppContext.BaseDirectory : directory);

        var tempPath = $"{filePath}.tmp";
        await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        if (File.Exists(filePath))
        {
            File.Copy(tempPath, filePath, overwrite: true);
            File.Delete(tempPath);
            return;
        }

        File.Move(tempPath, filePath);
    }

    private static SystemSettings Clone(SystemSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        return JsonSerializer.Deserialize<SystemSettings>(json, JsonOptions) ?? SystemSettings.CreateDefaults();
    }

    private static string ExpandPath(string value)
    {
        return Environment.ExpandEnvironmentVariables(value);
    }

    private sealed class SettingsDocument
    {
        public SystemSettings? SystemSettings { get; set; }
    }
}
