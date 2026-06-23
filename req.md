# Demo Prompt: Build a .NET 10 MCP Server for dSPACE ControlDesk Automation

> Use this prompt as-is with an LLM coding agent. It is a self-contained task
> description. The agent is expected to research via the referenced MCP servers
> before writing any code.

---

## Your Task

Build a **Model Context Protocol (MCP) server** that automates **dSPACE
ControlDesk**. The server must be written in **C# targeting .NET 10**.

The server must use the **stdio (standard I/O) transport** — **not HTTP**.

All ControlDesk interactions must be implemented through the **ControlDesk COM
automation** interface. Determine the exact COM entry point, interfaces, and
members yourself from the dSPACE documentation (see Research First) — do not
assume them. Note that the automation client must be **64-bit**.

The MCP server exposes tools that let an LLM client drive ControlDesk
programmatically. Implement the functionality described under
**Required Capabilities** below.

## Research First (mandatory)

Before writing any code, gather the required knowledge by querying the
available MCP servers. **Do not guess** APIs, project structure, or
conventions — look them up.

1. **Architecture Wiki MCP server** — Query it to learn **how an MCP server is
   to be implemented** in this organization: the expected project layout,
   coding conventions, transport (stdio/HTTP), tool registration patterns,
   logging, configuration, packaging, and any mandated libraries or templates.
   **Before writing any code, download the complete MCP guideline document
   (the "MCP Design Guidelines") from the Architecture Wiki in full and read and
   understand it end to end** — do not rely on isolated search snippets.
   Follow these conventions exactly, and use the **"MCP Server Development
   Skill" (step-by-step guide for C# and Python)** referenced there from the
   CodingAssistantLibrary.

2. **dSPACE documentation MCP server** — Query it to learn **how ControlDesk
   COM automation works** (use the **`ControlDesk`** product filter): the COM
   automation API/object model, how to create experiments and projects, how to
   add instruments, how layouts work, and how to enumerate layouts and the
   instruments they contain. Base every ControlDesk interaction on what this
   documentation specifies.

   **If no ControlDesk/dSPACE documentation MCP server is available**, do
   **not** guess the COM automation API. Stop and ask the user for the correct
   documentation source instead of inventing API calls.

Cite the relevant findings from both sources in your implementation notes so it
is clear which API calls and conventions you relied on.

## Required Capabilities

The MCP server must offer the following tools:

1. **Create a ControlDesk experiment with a project**
   Create a new experiment together with its associated project.

2. **Create an instrument**
   Add a new instrument.

3. **List all layouts**
   Return the list of layouts that are currently available/displayed.

4. **List the instruments of a layout**
   For a given layout, return the list of instruments it contains.

5. **Create a layout**
   Add a new layout.

6. **Inspect an instrument in a layout**
   View/return the details of a specific instrument within a given layout.

7. **Delete a project**
   Delete a project (including its experiment/demo data) via the MCP server.
   This ensures any demo or test project that is created can be removed again,
   so test runs do not leave artifacts behind or cause conflicts.

## Testing

- Create a dedicated **test project** for the MCP server.
- These are **integration tests** (not pure unit tests): they require a
  **running ControlDesk instance** and the ControlDesk COM automation, so they
  run **locally on Windows only**.
- Tests should create the resources they need (e.g. a demo project) and
  **clean up afterwards** by deleting the project again, so repeated test runs
  stay isolated and conflict-free.

## Deliverables

- A buildable .NET 10 MCP server project implementing all seven tools above.
- A dedicated test project that validates the tools against a running
  ControlDesk instance and cleans up the projects it creates.
- Each tool exposed as a proper MCP tool with a clear name, description, and
  typed input/output schema.
- Robust error handling for the ControlDesk automation calls.
- A short README describing how to build, configure, and run the server, plus
  the prerequisites (ControlDesk installation, .NET 10 SDK).
- Implementation notes referencing the conventions from the Architecture Wiki
  and the API details from the dSPACE documentation.

## Constraints

- Target framework: **.NET 10** (C#).
- Transport: **stdio only — no HTTP**.
- Drive ControlDesk exclusively via its **COM automation** interface.
- Because COM automation is used, the server runs on **Windows only** and must
  be built for the **win-x64** runtime identifier.
- Follow the MCP server conventions obtained from the Architecture Wiki.
- Implement all ControlDesk interactions according to the dSPACE documentation.

## Verification Loop (mandatory)

Do not consider the task done until **every requirement above is verifiably
met**. After implementing, run a verification loop:

1. **Run the integration tests** for the MCP server (locally, against a running
   ControlDesk instance on Windows).
2. If any test fails — or any required capability is missing or incomplete —
   **make corrections** to the code and tests.
3. **Repeat** steps 1–2 until all tests pass and all requirements are fully
   implemented.

Only stop once the full test suite is green and you can confirm that all seven
tools and every constraint have actually been delivered.

## Source Control & Release Pipeline

- The demo is checked into a **GitHub repository**.
- Add a **GitHub Actions release pipeline** that is triggered **only when a new
  tag / GitHub Release is published**.
- The release pipeline must **not run the tests** — they require a local
  ControlDesk instance and are executed locally only.
- On release, the pipeline must **compile the .NET 10 application into a
  self-contained single-file executable** for **win-x64** that bundles the
  .NET 10 runtime, so the resulting binary runs without a separate .NET
  installation.
- The executable is **attached to the GitHub Release** so it can be downloaded
  directly and the MCP server used immediately.
