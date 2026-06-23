# It's a temporary demo 

A [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) server that
automates [dSPACE ControlDesk](https://www.dspace.com/en/pub/home/products/sw/experimentandvisualization/controldesk.cfm)
through its **COM automation** interface. It lets an LLM client create projects
and experiments, manage layouts, and add/inspect instruments programmatically.

- **Language / runtime:** C# on **.NET 10**
- **Transport:** **stdio** (standard input/output) — no HTTP
- **Platform:** **Windows only**, **win-x64** (ControlDesk COM automation
  requires a 64-bit client)

Download the latest release from [GitHub](https://github.com/marklechtermann/mcp-demo/releases).

## Tools

| Tool | Description | Annotations |
| --- | --- | --- |
| `create_experiment_with_project` | Create a new project together with an experiment and activate the experiment. | write |
| `create_instrument` | Add a new instrument of a given type to an open layout. | write |
| `list_layouts` | List all currently available layouts. | read-only |
| `list_layout_instruments` | List the instruments contained in a given layout. | read-only |
| `create_layout` | Create a new layout. | write |
| `inspect_instrument` | Return the details of a specific instrument within a layout. | read-only |
| `delete_project` | Delete a project (including its experiments and data) from disk. | destructive |

### Tool inputs

- **`create_experiment_with_project`** — `projectName` (string, required),
  `experimentName` (string, required), `projectRoot` (string, optional absolute
  path; the active project root is used when omitted).
- **`create_instrument`** — `layoutName` (string, required), `instrumentType`
  (string, required — a ControlDesk instrument-library name such as
  `"Variable Array"`, `"Time Plotter"`, or `"Knob"`), `instrumentName`
  (string, required), `x` (int, default `0`), `y` (int, default `0`),
  `width` (int, default `400`), `height` (int, default `200`).
- **`list_layouts`** — no parameters.
- **`list_layout_instruments`** — `layoutName` (string, required).
- **`create_layout`** — `layoutName` (string, required).
- **`inspect_instrument`** — `layoutName` (string, required), `instrumentName`
  (string, required).
- **`delete_project`** — `projectName` (string, required), `projectRoot`
  (string, optional).

Errors from the automation layer are returned as MCP tool errors with a
machine-parseable prefix `[<code>] <permanent|transient>: <message>` (for
example `[instrument_not_found] permanent: ...`).

## Prerequisites

- **Windows** (x64).
- **dSPACE ControlDesk** installed and registered (the COM ProgID
  `ControlDeskNG.Application` must be available). A running ControlDesk
  instance is required at runtime and for the integration tests.
- **.NET 10 SDK** (to build and to run the framework-dependent build). The
  released single-file executable bundles the runtime and does **not** require
  a separate .NET installation.

## Build

```powershell
dotnet build -c Release
```

## Run

The server communicates over stdio and is normally launched by an MCP client.
To run it directly:

```powershell
dotnet run --project src/DSpace.ControlDesk.Mcp -c Release
```

All diagnostic logging is written to **stderr**; **stdout** is reserved for the
MCP protocol stream.

### Configure an MCP client

Point your MCP client at the built executable (or `dotnet` + the project/DLL).
Example client configuration:

```json
{
  "servers": {
    "dspace-controldesk": {
      "command": "dspace-controldesk-mcp.exe",
      "args": []
    }
  }
}
```

During development you can instead use:

```json
{
  "servers": {
    "dspace-controldesk": {
      "command": "dotnet",
      "args": ["run", "--project", "src/DSpace.ControlDesk.Mcp", "-c", "Release"]
    }
  }
}
```

## Tests

```powershell
dotnet test
```
