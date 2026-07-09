namespace PipelineControl.UI.Services.Io;

public interface IIoPointVerificationImporter
{
    /// <summary>
    /// Imports a verified field CSV and writes the confirmed point map back to io-points.json.
    /// </summary>
    Task<IoPointVerificationImportResult> ImportCsvAsync(
        string csvFilePath,
        string ioPointsJsonPath,
        CancellationToken cancellationToken = default);
}

public sealed record IoPointVerificationImportResult(
    int UpdatedCount,
    int InputUpdatedCount,
    int OutputUpdatedCount,
    int SkippedCount,
    string BackupFilePath,
    string OutputFilePath);
