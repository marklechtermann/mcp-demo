using System.ComponentModel;
using DSpace.ControlDesk.Mcp.ControlDesk;
using DSpace.ControlDesk.Mcp.Models;
using ModelContextProtocol.Server;

namespace DSpace.ControlDesk.Mcp.Tools;

/// <summary>MCP tools for creating ControlDesk projects and experiments.</summary>
[McpServerToolType]
public sealed class ProjectExperimentTools
{
    private readonly IControlDeskClient _controlDesk;

    public ProjectExperimentTools(IControlDeskClient controlDesk) => _controlDesk = controlDesk;

    [McpServerTool(Name = "create_experiment_with_project", Destructive = false, OpenWorld = false)]
    [Description("Create a new ControlDesk project together with an associated experiment, and activate the experiment. " +
                "Returns the project name, experiment name, and the project root directory.")]
    public async Task<ExperimentInfo> CreateExperimentWithProject(
        [Description("Name of the new ControlDesk project, e.g. \"DemoProject\".")] string projectName,
        [Description("Name of the experiment to create inside the project, e.g. \"Experiment1\".")] string experimentName,
        [Description("Optional absolute path of the ControlDesk project root directory. When omitted, the active project root is used.")] string? projectRoot = null,
        CancellationToken cancellationToken = default)
    {
        ToolHelpers.RequireNonBlank(projectName, nameof(projectName));
        ToolHelpers.RequireNonBlank(experimentName, nameof(experimentName));

        try
        {
            return await _controlDesk.CreateExperimentWithProjectAsync(projectName, experimentName, projectRoot, cancellationToken);
        }
        catch (ControlDeskException ex)
        {
            throw ToolHelpers.ToMcpError(ex);
        }
    }

    [McpServerTool(Name = "delete_project", Destructive = true, OpenWorld = false)]
    [Description("Delete a ControlDesk project, including its experiments and data, from disk. " +
                "If the project is currently active it is closed first. Use this to clean up demo or test projects.")]
    public async Task<ProjectDeletionResult> DeleteProject(
        [Description("Name of the ControlDesk project to delete, e.g. \"DemoProject\".")] string projectName,
        [Description("Optional absolute path of the project root directory that contains the project. When omitted, the active project root is used.")] string? projectRoot = null,
        CancellationToken cancellationToken = default)
    {
        ToolHelpers.RequireNonBlank(projectName, nameof(projectName));

        try
        {
            return await _controlDesk.DeleteProjectAsync(projectName, projectRoot, cancellationToken);
        }
        catch (ControlDeskException ex)
        {
            throw ToolHelpers.ToMcpError(ex);
        }
    }
}
