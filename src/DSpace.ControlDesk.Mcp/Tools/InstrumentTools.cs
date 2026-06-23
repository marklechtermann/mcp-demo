using System.ComponentModel;
using DSpace.ControlDesk.Mcp.ControlDesk;
using DSpace.ControlDesk.Mcp.Models;
using ModelContextProtocol.Server;

namespace DSpace.ControlDesk.Mcp.Tools;

/// <summary>MCP tools for creating, listing, and inspecting instruments on layouts.</summary>
[McpServerToolType]
public sealed class InstrumentTools
{
    private readonly IControlDeskClient _controlDesk;

    public InstrumentTools(IControlDeskClient controlDesk) => _controlDesk = controlDesk;

    [McpServerTool(Name = "create_instrument", Destructive = false, OpenWorld = false)]
    [Description("Add a new instrument of a given type to a layout. " +
                "The instrument type must be a ControlDesk instrument library name such as \"Variable Array\", \"Time Plotter\", or \"Knob\".")]
    public async Task<InstrumentInfo> CreateInstrument(
        [Description("Name of the layout to add the instrument to. The layout must already be open.")] string layoutName,
        [Description("ControlDesk instrument type/library name, e.g. \"Variable Array\".")] string instrumentType,
        [Description("Unique name for the new instrument within the layout, e.g. \"MyVariableArray\".")] string instrumentName,
        [Description("X position of the instrument on the layout, in layout units. Default 0.")] int x = 0,
        [Description("Y position of the instrument on the layout, in layout units. Default 0.")] int y = 0,
        [Description("Width of the instrument, in layout units, e.g. 400.")] int width = 400,
        [Description("Height of the instrument, in layout units, e.g. 200.")] int height = 200,
        CancellationToken cancellationToken = default)
    {
        ToolHelpers.RequireNonBlank(layoutName, nameof(layoutName));
        ToolHelpers.RequireNonBlank(instrumentType, nameof(instrumentType));
        ToolHelpers.RequireNonBlank(instrumentName, nameof(instrumentName));
        ToolHelpers.RequirePositive(width, nameof(width));
        ToolHelpers.RequirePositive(height, nameof(height));

        try
        {
            return await _controlDesk.CreateInstrumentAsync(layoutName, instrumentType, instrumentName, x, y, width, height, cancellationToken);
        }
        catch (ControlDeskException ex)
        {
            throw ToolHelpers.ToMcpError(ex);
        }
    }

    [McpServerTool(Name = "list_layout_instruments", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("List all instruments contained in a given layout, including their positions on the layout.")]
    public async Task<IReadOnlyList<InstrumentInfo>> ListLayoutInstruments(
        [Description("Name of the layout whose instruments should be listed.")] string layoutName,
        CancellationToken cancellationToken = default)
    {
        ToolHelpers.RequireNonBlank(layoutName, nameof(layoutName));

        try
        {
            return await _controlDesk.ListInstrumentsAsync(layoutName, cancellationToken);
        }
        catch (ControlDeskException ex)
        {
            throw ToolHelpers.ToMcpError(ex);
        }
    }

    [McpServerTool(Name = "inspect_instrument", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Return the details (name, position, size, and custom data) of a specific instrument within a given layout.")]
    public async Task<InstrumentInfo> InspectInstrument(
        [Description("Name of the layout that contains the instrument.")] string layoutName,
        [Description("Name of the instrument to inspect.")] string instrumentName,
        CancellationToken cancellationToken = default)
    {
        ToolHelpers.RequireNonBlank(layoutName, nameof(layoutName));
        ToolHelpers.RequireNonBlank(instrumentName, nameof(instrumentName));

        try
        {
            return await _controlDesk.InspectInstrumentAsync(layoutName, instrumentName, cancellationToken);
        }
        catch (ControlDeskException ex)
        {
            throw ToolHelpers.ToMcpError(ex);
        }
    }
}
