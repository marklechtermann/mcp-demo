using DSpace.ControlDesk.Mcp.ControlDesk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// dSPACE ControlDesk MCP server.
// Transport: stdio only (stdout is reserved for the MCP protocol; all logging goes to stderr).

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Route every log to stderr so that nothing pollutes the stdio MCP channel.
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

// A single ControlDesk client shared by all tools. It marshals every COM call
// onto one dedicated STA thread, which is required by ControlDesk's COM server.
builder.Services.AddSingleton<IControlDeskClient, ControlDeskClient>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
