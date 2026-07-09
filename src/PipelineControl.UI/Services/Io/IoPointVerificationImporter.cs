using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PipelineControl.UI.Services.Io.Mapping;

namespace PipelineControl.UI.Services.Io;

public sealed class IoPointVerificationImporter : IIoPointVerificationImporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly HashSet<string> VerifiedResults = new(StringComparer.OrdinalIgnoreCase)
    {
        "OK",
        "PASS",
        "VERIFIED",
        "Y",
        "YES",
        "通过",
        "已验证",
        "验证通过",
        "是"
    };

    /// <summary>
    /// Imports a verified field CSV and atomically updates the IO point map.
    /// </summary>
    public async Task<IoPointVerificationImportResult> ImportCsvAsync(
        string csvFilePath,
        string ioPointsJsonPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(csvFilePath) || !File.Exists(csvFilePath))
        {
            throw new FileNotFoundException("未找到现场点位验证 CSV。", csvFilePath);
        }

        if (string.IsNullOrWhiteSpace(ioPointsJsonPath))
        {
            throw new ArgumentException("io-points.json 路径不能为空。", nameof(ioPointsJsonPath));
        }

        var document = await LoadDocumentAsync(ioPointsJsonPath, cancellationToken).ConfigureAwait(false);
        var rows = await ReadCsvAsync(csvFilePath, cancellationToken).ConfigureAwait(false);
        var updates = new Dictionary<(IoType Type, int PointNo), IoPointDefinition>();
        var skipped = 0;

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryResolvePoint(row, out var ioType, out var pointNo))
            {
                skipped++;
                continue;
            }

            if (!IsVerified(row))
            {
                skipped++;
                continue;
            }

            var existing = FindPoint(document, ioType, pointNo);
            var updated = CreateUpdatedPoint(existing, row);
            updates[(ioType, pointNo)] = updated;
        }

        document.Inputs = document.Inputs
            .OrderBy(point => point.PointNo)
            .Select(point => updates.TryGetValue((IoType.Input, point.PointNo), out var updated) ? updated : point)
            .ToList();
        document.Outputs = document.Outputs
            .OrderBy(point => point.PointNo)
            .Select(point => updates.TryGetValue((IoType.Output, point.PointNo), out var updated) ? updated : point)
            .ToList();
        EnsureImportNote(document);

        var backupPath = BackupIfExists(ioPointsJsonPath);
        await WriteDocumentAtomicAsync(ioPointsJsonPath, document, cancellationToken).ConfigureAwait(false);

        return new IoPointVerificationImportResult(
            updates.Count,
            updates.Keys.Count(key => key.Type == IoType.Input),
            updates.Keys.Count(key => key.Type == IoType.Output),
            skipped,
            backupPath,
            ioPointsJsonPath);
    }

    private static async Task<IoPointMapDocument> LoadDocumentAsync(string ioPointsJsonPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(ioPointsJsonPath))
        {
            return JsonIoPointMapProvider.CreateDefaultDocument();
        }

        await using var stream = File.OpenRead(ioPointsJsonPath);
        return await JsonSerializer.DeserializeAsync<IoPointMapDocument>(stream, JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? JsonIoPointMapProvider.CreateDefaultDocument();
    }

    private static async Task<IReadOnlyList<Dictionary<string, string>>> ReadCsvAsync(string csvFilePath, CancellationToken cancellationToken)
    {
        var text = await File.ReadAllTextAsync(csvFilePath, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        var records = ParseCsv(text).ToList();
        if (records.Count == 0)
        {
            return [];
        }

        var headers = records[0];
        var rows = new List<Dictionary<string, string>>();
        foreach (var record in records.Skip(1))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (record.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < headers.Count; index++)
            {
                var value = index < record.Count ? record[index] : string.Empty;
                row[headers[index]] = value.Trim();
            }

            rows.Add(row);
        }

        return rows;
    }

    private static IEnumerable<IReadOnlyList<string>> ParseCsv(string text)
    {
        var row = new List<string>();
        var value = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < text.Length; index++)
        {
            var current = text[index];
            if (current == '"')
            {
                if (inQuotes && index + 1 < text.Length && text[index + 1] == '"')
                {
                    value.Append('"');
                    index++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (current == ',' && !inQuotes)
            {
                row.Add(value.ToString());
                value.Clear();
                continue;
            }

            if ((current == '\n' || current == '\r') && !inQuotes)
            {
                if (current == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                {
                    index++;
                }

                row.Add(value.ToString());
                value.Clear();
                yield return row;
                row = [];
                continue;
            }

            value.Append(current);
        }

        if (value.Length > 0 || row.Count > 0)
        {
            row.Add(value.ToString());
            yield return row;
        }
    }

    private static bool TryResolvePoint(
        IReadOnlyDictionary<string, string> row,
        out IoType ioType,
        out int pointNo)
    {
        var label = GetValue(row, "点号").Trim();
        if (label.Length >= 2)
        {
            var prefix = char.ToUpperInvariant(label[0]);
            if ((prefix == 'X' || prefix == 'Y') && int.TryParse(label[1..], NumberStyles.Integer, CultureInfo.InvariantCulture, out pointNo))
            {
                ioType = prefix == 'X' ? IoType.Input : IoType.Output;
                return pointNo is >= 1 and <= 64;
            }
        }

        var typeText = GetValue(row, "类型");
        if (!int.TryParse(GetValue(row, "全局序号"), NumberStyles.Integer, CultureInfo.InvariantCulture, out pointNo))
        {
            ioType = IoType.Input;
            return false;
        }

        if (typeText.Contains("输入", StringComparison.OrdinalIgnoreCase) || typeText.Equals("Input", StringComparison.OrdinalIgnoreCase))
        {
            ioType = IoType.Input;
            return pointNo is >= 1 and <= 64;
        }

        if (typeText.Contains("输出", StringComparison.OrdinalIgnoreCase) || typeText.Equals("Output", StringComparison.OrdinalIgnoreCase))
        {
            ioType = IoType.Output;
            return pointNo is >= 1 and <= 64;
        }

        ioType = IoType.Input;
        return false;
    }

    private static bool IsVerified(IReadOnlyDictionary<string, string> row)
    {
        var result = GetValue(row, "验证结果").Trim();
        return VerifiedResults.Contains(result);
    }

    private static IoPointDefinition FindPoint(IoPointMapDocument document, IoType ioType, int pointNo)
    {
        var source = ioType == IoType.Input ? document.Inputs : document.Outputs;
        return source.FirstOrDefault(point => point.PointNo == pointNo)
            ?? throw new InvalidDataException($"{(ioType == IoType.Input ? "X" : "Y")}{pointNo} 不存在于 io-points.json。");
    }

    private static IoPointDefinition CreateUpdatedPoint(IoPointDefinition existing, IReadOnlyDictionary<string, string> row)
    {
        var moduleIndex = ParseRequiredAddress(GetValue(row, "实测模块号"), existing.GlobalLabel, "实测模块号");
        var bitIndex = ParseRequiredAddress(GetValue(row, "实测位号"), existing.GlobalLabel, "实测位号");
        if (bitIndex > 15)
        {
            throw new InvalidDataException($"{existing.GlobalLabel} 的实测位号 {bitIndex} 超出 0-15。24 点扩展卡仍需按厂家 API 的 16 位逻辑模块回填。");
        }

        var actualSignal = GetValue(row, "实际设备/信号");
        var name = string.IsNullOrWhiteSpace(actualSignal)
            ? existing.Name
            : actualSignal.Trim();

        return existing with
        {
            Name = name,
            ModuleIndex = moduleIndex,
            BitIndex = bitIndex,
            Description = CreateDescription(existing, row),
            IsEnabled = ParseOptionalBoolean(GetValue(row, "启用"), existing.IsEnabled),
            SafeDefaultValue = existing.IoType == IoType.Output
                ? ParseOptionalOutputDefault(GetValue(row, "安全默认输出"), existing.SafeDefaultValue)
                : false
        };
    }

    private static int ParseRequiredAddress(string value, string pointLabel, string columnName)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed < 0)
        {
            throw new InvalidDataException($"{pointLabel} 已标记验证通过，但 {columnName} 不是有效的非负整数。");
        }

        return parsed;
    }

    private static bool ParseOptionalBoolean(string value, bool fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var text = value.Trim();
        if (text is "是" or "启用" or "1" or "ON" or "开")
        {
            return true;
        }

        if (text is "否" or "禁用" or "0" or "OFF" or "关")
        {
            return false;
        }

        return bool.TryParse(text, out var parsed) ? parsed : fallback;
    }

    private static bool ParseOptionalOutputDefault(string value, bool fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var text = value.Trim();
        if (text is "ON" or "开" or "1" or "TRUE" or "true" or "是")
        {
            return true;
        }

        if (text is "OFF" or "关" or "0" or "FALSE" or "false" or "否")
        {
            return false;
        }

        return fallback;
    }

    private static string CreateDescription(IoPointDefinition existing, IReadOnlyDictionary<string, string> row)
    {
        var parts = new List<string>();
        AddPart(parts, GetValue(row, "实际设备/信号"));
        AddPart(parts, GetValue(row, "现场端子"), "端子");
        AddPart(parts, GetValue(row, "现场线号"), "线号");
        AddPart(parts, GetValue(row, "备注"));
        return parts.Count == 0 ? existing.Description : string.Join("；", parts);
    }

    private static void AddPart(ICollection<string> parts, string value, string? label = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        parts.Add(label is null ? value.Trim() : $"{label}: {value.Trim()}");
    }

    private static string BackupIfExists(string ioPointsJsonPath)
    {
        if (!File.Exists(ioPointsJsonPath))
        {
            return string.Empty;
        }

        var backupPath = $"{ioPointsJsonPath}.bak-{DateTime.Now:yyyyMMdd-HHmmss-fff}";
        File.Copy(ioPointsJsonPath, backupPath);
        return backupPath;
    }

    private static async Task WriteDocumentAtomicAsync(string ioPointsJsonPath, IoPointMapDocument document, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(ioPointsJsonPath);
        Directory.CreateDirectory(string.IsNullOrWhiteSpace(directory) ? AppContext.BaseDirectory : directory);

        var tempPath = $"{ioPointsJsonPath}.tmp";
        await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        if (File.Exists(ioPointsJsonPath))
        {
            File.Copy(tempPath, ioPointsJsonPath, overwrite: true);
            File.Delete(tempPath);
            return;
        }

        File.Move(tempPath, ioPointsJsonPath);
    }

    private static void EnsureImportNote(IoPointMapDocument document)
    {
        const string note = "现场验证表回填规则：只有验证结果明确通过的点会覆盖 moduleIndex/bitIndex，未验证点保持原映射。";
        if (!document.Notes.Any(item => item.Equals(note, StringComparison.Ordinal)))
        {
            document.Notes.Add(note);
        }
    }

    private static string GetValue(IReadOnlyDictionary<string, string> row, string key)
    {
        return row.TryGetValue(key, out var value) ? value : string.Empty;
    }
}
