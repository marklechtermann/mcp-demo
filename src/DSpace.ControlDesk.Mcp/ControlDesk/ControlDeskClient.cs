using System.Collections;
using System.Runtime.InteropServices;
using DSpace.ControlDesk.Mcp.Models;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.Logging;

namespace DSpace.ControlDesk.Mcp.ControlDesk;

/// <summary>
/// Drives ControlDesk through its COM automation interface using late binding
/// (<c>dynamic</c>). Late binding is used deliberately so the server builds and
/// runs without the ControlDesk interop assemblies being present at build time
/// (they ship only with a local ControlDesk installation).
/// </summary>
/// <remarks>
/// COM entry point per the dSPACE ControlDesk Automation documentation:
/// ProgID <c>ControlDeskNG.Application</c>, root interface <c>IXaApplication</c>.
/// A 64-bit client is mandatory; 32-bit automation clients are unsupported.
/// </remarks>
public sealed class ControlDeskClient : IControlDeskClient, IDisposable
{
    private const string ProgId = "ControlDeskNG.Application";

    private readonly StaThreadDispatcher _dispatcher = new();
    private readonly ILogger<ControlDeskClient> _logger;

    // Accessed only from the STA dispatcher thread.
    private dynamic? _application;

    public ControlDeskClient(ILogger<ControlDeskClient> logger) => _logger = logger;

    // --- Public operations ------------------------------------------------

    public Task<ExperimentInfo> CreateExperimentWithProjectAsync(
        string projectName,
        string experimentName,
        string? projectRoot,
        CancellationToken cancellationToken) =>
        ExecuteAsync(nameof(CreateExperimentWithProjectAsync), app =>
        {
            ActivateProjectRoot(app, projectRoot);

            // Projects.Add returns the new project, which becomes the active project.
            dynamic project = app.Projects.Add(projectName);

            // Experiments.Add returns the new experiment, which is already the active
            // experiment (IXaActiveExperiment) - it must not be activated again. The
            // boolean argument auto-saves any previously active experiment.
            project.Experiments.Add(experimentName, true);

            string root = (string)app.ActiveProjectRoot.PathName;
            _logger.LogInformation("Created project '{Project}' with experiment '{Experiment}' in '{Root}'.", projectName, experimentName, root);
            return new ExperimentInfo(projectName, experimentName, root);
        }, cancellationToken);

    public Task<InstrumentInfo> CreateInstrumentAsync(
        string layoutName,
        string instrumentType,
        string instrumentName,
        int x,
        int y,
        int width,
        int height,
        CancellationToken cancellationToken) =>
        ExecuteAsync(nameof(CreateInstrumentAsync), app =>
        {
            dynamic layout = RequireLayout(app, layoutName);

            if ((bool)layout.Instruments.Contains(instrumentName))
            {
                throw new ControlDeskException(
                    "instrument_already_exists",
                    $"Layout '{layoutName}' already contains an instrument named '{instrumentName}'.",
                    transient: false);
            }

            dynamic instrument = layout.Instruments.Add(instrumentType, instrumentName, x, y, width, height);
            _logger.LogInformation("Added instrument '{Name}' ({Type}) to layout '{Layout}'.", instrumentName, instrumentType, layoutName);
            return (InstrumentInfo)ReadInstrument(instrument, layoutName, instrumentType);
        }, cancellationToken);

    public Task<IReadOnlyList<LayoutInfo>> ListLayoutsAsync(CancellationToken cancellationToken) =>
        ExecuteAsync<IReadOnlyList<LayoutInfo>>(nameof(ListLayoutsAsync), app =>
        {
            var layouts = new List<LayoutInfo>();
            foreach (dynamic layout in Enumerate(app.LayoutManagement.Layouts))
            {
                layouts.Add(new LayoutInfo((string)layout.Name, TryGetInstrumentCount(layout)));
            }

            return layouts;
        }, cancellationToken);

    public Task<IReadOnlyList<InstrumentInfo>> ListInstrumentsAsync(string layoutName, CancellationToken cancellationToken) =>
        ExecuteAsync<IReadOnlyList<InstrumentInfo>>(nameof(ListInstrumentsAsync), app =>
        {
            dynamic layout = RequireLayout(app, layoutName);

            var instruments = new List<InstrumentInfo>();
            foreach (dynamic instrument in Enumerate(layout.Instruments))
            {
                instruments.Add(ReadInstrument(instrument, layoutName, type: null));
            }

            return instruments;
        }, cancellationToken);

    public Task<LayoutInfo> CreateLayoutAsync(string layoutName, CancellationToken cancellationToken) =>
        ExecuteAsync(nameof(CreateLayoutAsync), app =>
        {
            dynamic layout = app.LayoutManagement.Layouts.Add(layoutName);
            _logger.LogInformation("Created layout '{Layout}'.", layoutName);
            return new LayoutInfo((string)layout.Name, TryGetInstrumentCount(layout));
        }, cancellationToken);

    public Task<InstrumentInfo> InspectInstrumentAsync(string layoutName, string instrumentName, CancellationToken cancellationToken) =>
        ExecuteAsync(nameof(InspectInstrumentAsync), app =>
        {
            dynamic layout = RequireLayout(app, layoutName);

            if (!(bool)layout.Instruments.Contains(instrumentName))
            {
                throw new ControlDeskException(
                    "instrument_not_found",
                    $"Layout '{layoutName}' does not contain an instrument named '{instrumentName}'.",
                    transient: false);
            }

            dynamic instrument = layout.Instruments.Item(instrumentName);
            return (InstrumentInfo)ReadInstrument(instrument, layoutName, type: null);
        }, cancellationToken);

    public Task<ProjectDeletionResult> DeleteProjectAsync(string projectName, string? projectRoot, CancellationToken cancellationToken) =>
        ExecuteAsync(nameof(DeleteProjectAsync), app =>
        {
            if (projectRoot is not null && (bool)app.ProjectRoots.Contains(projectRoot))
            {
                app.ProjectRoots.Item(projectRoot).Activate();
            }

            CloseProjectIfActive(app, projectName);

            dynamic projects = app.Projects;
            if (!(bool)projects.Contains(projectName))
            {
                throw new ControlDeskException(
                    "project_not_found",
                    $"No project named '{projectName}' was found in project root '{(string)app.ActiveProjectRoot.PathName}'.",
                    transient: false);
            }

            // Remove(True) also deletes the project files from disk.
            projects.Item(projectName).Remove(true);
            _logger.LogInformation("Deleted project '{Project}'.", projectName);
            return new ProjectDeletionResult(projectName, Deleted: true);
        }, cancellationToken);

    // --- COM helpers ------------------------------------------------------

    private dynamic GetApplication()
    {
        // Must be invoked on the STA dispatcher thread.
        if (_application is not null)
        {
            return _application;
        }

        Type? serverType = Type.GetTypeFromProgID(ProgId);
        if (serverType is null)
        {
            throw new ControlDeskException(
                "controldesk_not_installed",
                $"The ControlDesk COM server '{ProgId}' is not registered. Install 64-bit ControlDesk and try again.",
                transient: false);
        }

        try
        {
            _application = Activator.CreateInstance(serverType);
        }
        catch (Exception ex)
        {
            throw new ControlDeskException(
                "controldesk_unavailable",
                "Could not start or connect to ControlDesk via COM automation.",
                transient: true,
                ex);
        }

        if (_application is null)
        {
            throw new ControlDeskException(
                "controldesk_unavailable",
                "ControlDesk COM automation did not return an application object.",
                transient: true);
        }

        return _application;
    }

    private Task<T> ExecuteAsync<T>(string operation, Func<dynamic, T> action, CancellationToken cancellationToken) =>
        _dispatcher.InvokeAsync<T>(
            () =>
            {
                try
                {
                    dynamic app = GetApplication();
                    T result = action(app);
                    return result;
                }
                catch (ControlDeskException)
                {
                    throw;
                }
                catch (COMException ex)
                {
                    throw new ControlDeskException(
                        "controldesk_com_error",
                        $"ControlDesk COM call failed during '{operation}': {ex.Message}",
                        transient: false,
                        ex);
                }
                catch (RuntimeBinderException ex)
                {
                    throw new ControlDeskException(
                        "controldesk_api_mismatch",
                        $"A ControlDesk automation member was not found during '{operation}': {ex.Message}",
                        transient: false,
                        ex);
                }
            },
            cancellationToken);

    private static dynamic RequireLayout(dynamic app, string layoutName)
    {
        foreach (dynamic layout in Enumerate(app.LayoutManagement.Layouts))
        {
            // ControlDesk automation is case-sensitive, so use an ordinal comparison.
            if (string.Equals((string)layout.Name, layoutName, StringComparison.Ordinal))
            {
                return layout;
            }
        }

        throw new ControlDeskException(
            "layout_not_found",
            $"No open layout named '{layoutName}' was found in the active experiment.",
            transient: false);
    }

    private static void ActivateProjectRoot(dynamic app, string? projectRoot)
    {
        if (string.IsNullOrWhiteSpace(projectRoot))
        {
            return;
        }

        if (!(bool)app.ProjectRoots.Contains(projectRoot))
        {
            app.ProjectRoots.Add(projectRoot);
        }

        app.ProjectRoots.Item(projectRoot).Activate();
    }

    private static void CloseProjectIfActive(dynamic app, string projectName)
    {
        // ControlDesk cannot delete the currently active project, so close it first.
        dynamic? activeProject = null;
        try
        {
            activeProject = app.ActiveProject;
        }
        catch (Exception)
        {
            // No active project; nothing to close.
            return;
        }

        if (activeProject is null)
        {
            return;
        }

        try
        {
            if (string.Equals((string)activeProject.Name, projectName, StringComparison.Ordinal))
            {
                activeProject.Close(false);
            }
        }
        catch (Exception)
        {
            // If the active project name cannot be read or closed, fall through and let
            // the subsequent Remove call surface a meaningful error.
        }
    }

    private static InstrumentInfo ReadInstrument(dynamic instrument, string layoutName, string? type)
    {
        var name = (string)instrument.Name;

        int x = 0, y = 0, width = 0, height = 0;
        try
        {
            dynamic position = instrument.Position;
            x = (int)position.X;
            y = (int)position.Y;
            width = (int)position.Width;
            height = (int)position.Height;
        }
        catch (Exception)
        {
            // Position is best-effort; keep zeros if unavailable.
        }

        string? customData = null;
        try
        {
            customData = (string?)instrument.CustomData;
        }
        catch (Exception)
        {
            // CustomData is optional.
        }

        return new InstrumentInfo(name, layoutName, type, x, y, width, height, customData);
    }

    private static int TryGetInstrumentCount(dynamic layout)
    {
        try
        {
            return (int)layout.Instruments.Count;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    private static IEnumerable<dynamic> Enumerate(dynamic collection)
    {
        if (collection is IEnumerable enumerable)
        {
            foreach (object? item in enumerable)
            {
                yield return item!;
            }

            yield break;
        }

        // Fallback for collections that only expose Count/Item (0-based).
        var count = (int)collection.Count;
        for (var i = 0; i < count; i++)
        {
            yield return collection.Item(i);
        }
    }

    public void Dispose()
    {
        try
        {
            _dispatcher.InvokeAsync<object?>(() =>
            {
                if (_application is not null)
                {
                    try
                    {
                        Marshal.FinalReleaseComObject(_application);
                    }
                    catch (Exception)
                    {
                        // Best-effort release; the OS reclaims the RCW on process exit.
                    }

                    _application = null;
                }

                return null;
            }).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            // Ignore shutdown races.
        }

        _dispatcher.Dispose();
    }
}
