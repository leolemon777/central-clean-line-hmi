namespace PipelineControl.Drivers.Abstractions.Selection;

public interface IDriverSelection
{
    DriverKind ActiveDriver { get; }
}
