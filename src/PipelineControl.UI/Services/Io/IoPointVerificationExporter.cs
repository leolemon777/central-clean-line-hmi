using System.Globalization;
using System.IO;
using System.Text;

namespace PipelineControl.UI.Services.Io;

public sealed class IoPointVerificationExporter : IIoPointVerificationExporter
{
    private static readonly string[] Headers =
    [
        "点号",
        "类型",
        "全局序号",
        "名称",
        "假设模块号",
        "假设位号",
        "假设地址",
        "启用",
        "安全默认输出",
        "现场端子",
        "现场线号",
        "实际设备/信号",
        "实测模块号",
        "实测位号",
        "验证结果",
        "验证人",
        "验证时间",
        "备注"
    ];

    /// <summary>
    /// Exports a CSV worksheet for field technicians to verify X/Y points one by one.
    /// </summary>
    public async Task ExportCsvAsync(
        IEnumerable<IoPointDefinition> inputs,
        IEnumerable<IoPointDefinition> outputs,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(filePath);
        Directory.CreateDirectory(string.IsNullOrWhiteSpace(directory) ? AppContext.BaseDirectory : directory);

        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        await WriteRowAsync(writer, Headers, cancellationToken).ConfigureAwait(false);
        foreach (var point in inputs.OrderBy(point => point.PointNo).Concat(outputs.OrderBy(point => point.PointNo)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WriteRowAsync(writer, CreateRow(point), cancellationToken).ConfigureAwait(false);
        }
    }

    private static string[] CreateRow(IoPointDefinition point)
    {
        return
        [
            point.GlobalLabel,
            point.IoType == IoType.Input ? "输入" : "输出",
            point.PointNo.ToString(CultureInfo.InvariantCulture),
            point.Name,
            point.ModuleIndex.ToString(CultureInfo.InvariantCulture),
            point.BitIndex.ToString(CultureInfo.InvariantCulture),
            $"module {point.ModuleIndex} / bit {point.BitIndex}",
            point.IsEnabled ? "是" : "否",
            point.IoType == IoType.Output ? (point.SafeDefaultValue ? "ON" : "OFF") : string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            "未验证",
            string.Empty,
            string.Empty,
            "初始映射是假设值，最终以厂家 Demo、万用表和现场逐点验证结果为准。"
        ];
    }

    private static Task WriteRowAsync(TextWriter writer, IEnumerable<string> values, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return writer.WriteLineAsync(string.Join(",", values.Select(Escape)));
    }

    private static string Escape(string value)
    {
        var text = value ?? string.Empty;
        return text.Contains('"', StringComparison.Ordinal) || text.Contains(',', StringComparison.Ordinal) || text.Contains('\n', StringComparison.Ordinal) || text.Contains('\r', StringComparison.Ordinal)
            ? $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : text;
    }
}
