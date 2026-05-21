# DotNetAgenticRepairKit

DotNetAgenticRepairKit is a custom .NET agentic AI remediation skeleton for a public portfolio demo.

The repository is intended to show how a .NET application can be organized for software repair workflows, including:

- A Blazor web interface for reviewing repair sessions.
- A core domain library for remediation concepts and contracts.
- An infrastructure library for adapters, persistence, and external tool integrations.
- A console agent for running repair workflows from the command line.
- Focused xUnit test projects for core behavior and agent behavior.

No real API keys, credentials, or provider-specific secrets are included.

## Projects

| Path | Purpose |
| --- | --- |
| `src/RepairKit.Web` | Blazor web app |
| `src/RepairKit.Core` | Core domain and abstractions |
| `src/RepairKit.Infrastructure` | Infrastructure adapters and integrations |
| `src/RepairKit.Agent` | Console agent runner |
| `tests/RepairKit.Tests` | Core unit tests |
| `tests/RepairKit.Agent.Tests` | Agent-focused tests |

## Getting Started

From CMD or PowerShell:

```cmd
dotnet restore
dotnet build
dotnet test
```

Run the web app:

```cmd
dotnet run --project src\RepairKit.Web
```

Run the agent:

```cmd
dotnet run --project src\RepairKit.Agent
```

## Agent Runner v1

The current agent runner is deterministic and does not call any AI services. It first runs `dotnet build --no-incremental`, then runs `dotnet test --no-build` only if the build succeeds. This avoids stale incremental build output when controlled repair scenarios overwrite source files.

Each run writes structured output under `.agent\runs\<runId>\`:

- `build-output.txt`
- `test-output.txt`
- `run-summary.json`

When a build or test run fails, the agent also writes deterministic repair-planning context:

- `context-packet.md`
- `context-metadata.json`

Run it from the repository root:

```cmd
dotnet run --project src\RepairKit.Agent
```

The agent currently only builds, runs tests, records output, and collects deterministic failure context. AI planning, repair selection, and patch application are reserved for later phases.

## Documentation

- `docs/architecture.md`
- `docs/agent-workflow.md`
- `docs/repair-scenarios.md`

## Controlled Repair Scenarios

This repository includes controlled bug scripts for demonstrating future AI agent repair behavior. They do not add AI integration; they only copy known buggy service implementations over the current local files so tests fail in predictable ways.

Example:

```cmd
scripts\introduce-critical-sla-bug.cmd
dotnet test
scripts\restore-clean-services.cmd
dotnet test
```

Available scenarios:

```cmd
scripts\introduce-critical-sla-bug.cmd
scripts\introduce-closed-ticket-reopen-bug.cmd
scripts\introduce-enterprise-escalation-priority-bug.cmd
```

Safety notes:

- These scripts are intentionally destructive to local working files.
- Commit or stash work before running a scenario.
- `scripts\restore-clean-services.cmd` returns the service files to the known-good versions.
