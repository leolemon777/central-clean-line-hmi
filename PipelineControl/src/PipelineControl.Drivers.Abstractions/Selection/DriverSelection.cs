namespace PipelineControl.Drivers.Abstractions.Selection;

public sealed record DriverSelection(DriverKind ActiveDriver) : IDriverSelection;
