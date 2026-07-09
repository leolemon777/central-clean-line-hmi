using System.IO;
using PipelineControl.UI.Services.Io;
using PipelineControl.UI.Services.Io.Mapping;
using Xunit;

namespace PipelineControl.UI.Tests;

public sealed class IoPointVerificationExporterTests : IDisposable
{
    private readonly string tempRoot = Path.Combine(Path.GetTempPath(), $"PipelineIoVerify-{Guid.NewGuid():N}");

    public IoPointVerificationExporterTests()
    {
        Directory.CreateDirectory(tempRoot);
    }

    [Fact]
    public async Task Export_csv_contains_all_input_and_output_points()
    {
        var map = new JsonIoPointMapProvider("missing-io-points.json").Load();
        var exporter = new IoPointVerificationExporter();
        var filePath = Path.Combine(tempRoot, "io-point-verification.csv");

        await exporter.ExportCsvAsync(map.Inputs, map.Outputs, filePath);

        var lines = await File.ReadAllLinesAsync(filePath);
        Assert.Equal(129, lines.Length);
        Assert.Contains("点号,类型,全局序号,名称,假设模块号,假设位号", lines[0], StringComparison.Ordinal);
        Assert.Contains("X1,输入,1", lines[1], StringComparison.Ordinal);
        Assert.Contains("Y64,输出,64", lines[^1], StringComparison.Ordinal);
        Assert.Contains("未验证", lines[^1], StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
