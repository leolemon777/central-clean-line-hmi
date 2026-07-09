using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PipelineControl.UI.Services.Io;
using PipelineControl.UI.Services.Io.Mapping;
using Xunit;

namespace PipelineControl.UI.Tests;

public sealed class IoPointVerificationImporterTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string tempRoot = Path.Combine(Path.GetTempPath(), $"PipelineIoImport-{Guid.NewGuid():N}");

    public IoPointVerificationImporterTests()
    {
        Directory.CreateDirectory(tempRoot);
    }

    [Fact]
    public async Task Import_csv_updates_only_verified_points_and_creates_backup()
    {
        var jsonPath = await WriteDefaultMapAsync();
        var csvPath = Path.Combine(tempRoot, "verified.csv");
        await File.WriteAllTextAsync(
            csvPath,
            string.Join(Environment.NewLine,
            [
                "点号,类型,全局序号,名称,假设模块号,假设位号,假设地址,启用,安全默认输出,现场端子,现场线号,实际设备/信号,实测模块号,实测位号,验证结果,验证人,验证时间,备注",
                "X1,输入,1,X1,0,0,module 0 / bit 0,是,,TB1-01,X101,启动按钮,7,2,通过,调试员,2026-05-16,首件确认",
                "Y64,输出,64,Y64,4,7,module 4 / bit 7,否,ON,TB2-08,Y264,排水阀,8,3,OK,调试员,2026-05-16,安全负载测试",
                "X2,输入,2,X2,0,1,module 0 / bit 1,是,,TB1-02,X102,备用输入,9,1,未验证,调试员,2026-05-16,"
            ]),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        var importer = new IoPointVerificationImporter();
        var result = await importer.ImportCsvAsync(csvPath, jsonPath);
        var map = new JsonIoPointMapProvider(jsonPath).Load();

        Assert.Equal(2, result.UpdatedCount);
        Assert.Equal(1, result.InputUpdatedCount);
        Assert.Equal(1, result.OutputUpdatedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.True(File.Exists(result.BackupFilePath));

        var x1 = map.Inputs.Single(point => point.PointNo == 1);
        Assert.Equal("启动按钮", x1.Name);
        Assert.Equal(7, x1.ModuleIndex);
        Assert.Equal(2, x1.BitIndex);
        Assert.Contains("TB1-01", x1.Description, StringComparison.Ordinal);
        Assert.Contains("X101", x1.Description, StringComparison.Ordinal);

        var x2 = map.Inputs.Single(point => point.PointNo == 2);
        Assert.Equal("X1", x2.Name);
        Assert.Equal(0, x2.ModuleIndex);
        Assert.Equal(1, x2.BitIndex);

        var y64 = map.Outputs.Single(point => point.PointNo == 64);
        Assert.Equal("排水阀", y64.Name);
        Assert.Equal(8, y64.ModuleIndex);
        Assert.Equal(3, y64.BitIndex);
        Assert.False(y64.IsEnabled);
        Assert.True(y64.SafeDefaultValue);
    }

    [Fact]
    public async Task Default_output_points_keep_y_style_local_names()
    {
        var jsonPath = await WriteDefaultMapAsync();
        var map = new JsonIoPointMapProvider(jsonPath).Load();

        Assert.Equal("Y0", map.Outputs.Single(point => point.PointNo == 1).Name);
        Assert.Equal("Y23", map.Outputs.Single(point => point.PointNo == 64).Name);
        Assert.All(map.Outputs, point => Assert.Matches("^Y\\d+$", point.Name));
    }

    [Fact]
    public async Task Import_csv_rejects_verified_rows_with_invalid_bit_index()
    {
        var jsonPath = await WriteDefaultMapAsync();
        var csvPath = Path.Combine(tempRoot, "invalid.csv");
        await File.WriteAllTextAsync(
            csvPath,
            string.Join(Environment.NewLine,
            [
                "点号,类型,全局序号,名称,假设模块号,假设位号,假设地址,启用,安全默认输出,现场端子,现场线号,实际设备/信号,实测模块号,实测位号,验证结果,验证人,验证时间,备注",
                "Y1,输出,1,Y1,0,0,module 0 / bit 0,是,OFF,TB2-01,Y201,测试输出,1,23,通过,调试员,2026-05-16,"
            ]),
            Encoding.UTF8);

        var importer = new IoPointVerificationImporter();
        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => importer.ImportCsvAsync(csvPath, jsonPath));

        Assert.Contains("超出 0-15", ex.Message, StringComparison.Ordinal);
    }

    private async Task<string> WriteDefaultMapAsync()
    {
        var jsonPath = Path.Combine(tempRoot, "io-points.json");
        await using var stream = File.Create(jsonPath);
        await JsonSerializer.SerializeAsync(stream, JsonIoPointMapProvider.CreateDefaultDocument(), JsonOptions);
        return jsonPath;
    }

    public void Dispose()
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
