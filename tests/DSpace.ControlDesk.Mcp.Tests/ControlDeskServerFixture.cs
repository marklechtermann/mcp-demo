using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit;

namespace DSpace.ControlDesk.Mcp.Tests;

/// <summary>
/// Starts the MCP server over stdio and exposes an <see cref="McpClient"/> for the tests.
/// One server process is shared by all tests in a class via <see cref="IClassFixture{T}"/>.
/// </summary>
public sealed class ControlDeskServerFixture : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public McpClient Client { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        string serverAssembly = TestEnvironment.LocateServerAssembly();

        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "dspace-controldesk-mcp",
            Command = "dotnet",
            Arguments = [serverAssembly],
        });

        Client = await McpClient.CreateAsync(transport);
    }

    public async ValueTask DisposeAsync()
    {
        if (Client is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }

    /// <summary>Calls a tool and deserializes its structured result into <typeparamref name="T"/>.</summary>
    public async Task<T> CallAsync<T>(string toolName, Dictionary<string, object?> arguments, CancellationToken cancellationToken = default)
    {
        CallToolResult result = await Client.CallToolAsync(toolName, arguments, cancellationToken: cancellationToken);

        if (result.IsError == true)
        {
            throw new InvalidOperationException($"Tool '{toolName}' returned an error: {DescribeError(result)}");
        }

        JsonElement element;
        if (result.StructuredContent is JsonElement structured)
        {
            element = structured;
        }
        else
        {
            // Some tools surface their payload as a JSON text content block rather than
            // structured content; parse the concatenated text as JSON in that case.
            string text = string.Join(string.Empty, result.Content
                .OfType<TextContentBlock>()
                .Select(block => block.Text));

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException($"Tool '{toolName}' returned no content.");
            }

            using JsonDocument document = JsonDocument.Parse(text);
            element = document.RootElement.Clone();
        }

        // Tools that return a list are wrapped by the SDK as { "result": [...] }.
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("result", out JsonElement inner))
        {
            return inner.Deserialize<T>(JsonOptions)!;
        }

        return element.Deserialize<T>(JsonOptions)!;
    }

    /// <summary>Calls a tool and returns the raw result (for asserting error behavior).</summary>
    public async Task<CallToolResult> CallRawAsync(string toolName, Dictionary<string, object?> arguments, CancellationToken cancellationToken = default)
        => await Client.CallToolAsync(toolName, arguments, cancellationToken: cancellationToken);

    public static string DescribeError(CallToolResult result)
    {
        IEnumerable<string> texts = result.Content
            .OfType<TextContentBlock>()
            .Select(block => block.Text);

        return string.Join(" ", texts);
    }
}
