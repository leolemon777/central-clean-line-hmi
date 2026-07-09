namespace PipelineControl.UI.Services.Io;

public sealed record IoPointDefinition
{
    public int PointNo { get; init; }

    public string Name { get; init; } = string.Empty;

    public IoType IoType { get; init; }

    public int ModuleIndex { get; init; }

    public int BitIndex { get; init; }

    public string Description { get; init; } = string.Empty;

    public bool IsEnabled { get; init; } = true;

    public bool SafeDefaultValue { get; init; }

    public string GlobalLabel => $"{(IoType == IoType.Input ? "X" : "Y")}{PointNo}";

    public string TagAddress => $"{(IoType == IoType.Input ? "X" : "Y")}{PointNo}";
}
