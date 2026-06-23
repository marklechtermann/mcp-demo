using ModelContextProtocol.Client;
using Xunit;

namespace DSpace.ControlDesk.Mcp.Tests;

/// <summary>
/// Verifies that every required tool is registered with the correct name and
/// safety annotations. These checks exercise the MCP surface only and do not
/// require a running ControlDesk instance.
/// </summary>
public sealed class ToolRegistrationTests : IClassFixture<ControlDeskServerFixture>
{
    private readonly ControlDeskServerFixture _fixture;

    public ToolRegistrationTests(ControlDeskServerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task All_seven_tools_are_registered()
    {
        IList<McpClientTool> tools = await _fixture.Client.ListToolsAsync();

        string[] expected =
        [
            "create_experiment_with_project",
            "create_instrument",
            "list_layouts",
            "list_layout_instruments",
            "create_layout",
            "inspect_instrument",
            "delete_project",
        ];

        string[] actual = tools.Select(t => t.Name).OrderBy(n => n).ToArray();

        Assert.Equal(expected.OrderBy(n => n).ToArray(), actual);
    }

    [Fact]
    public async Task Read_only_tools_are_annotated_read_only()
    {
        IList<McpClientTool> tools = await _fixture.Client.ListToolsAsync();

        foreach (string name in new[] { "list_layouts", "list_layout_instruments", "inspect_instrument" })
        {
            McpClientTool tool = tools.Single(t => t.Name == name);
            Assert.True(tool.ProtocolTool.Annotations?.ReadOnlyHint, $"'{name}' should be annotated readOnlyHint=true.");
        }
    }

    [Fact]
    public async Task Delete_project_is_annotated_destructive()
    {
        IList<McpClientTool> tools = await _fixture.Client.ListToolsAsync();

        McpClientTool tool = tools.Single(t => t.Name == "delete_project");
        Assert.True(tool.ProtocolTool.Annotations?.DestructiveHint, "'delete_project' should be annotated destructiveHint=true.");
    }

    [Fact]
    public async Task Every_tool_has_a_description()
    {
        IList<McpClientTool> tools = await _fixture.Client.ListToolsAsync();

        Assert.All(tools, tool => Assert.False(string.IsNullOrWhiteSpace(tool.Description)));
    }
}
