namespace DSpace.ControlDesk.Mcp.Models;

/// <summary>A ControlDesk project together with the experiment created inside it.</summary>
public sealed record ExperimentInfo(string ProjectName, string ExperimentName, string ProjectRoot);

/// <summary>Summary of a ControlDesk layout.</summary>
public sealed record LayoutInfo(string Name, int InstrumentCount);

/// <summary>Details of a single instrument placed on a layout.</summary>
public sealed record InstrumentInfo(
    string Name,
    string LayoutName,
    string? Type,
    int X,
    int Y,
    int Width,
    int Height,
    string? CustomData);

/// <summary>Result of deleting a ControlDesk project.</summary>
public sealed record ProjectDeletionResult(string ProjectName, bool Deleted);
