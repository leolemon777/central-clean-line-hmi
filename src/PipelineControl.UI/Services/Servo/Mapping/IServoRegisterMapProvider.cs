namespace PipelineControl.UI.Services.Servo.Mapping;

public interface IServoRegisterMapProvider
{
    ServoRegisterMap Load();
}

public sealed record ServoRegisterMap(
    IReadOnlyList<string> Notes,
    IReadOnlyDictionary<string, ServoRegisterDefinition> Registers,
    IReadOnlyList<ServoAxisDefinition> Axes)
{
    public bool TryGetRegister(string key, out ServoRegisterDefinition definition)
    {
        return Registers.TryGetValue(key, out definition!);
    }
}

public sealed record ServoRegisterDefinition(string Address, int? OnValue, int? OffValue, string Note)
{
    public ushort AddressValue
    {
        get
        {
            var normalized = string.IsNullOrWhiteSpace(Address) ? "0" : Address.Trim();
            if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[2..];
            }

            return (ushort)Convert.ToUInt16(normalized, 16);
        }
    }

    public bool HasWritableValue => OnValue.HasValue || OffValue.HasValue;
}

public sealed record ServoAxisDefinition(int Axis, string Name, int Station);
