using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PipelineControl.UI.Services.Servo.Mapping;

public sealed class JsonServoRegisterMapProvider : IServoRegisterMapProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string filePath;

    public JsonServoRegisterMapProvider()
        : this(Path.Combine(AppContext.BaseDirectory, "Resources", "servo-registers.json"))
    {
    }

    public JsonServoRegisterMapProvider(string filePath)
    {
        this.filePath = filePath;
    }

    public ServoRegisterMap Load()
    {
        var document = File.Exists(filePath)
            ? JsonSerializer.Deserialize<ServoRegisterMapDocument>(File.ReadAllText(filePath), JsonOptions)
            : CreateDefaultDocument();

        var notes = document?.Notes ?? new List<string>();
        var registers = (document?.Registers ?? new Dictionary<string, ServoRegisterDefinitionDocument>())
            .ToDictionary(
                pair => pair.Key,
                pair => new ServoRegisterDefinition(
                    pair.Value.Address,
                    pair.Value.OnValue,
                    pair.Value.OffValue,
                    pair.Value.Note ?? string.Empty),
                StringComparer.Ordinal);
        var axes = (document?.Axes ?? new List<ServoAxisDefinition>())
            .OrderBy(axis => axis.Axis)
            .ToList();

        Validate(registers, axes);

        return new ServoRegisterMap(notes, registers, axes);
    }

    public static ServoRegisterMapDocument CreateDefaultDocument()
    {
        return new ServoRegisterMapDocument
        {
            Notes = new List<string>
            {
                "默认伺服寄存器映射。实际使用前请通过 servo-registers.json 现场回填。"
            },
            Registers = new Dictionary<string, ServoRegisterDefinitionDocument>
            {
                ["ServoOn"] = new ServoRegisterDefinitionDocument
                {
                    Address = "3607",
                    OnValue = 2,
                    OffValue = 0,
                    Note = "伺服使能（docx 已确认）"
                }
            },
            Axes = Enumerable.Range(1, 4)
                .Select(axis => new ServoAxisDefinition(axis, $"{axis}#伺服", axis))
                .ToList()
        };
    }

    private static void Validate(IReadOnlyDictionary<string, ServoRegisterDefinition> registers, IReadOnlyList<ServoAxisDefinition> axes)
    {
        foreach (var (key, register) in registers)
        {
            try
            {
                _ = register.AddressValue;
            }
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                throw new InvalidDataException($"寄存器 {key} 的地址 {register.Address} 不是合法的十六进制值。");
            }
        }

        var duplicated = axes.GroupBy(axis => axis.Axis).FirstOrDefault(group => group.Count() > 1);
        if (duplicated is not null)
        {
            throw new InvalidDataException($"伺服轴号 {duplicated.Key} 重复。");
        }

        foreach (var axis in axes)
        {
            if (axis.Station < 1 || axis.Station > 247)
            {
                throw new InvalidDataException($"{axis.Name} 的站号 {axis.Station} 超出 1-247。");
            }
        }
    }
}

public sealed class ServoRegisterMapDocument
{
    public List<string> Notes { get; set; } = new();

    public Dictionary<string, ServoRegisterDefinitionDocument> Registers { get; set; } = new();

    public List<ServoAxisDefinition> Axes { get; set; } = new();
}

public sealed class ServoRegisterDefinitionDocument
{
    public string Address { get; set; } = string.Empty;

    public int? OnValue { get; set; }

    public int? OffValue { get; set; }

    public string? Note { get; set; }
}
