# Implementation Notes

This document records the conventions and API details the implementation relies
on, with citations to the two mandated research sources.

## Sources

1. **Architecture Wiki — MCP Design Guidelines** and the referenced
   **MCP Server Development Skill** (step-by-step guide for C# and Python),
   from the organization's Architecture Wiki / CodingAssistantLibrary. Used for
   project layout, transport choice, tool registration, logging, and packaging
   conventions.
2. **dSPACE ControlDesk Automation** documentation (release **RLS2025-B**),
   retrieved via the dSPACE documentation MCP server using the `ControlDesk`
   product filter. Used for the COM automation object model and every
   ControlDesk interaction.

## MCP conventions applied (from the Architecture Wiki guidelines)

- **C# / .NET** server using the official `ModelContextProtocol` NuGet package
  together with `Microsoft.Extensions.Hosting`.
- **stdio transport**: the host is built with
  `AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly()` in
  [Program.cs](../src/DSpace.ControlDesk.Mcp/Program.cs).
- **Logging goes to stderr only.** stdout carries the MCP protocol stream, so
  the console logger is configured with
  `LogToStandardErrorThreshold = LogLevel.Trace`. Writing logs to stdout would
  corrupt the protocol.
- **Tool grouping and naming**: related tools are grouped in classes annotated
  with `[McpServerToolType]`; each tool method is annotated with
  `[McpServerTool]` and uses **snake_case** tool names. Every tool and every
  parameter carries a `[Description]` so the generated input schema is
  self-describing.
- **Tool annotations** reflect behavior: read-only tools (`list_layouts`,
  `list_layout_instruments`, `inspect_instrument`) set `ReadOnly = true` /
  `Idempotent = true`; `delete_project` sets `Destructive = true`. All tools set
  `OpenWorld = false` because they operate against the local ControlDesk
  instance only.
- **Typed inputs/outputs**: tools accept typed parameters and return typed
  records ([Models/Contracts.cs](../src/DSpace.ControlDesk.Mcp/Models/Contracts.cs)),
  which the SDK serializes into the tool result.
- **Dependency injection**: the COM automation client is registered as a
  singleton (`IControlDeskClient`) and injected into the tool classes.

## ControlDesk COM automation API (from the dSPACE documentation)

### Entry point

- ControlDesk's automation interface is a **COM** API. The **only creatable
  object** is the application, exposed through the **`IXaApplication`**
  interface with ProgID **`ControlDeskNG.Application`**.
- The client **must be 64-bit**; 32-bit automation clients are not supported.
  The project therefore targets `win-x64` / `Platform=x64`.
- The server uses **late binding** via C# `dynamic`
  ([ControlDeskClient.cs](../src/DSpace.ControlDesk.Mcp/ControlDesk/ControlDeskClient.cs)),
  obtaining the type with `Type.GetTypeFromProgID("ControlDeskNG.Application")`
  and creating it with `Activator.CreateInstance`. Late binding lets the server
  build and run without the ControlDesk interop assemblies (which ship only
  with a local ControlDesk installation).
- COM is apartment-threaded, so all COM calls are marshalled onto a single
  dedicated **STA thread**
  ([StaThreadDispatcher.cs](../src/DSpace.ControlDesk.Mcp/ControlDesk/StaThreadDispatcher.cs)).

### Object model used

| Capability | API path (per the documentation) |
| --- | --- |
| Project roots | `app.ProjectRoots.Add(path)`, `.Contains(path)`, `.Item(path).Activate()`; `app.ActiveProjectRoot.PathName` |
| Create project | `app.Projects.Add(projectName)` — `IXaProjects.Add` returns the new **active** project (`IXaActiveProject`) |
| Create experiment | `project.Experiments.Add(experimentName, autoSaveActiveExperiment)` — `IXaExperiments.Add` returns the new **active** experiment (`IXaActiveExperiment`), which is already active |
| Close active project | `app.ActiveProject.Close(false)` |
| Delete project | `app.Projects.Item(projectName).Remove(true)` (`true` also deletes the files from disk) |
| Layouts | `app.LayoutManagement.Layouts.Add(name)`, iterate the `Layouts` collection, `layout.Name`, `layout.Instruments` |
| Instruments | `layout.Instruments.Add(typeName, name, x, y, width, height)`, `.Contains(name)`, `.Item(nameOrIndex)`, `.Count`; `instrument.Name`, `instrument.Position.X/.Y/.Width/.Height`, `instrument.CustomData` |
| Instrument types | from `app.LayoutManagement.InstrumentLibraries.Item("ControlDeskInstruments")`, e.g. `"Variable Array"`, `"Time Plotter"`, `"Knob"` |

### Key API subtlety (verified against the documentation and a live instance)

`IXaExperiments.Add(name, autoSaveActiveExperiment)` returns the
**`IXaActiveExperiment`** object — the experiment it creates is **already
active**. The `Activate(...)` method exists only on the *inactive*
`IXaExperiment` interface (the type returned by `Experiments.Item(name)`). Calling
`Activate()` on the freshly created experiment therefore fails with a missing-member
error. The implementation creates the experiment with a single
`Experiments.Add(experimentName, true)` call and does **not** call `Activate`
afterwards.

## Error handling

- COM failures (`COMException`) are mapped to a `controldesk_com_error` code;
  missing members from late binding (`RuntimeBinderException`) map to
  `controldesk_api_mismatch`; an unregistered/unavailable COM server maps to
  `controldesk_not_installed` / `controldesk_unavailable`.
- Domain conditions surface dedicated codes such as `layout_not_found`,
  `instrument_not_found`, `instrument_already_exists`, and `project_not_found`.
- All codes are returned to the MCP client as tool errors formatted as
  `[<code>] <permanent|transient>: <message>`, so clients can react
  programmatically.

## Testing approach

- Integration tests start the server as a child process over stdio using the
  MCP **client** from the same `ModelContextProtocol` package, then call the
  tools end to end against a running ControlDesk instance.
- Tests are skipped automatically when ControlDesk's COM server is not
  registered, and each test deletes the project it creates in a `finally`
  block so runs remain isolated.
