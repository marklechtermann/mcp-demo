using System.ComponentModel;
using DSpace.ControlDesk.Mcp.ControlDesk;
using DSpace.ControlDesk.Mcp.Models;
using ModelContextProtocol.Server;

namespace DSpace.ControlDesk.Mcp.Tools;

/// <summary>MCP tools for creating and listing ControlDesk layouts.</summary>
[McpServerToolType]
public sealed class LayoutTools
{
    private readonly IControlDeskClient _controlDesk;

    public LayoutTools(IControlDeskClient controlDesk) => _controlDesk = controlDesk;

    [McpServerTool(Name = "create_layout", Destructive = false, OpenWorld = false)]
    [Description("Create a new, empty layout in the active ControlDesk experiment. Returns the created layout and its instrument count.")]
    public async Task<LayoutInfo> CreateLayout(
        [Description("Name of the new layout, e.g. \"MyLayout\".")] string layoutName,
        CancellationToken cancellationToken = default)
    {
        ToolHelpers.RequireNonBlank(layoutName, nameof(layoutName));

        try
        {
            return await _controlDesk.CreateLayoutAsync(layoutName, cancellationToken);
        }
        catch (ControlDeskException ex)
        {
            throw ToolHelpers.ToMcpError(ex);
        }
    }

    [McpServerTool(Name = "list_layouts", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("List all layouts that are currently open/displayed in the active ControlDesk experiment, with the number of instruments on each.")]
    public async Task<IReadOnlyList<LayoutInfo>> ListLayouts(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _controlDesk.ListLayoutsAsync(cancellationToken);
        }
        catch (ControlDeskException ex)
        {
            throw ToolHelpers.ToMcpError(ex);
        }
    }
}
