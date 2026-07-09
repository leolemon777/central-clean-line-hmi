namespace PipelineControl.UI.Services.Io;

public interface IIoPointVerificationExporter
{
    /// <summary>
    /// Exports the current IO point map to a field verification CSV worksheet.
    /// </summary>
    Task ExportCsvAsync(
        IEnumerable<IoPointDefinition> inputs,
        IEnumerable<IoPointDefinition> outputs,
        string filePath,
        CancellationToken cancellationToken = default);
}
