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

## Documentation

- `docs/architecture.md`
- `docs/agent-workflow.md`

