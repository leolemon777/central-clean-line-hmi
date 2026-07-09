namespace PipelineControl.UI.Services.Io.Mapping;

public interface IIoPointMapProvider
{
    IoPointMap Load();
}

public sealed record IoPointMap(
    IReadOnlyList<string> Notes,
    IReadOnlyList<IoPointDefinition> Inputs,
    IReadOnlyList<IoPointDefinition> Outputs)
{
    public IReadOnlyList<IoPointDefinition> AllPoints => Inputs.Concat(Outputs).ToList();
}
