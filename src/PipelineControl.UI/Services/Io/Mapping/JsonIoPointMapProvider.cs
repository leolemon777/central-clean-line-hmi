using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PipelineControl.UI.Services.Io.Mapping;

public sealed class JsonIoPointMapProvider : IIoPointMapProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string filePath;

    public JsonIoPointMapProvider()
        : this(Path.Combine(AppContext.BaseDirectory, "Resources", "io-points.json"))
    {
    }

    public JsonIoPointMapProvider(string filePath)
    {
        this.filePath = filePath;
    }

    public IoPointMap Load()
    {
        var map = File.Exists(filePath)
            ? JsonSerializer.Deserialize<IoPointMapDocument>(File.ReadAllText(filePath), JsonOptions)
            : CreateDefaultDocument();

        var inputs = (map?.Inputs ?? new List<IoPointDefinition>()).OrderBy(point => point.PointNo).ToList();
        var outputs = (map?.Outputs ?? new List<IoPointDefinition>()).OrderBy(point => point.PointNo).ToList();
        Validate(inputs, IoType.Input);
        Validate(outputs, IoType.Output);

        return new IoPointMap(map?.Notes ?? new List<string>(), inputs, outputs);
    }

    public static IoPointMapDocument CreateDefaultDocument()
    {
        var notes = new[]
        {
            "当前 64 点映射按三张物理卡显示：IO控制卡 16 点、扩展卡1 24 点、扩展卡2 24 点。",
            "24 点扩展卡底层仍按厂家 16 位 module 拆分为 16+8 读取，但画面点位名称在每张物理卡内从 0 重新开始。",
            "现场验证后只修改本 io-points.json，不要把点位地址写死到 C# 代码里。"
        };

        return new IoPointMapDocument
        {
            Notes = notes.ToList(),
            Inputs = CreateDefinitions(IoType.Input).ToList(),
            Outputs = CreateDefinitions(IoType.Output).ToList()
        };
    }

    private static IEnumerable<IoPointDefinition> CreateDefinitions(IoType ioType)
    {
        for (var pointNo = 1; pointNo <= 64; pointNo++)
        {
            var (moduleIndex, bitIndex) = ResolveDefaultAddress(pointNo);
            var localIndex = ResolveLocalPointIndex(moduleIndex, bitIndex);
            var displayPrefix = ioType == IoType.Input ? "X" : "Y";
            yield return new IoPointDefinition
            {
                PointNo = pointNo,
                IoType = ioType,
                Name = $"{displayPrefix}{localIndex}",
                ModuleIndex = moduleIndex,
                BitIndex = bitIndex,
                Description = $"{CreatePhysicalCardName(moduleIndex)} {(ioType == IoType.Input ? "输入" : "输出")}点 {displayPrefix}{localIndex}，现场确认后改为实际名称。",
                IsEnabled = true,
                SafeDefaultValue = false
            };
        }
    }

    private static (int ModuleIndex, int BitIndex) ResolveDefaultAddress(int pointNo)
    {
        return pointNo switch
        {
            >= 1 and <= 16 => (0, pointNo - 1),
            >= 17 and <= 32 => (1, pointNo - 17),
            >= 33 and <= 40 => (2, pointNo - 33),
            >= 41 and <= 56 => (3, pointNo - 41),
            >= 57 and <= 64 => (4, pointNo - 57),
            _ => throw new ArgumentOutOfRangeException(nameof(pointNo), pointNo, "全局 IO 点号必须在 1-64 范围内。")
        };
    }

    private static int ResolveLocalPointIndex(int moduleIndex, int bitIndex)
    {
        return moduleIndex switch
        {
            0 or 1 or 3 => bitIndex,
            2 or 4 => 16 + bitIndex,
            _ => bitIndex
        };
    }

    private static string CreatePhysicalCardName(int moduleIndex)
    {
        return moduleIndex switch
        {
            0 => "IO控制卡",
            1 or 2 => "扩展卡1",
            3 or 4 => "扩展卡2",
            _ => $"扩展卡{moduleIndex}"
        };
    }

    private static void Validate(IReadOnlyList<IoPointDefinition> points, IoType expectedType)
    {
        var duplicated = points.GroupBy(point => point.PointNo).FirstOrDefault(group => group.Count() > 1);
        if (duplicated is not null)
        {
            throw new InvalidDataException($"{expectedType} 点位存在重复全局点号 {duplicated.Key}。");
        }

        foreach (var point in points)
        {
            if (point.IoType != expectedType)
            {
                throw new InvalidDataException($"{point.GlobalLabel} 的 IoType 与所在配置段不一致。");
            }

            if (point.PointNo < 1 || point.PointNo > 64)
            {
                throw new InvalidDataException($"{point.GlobalLabel} 的 PointNo 超出 1-64。");
            }

            if (point.ModuleIndex < 0 || point.BitIndex < 0 || point.BitIndex > 15)
            {
                throw new InvalidDataException($"{point.GlobalLabel} 的 moduleIndex/bitIndex 不合法。");
            }
        }
    }
}

public sealed class IoPointMapDocument
{
    public List<string> Notes { get; set; } = new();

    public List<IoPointDefinition> Inputs { get; set; } = new();

    public List<IoPointDefinition> Outputs { get; set; } = new();
}
