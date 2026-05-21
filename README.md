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

The current agent runner first runs `dotnet build --no-incremental`, then runs `dotnet test --no-build` only if the build succeeds. This avoids stale incremental build output when controlled repair scenarios overwrite source files.

The agent loads `repairkit.config.json` from the repository root by default. If the file is missing, it falls back to built-in defaults for this demo. This configuration is what makes the repair loop reusable for other .NET solutions.

Configurable values include:

- target solution path
- target repository root
- agent output path
- build/test command templates
- Git diff command
- allowed edit paths
- blocked path segments and secret/config terms
- maximum context size
- recent history limit

Each run writes structured output under `.agent\runs\<runId>\`:

- `build-output.txt`
- `test-output.txt`
- `run-summary.json`

When a build or test run fails, the agent also writes deterministic repair-planning context:

- `context-packet.md`
- `context-metadata.json`

By default, failed runs send the context packet to OpenRouter and write a plan-only AI repair plan. After a valid plan is generated, the agent requires approval before applying full-file replacements from `repair-plan.json`.

- `model-request.json`
- `model-response.raw.txt`
- `repair-plan.json`
- `approval-decision.json` after an approval decision
- `patch-application.json` after patch application
- `validation-build-output.txt`
- `validation-test-output.txt`
- `git-diff.patch`
- `repair-report.md`
- `ai-error.txt` if AI planning fails
- `patch-error.txt` if patch application fails

Applied changes are backed up under `.agent\runs\<runId>\backups\` before any source file is modified. After patching, the agent runs `dotnet build --no-incremental` and `dotnet test --no-build` again from an isolated validation output folder.

After patch validation, the agent captures `git diff -- src tests` to `git-diff.patch` and writes a human-readable `repair-report.md` summarizing the run, plan, approval decision, patch result, validation result, and diff excerpt.

Each completed run also appends a compact local history entry to `.agent\history.jsonl`. This file is gitignored. It is intended for lightweight memory and future dashboard features. It stores run metadata such as outcomes, target files, risk level, approval status, validation status, and token usage when OpenRouter provides it. It does not store API keys, full prompts, full context packets, model responses, source files, or full diffs.

When a new failure context is generated, the agent reads recent local history and appends a short `Recent Related Runs` section to `context-packet.md` if prior runs mention the same ticket service. That memory section contains only run IDs, outcomes, summaries, target files, and validation status.

Environment variables:

- `OPENROUTER_API_KEY` is required for AI planning.
- `REPAIRKIT_MODEL` is optional and defaults to `openai/gpt-5.2`.
- `REPAIRKIT_OPENROUTER_APP_TITLE` is optional and defaults to `DotNetAgenticRepairKit`.
- `REPAIRKIT_OPENROUTER_HTTP_REFERER` is optional.

Run it from the repository root:

```cmd
dotnet run --project src\RepairKit.Agent
```

Run with an explicit config:

```cmd
dotnet run --project src\RepairKit.Agent --config repairkit.config.json
```

Target another solution directly:

```cmd
dotnet run --project src\RepairKit.Agent --solution H:\Projects\SomeOtherApp\SomeOtherApp.sln --repo-root H:\Projects\SomeOtherApp
```

Use a custom agent output folder:

```cmd
dotnet run --project src\RepairKit.Agent --agent-output H:\Projects\SomeOtherApp\.agent
```

Run without AI planning:

```cmd
dotnet run --project src\RepairKit.Agent -- --no-ai
```

Run with an explicit model:

```cmd
set OPENROUTER_API_KEY=<your key>
set REPAIRKIT_MODEL=openai/gpt-5.2
dotnet run --project src\RepairKit.Agent
```

Run a full Phase 6 approval flow:

```cmd
call set-agent-env.local.cmd
scripts\introduce-critical-sla-bug.cmd
dotnet run --project src\RepairKit.Agent
```

Then type:

```cmd
APPLY
```

Plan without prompting for approval:

```cmd
dotnet run --project src\RepairKit.Agent --plan-only
```

Auto-approve only low-risk plans:

```cmd
dotnet run --project src\RepairKit.Agent --approve-plan
```

Plan and approve without applying:

```cmd
dotnet run --project src\RepairKit.Agent --no-apply
```

Force manual approval even when `--approve-plan` is present:

```cmd
dotnet run --project src\RepairKit.Agent --require-approval
```

Full repair demo:

```cmd
call set-agent-env.local.cmd
scripts\introduce-critical-sla-bug.cmd
dotnet run --project src\RepairKit.Agent
```

Then type:

```cmd
APPLY
```

Expected result:

- `Patch Applied: true`
- `Validation Overall Passed: true`
- `git-diff.patch` generated
- `repair-report.md` generated

Then run:

```cmd
dotnet test
```

Inspect the report and diff:

```cmd
type .agent\runs\<runId>\repair-report.md
type .agent\runs\<runId>\git-diff.patch
```

Inspect local history:

```cmd
type .agent\history.jsonl
```

You can run the agent multiple times and inspect appended history entries:

```cmd
dotnet run --project src\RepairKit.Agent -- --no-ai
dotnet run --project src\RepairKit.Agent -- --no-ai
type .agent\history.jsonl
```

## Documentation

- `docs/architecture.md`
- `docs/agent-workflow.md`
- `docs/external-solution-integration.md`
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
