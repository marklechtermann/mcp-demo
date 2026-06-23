using DSpace.ControlDesk.Mcp.Models;

namespace DSpace.ControlDesk.Mcp.ControlDesk;

/// <summary>
/// Abstraction over the ControlDesk COM automation interface. Every method
/// performs a single, focused ControlDesk operation. Implementations marshal
/// all COM access onto a dedicated STA thread.
/// </summary>
public interface IControlDeskClient
{
    /// <summary>Creates a new project and an experiment inside it, then activates the experiment.</summary>
    Task<ExperimentInfo> CreateExperimentWithProjectAsync(
        string projectName,
        string experimentName,
        string? projectRoot,
        CancellationToken cancellationToken);

    /// <summary>Adds a new instrument of the given type to a layout.</summary>
    Task<InstrumentInfo> CreateInstrumentAsync(
        string layoutName,
        string instrumentType,
        string instrumentName,
        int x,
        int y,
        int width,
        int height,
        CancellationToken cancellationToken);

    /// <summary>Lists all currently open layouts of the active experiment.</summary>
    Task<IReadOnlyList<LayoutInfo>> ListLayoutsAsync(CancellationToken cancellationToken);

    /// <summary>Lists the instruments placed on the given layout.</summary>
    Task<IReadOnlyList<InstrumentInfo>> ListInstrumentsAsync(string layoutName, CancellationToken cancellationToken);

    /// <summary>Creates a new, empty layout in the active experiment.</summary>
    Task<LayoutInfo> CreateLayoutAsync(string layoutName, CancellationToken cancellationToken);

    /// <summary>Returns the details of a single instrument located on a layout.</summary>
    Task<InstrumentInfo> InspectInstrumentAsync(string layoutName, string instrumentName, CancellationToken cancellationToken);

    /// <summary>Closes (if active) and deletes a project, including its experiments and data on disk.</summary>
    Task<ProjectDeletionResult> DeleteProjectAsync(string projectName, string? projectRoot, CancellationToken cancellationToken);
}
