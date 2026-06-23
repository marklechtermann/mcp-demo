using DSpace.ControlDesk.Mcp.Models;
using ModelContextProtocol.Protocol;
using Xunit;

namespace DSpace.ControlDesk.Mcp.Tests;

/// <summary>
/// End-to-end integration tests that drive a real ControlDesk instance through the
/// MCP server. They require ControlDesk to be installed (the COM server registered)
/// and therefore run on Windows only. Each test creates the resources it needs and
/// deletes the project again afterwards so repeated runs stay isolated.
/// </summary>
public sealed class ControlDeskIntegrationTests : IClassFixture<ControlDeskServerFixture>
{
    private readonly ControlDeskServerFixture _fixture;

    public ControlDeskIntegrationTests(ControlDeskServerFixture fixture) => _fixture = fixture;

    private static void SkipIfControlDeskUnavailable()
    {
        Assert.SkipUnless(
            TestEnvironment.IsControlDeskRegistered,
            "ControlDesk COM server is not registered; integration tests run on Windows with ControlDesk installed.");
    }

    private static string UniqueName(string prefix) => $"{prefix}_{Guid.NewGuid():N}".Substring(0, prefix.Length + 9);

    [Fact]
    public async Task Full_workflow_creates_inspects_and_cleans_up()
    {
        SkipIfControlDeskUnavailable();

        string projectName = UniqueName("McpItProj");
        string experimentName = "Experiment1";
        string layoutName = UniqueName("McpItLayout");
        string instrumentName = UniqueName("McpItInstr");
        const string instrumentType = "Variable Array";

        string projectRoot = Path.Combine(Path.GetTempPath(), "DSpaceMcpTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(projectRoot);

        try
        {
            // 1. Create a project together with an experiment.
            var experiment = await _fixture.CallAsync<ExperimentInfo>("create_experiment_with_project", new()
            {
                ["projectName"] = projectName,
                ["experimentName"] = experimentName,
                ["projectRoot"] = projectRoot,
            });

            Assert.Equal(projectName, experiment.ProjectName);
            Assert.Equal(experimentName, experiment.ExperimentName);

            // 2. Create a layout.
            var layout = await _fixture.CallAsync<LayoutInfo>("create_layout", new()
            {
                ["layoutName"] = layoutName,
            });
            Assert.Equal(layoutName, layout.Name);

            // 3. List layouts -> contains the new one.
            var layouts = await _fixture.CallAsync<List<LayoutInfo>>("list_layouts", new());
            Assert.Contains(layouts, l => l.Name == layoutName);

            // 4. Create an instrument on the layout.
            var instrument = await _fixture.CallAsync<InstrumentInfo>("create_instrument", new()
            {
                ["layoutName"] = layoutName,
                ["instrumentType"] = instrumentType,
                ["instrumentName"] = instrumentName,
                ["x"] = 0,
                ["y"] = 0,
                ["width"] = 400,
                ["height"] = 200,
            });
            Assert.Equal(instrumentName, instrument.Name);
            Assert.Equal(layoutName, instrument.LayoutName);

            // 5. List the instruments of the layout -> contains the new one.
            var instruments = await _fixture.CallAsync<List<InstrumentInfo>>("list_layout_instruments", new()
            {
                ["layoutName"] = layoutName,
            });
            Assert.Contains(instruments, i => i.Name == instrumentName);

            // 6. Inspect the instrument.
            var inspected = await _fixture.CallAsync<InstrumentInfo>("inspect_instrument", new()
            {
                ["layoutName"] = layoutName,
                ["instrumentName"] = instrumentName,
            });
            Assert.Equal(instrumentName, inspected.Name);
            Assert.Equal(400, inspected.Width);
            Assert.Equal(200, inspected.Height);

            // 7. Delete the project (cleanup is part of the tested behavior).
            var deletion = await _fixture.CallAsync<ProjectDeletionResult>("delete_project", new()
            {
                ["projectName"] = projectName,
                ["projectRoot"] = projectRoot,
            });
            Assert.True(deletion.Deleted);
        }
        finally
        {
            await TryDeleteProjectAsync(projectName, projectRoot);
            TryDeleteDirectory(projectRoot);
        }
    }

    [Fact]
    public async Task Inspect_unknown_instrument_returns_machine_parseable_error()
    {
        SkipIfControlDeskUnavailable();

        string projectName = UniqueName("McpErrProj");
        string layoutName = UniqueName("McpErrLayout");
        string projectRoot = Path.Combine(Path.GetTempPath(), "DSpaceMcpTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(projectRoot);

        try
        {
            await _fixture.CallAsync<ExperimentInfo>("create_experiment_with_project", new()
            {
                ["projectName"] = projectName,
                ["experimentName"] = "Experiment1",
                ["projectRoot"] = projectRoot,
            });
            await _fixture.CallAsync<LayoutInfo>("create_layout", new() { ["layoutName"] = layoutName });

            CallToolResult result = await _fixture.CallRawAsync("inspect_instrument", new()
            {
                ["layoutName"] = layoutName,
                ["instrumentName"] = "DoesNotExist",
            });

            Assert.True(result.IsError);
            string message = ControlDeskServerFixture.DescribeError(result);
            Assert.Contains("instrument_not_found", message);
        }
        finally
        {
            await TryDeleteProjectAsync(projectName, projectRoot);
            TryDeleteDirectory(projectRoot);
        }
    }

    private async Task TryDeleteProjectAsync(string projectName, string projectRoot)
    {
        try
        {
            await _fixture.CallRawAsync("delete_project", new()
            {
                ["projectName"] = projectName,
                ["projectRoot"] = projectRoot,
            });
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup.
        }
    }
}
