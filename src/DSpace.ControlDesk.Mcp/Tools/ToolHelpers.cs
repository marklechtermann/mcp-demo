using DSpace.ControlDesk.Mcp.ControlDesk;
using ModelContextProtocol;

namespace DSpace.ControlDesk.Mcp.Tools;

/// <summary>Helpers shared by the MCP tool classes for input validation and error mapping.</summary>
internal static class ToolHelpers
{
    /// <summary>Validates that a required string argument is non-empty.</summary>
    public static string RequireNonBlank(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new McpException($"[invalid_input] permanent: '{parameterName}' must not be empty.");
        }

        return value;
    }

    /// <summary>Validates that a dimension argument is positive.</summary>
    public static int RequirePositive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new McpException($"[invalid_input] permanent: '{parameterName}' must be greater than zero (was {value}).");
        }

        return value;
    }

    /// <summary>Maps a <see cref="ControlDeskException"/> to an MCP error with a machine-parseable code.</summary>
    public static McpException ToMcpError(ControlDeskException exception)
    {
        string severity = exception.Transient ? "transient" : "permanent";
        return new McpException($"[{exception.Code}] {severity}: {exception.Message}");
    }
}
