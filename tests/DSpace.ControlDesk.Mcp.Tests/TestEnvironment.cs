using System.Runtime.Versioning;

namespace DSpace.ControlDesk.Mcp.Tests;

/// <summary>Helpers to locate build artifacts and detect ControlDesk availability.</summary>
internal static class TestEnvironment
{
    /// <summary>ProgID of the ControlDesk COM automation server.</summary>
    public const string ControlDeskProgId = "ControlDeskNG.Application";

    /// <summary>True if a (64-bit) ControlDesk COM server is registered on this machine.</summary>
    [SupportedOSPlatform("windows")]
    public static bool IsControlDeskRegistered =>
        OperatingSystem.IsWindows() && Type.GetTypeFromProgID(ControlDeskProgId) is not null;

    /// <summary>
    /// Locates the built MCP server assembly by walking up to the repository root and
    /// searching the server project's build output for the current configuration.
    /// </summary>
    public static string LocateServerAssembly()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        string configuration = new DirectoryInfo(AppContext.BaseDirectory).Name; // e.g. win-x64

        while (dir is not null)
        {
            string serverBin = Path.Combine(dir.FullName, "src", "DSpace.ControlDesk.Mcp", "bin");
            if (Directory.Exists(serverBin))
            {
                string[] candidates = Directory.GetFiles(serverBin, "dspace-controldesk-mcp.dll", SearchOption.AllDirectories);
                if (candidates.Length > 0)
                {
                    // Prefer an artifact whose path matches the test run's configuration; otherwise newest.
                    string? preferred = candidates
                        .OrderByDescending(File.GetLastWriteTimeUtc)
                        .FirstOrDefault(p => p.Contains(Path.DirectorySeparatorChar + "Debug" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                                          || p.Contains(Path.DirectorySeparatorChar + "Release" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));

                    return preferred ?? candidates.OrderByDescending(File.GetLastWriteTimeUtc).First();
                }
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            "Could not locate the built 'dspace-controldesk-mcp.dll'. Build the server project before running the tests.");
    }
}
