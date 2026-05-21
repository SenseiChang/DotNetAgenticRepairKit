# DotNetAgenticRepairKit

[![CI](https://github.com/SenseiChang/DotNetAgenticRepairKit/actions/workflows/ci.yml/badge.svg)](https://github.com/SenseiChang/DotNetAgenticRepairKit/actions/workflows/ci.yml)

DotNetAgenticRepairKit is a custom .NET agentic AI remediation framework designed to bring AI-assisted repair planning, human approval, safe patching, validation, and observability into C#/.NET software maintenance workflows.

The pipeline can detect failing builds or tests, collect deterministic source context, generate OpenRouter-backed repair plans, require explicit approval, apply guarded full-file patches, rerun validation, capture Git diffs and repair reports, persist local run history, and display run artifacts in a read-only Blazor dashboard.

No real API keys or secrets are committed.

## Project Overview

The repository includes a **Support Ticket Triage Dashboard** as a reference integration target for exercising the remediation workflow end to end. The domain includes ticket severity, customer tier, SLA due dates, assignment, escalation, and status transition rules. These rules are intentionally focused and testable so the agent pipeline can be validated clearly before adapting the framework to larger .NET applications.

The console agent, `RepairKit.Agent`, is the remediation workflow host. It performs deterministic build/test execution, context generation, AI repair planning, approval handling, safe patch application, validation, reporting, and history capture.

The Blazor app, `RepairKit.Web`, provides both the reference ticket triage interface and a read-only dashboard for inspecting local agent run artifacts.

## What This Demonstrates

- Build and test failure detection for a .NET solution.
- Deterministic context generation from failing output.
- OpenRouter-backed AI repair planning using direct `HttpClient` calls.
- Strict JSON repair plan validation.
- Human approval gates before any source file changes.
- Safe full-file replacement with path validation and backups.
- Post-patch build and test validation.
- Git diff capture, repair reporting, and compact local run history.
- RAG-lite repository indexing and deterministic file retrieval.
- A read-only Blazor Agent Dashboard for inspecting runs and artifacts.
- External solution configuration through `repairkit.config.json`.

## Architecture

| Project | Purpose |
| --- | --- |
| `src/RepairKit.Web` | Blazor UI for the ticket dashboard and read-only agent dashboard |
| `src/RepairKit.Core` | Ticket domain models, enums, and core services |
| `src/RepairKit.Infrastructure` | Read-only dashboard artifact access and infrastructure helpers |
| `src/RepairKit.Agent` | Console remediation agent |
| `tests/RepairKit.Tests` | Ticket domain tests |
| `tests/RepairKit.Agent.Tests` | Agent helper, validation, planning, patching, reporting, history, and dashboard tests |

High-level flow:

```text
dotnet build/test
      |
      v
failure detected
      |
      v
repo-index.json
      |
      v
context-packet.md
      |
      v
OpenRouter repair plan
      |
      v
human approval
      |
      v
safe patch + backup
      |
      v
validation build/test
      |
      v
git diff + repair report + history
      |
      v
read-only Blazor dashboard
```

See [docs/architecture.md](docs/architecture.md) for more detail.

## Safety Model

Patch application is guarded by:

- A validated `repair-plan.json`.
- An approved `approval-decision.json`.
- Configurable allowed edit paths.
- Blocked path segments such as `.git`, `.agent`, `bin`, `obj`, `scripts`, and `docs`.
- Blocked path terms such as `.env`, `secret`, `token`, `password`, `key`, and `appsettings`.
- Rejection of absolute paths and `..` traversal.
- Backup files under `.agent\runs\<runId>\backups\`.
- Post-patch `dotnet build` and `dotnet test` validation.

The Blazor Agent Dashboard is read-only. It does not run the agent, approve plans, apply patches, call OpenRouter, or display environment variables.

## Quick Start

From CMD:

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

## Continuous Integration

GitHub Actions runs restore, build, and tests for `DotNetAgenticRepairKit.sln` on pushes to `main`, pull requests to `main`, and manual dispatches.

CI intentionally does not call OpenRouter, require `OPENROUTER_API_KEY`, run controlled repair scenarios, or apply patches. AI repair workflows remain local/manual unless a future workflow explicitly adds controlled agent planning.

## Demo Flow

Set local OpenRouter environment variables first. Start from `set-agent-env.example.cmd` and create your own untracked `set-agent-env.local.cmd`.

```cmd
call set-agent-env.local.cmd
dotnet test
scripts\introduce-critical-sla-bug.cmd
dotnet run --project src\RepairKit.Agent
```

When prompted, type:

```cmd
APPLY
```

Expected result:

- Initial tests fail because the controlled SLA bug was introduced.
- The agent generates a context packet and AI repair plan.
- Approval is recorded.
- The patch is applied with a backup.
- Validation build and tests pass.
- `git-diff.patch`, `repair-report.md`, and `.agent\history.jsonl` are written.

Restore the clean demo implementation:

```cmd
scripts\restore-clean-services.cmd
dotnet test
```

See [docs/demo-script.md](docs/demo-script.md) for a 5-minute walkthrough.

## Environment Variables

| Variable | Required | Purpose |
| --- | --- | --- |
| `OPENROUTER_API_KEY` | Yes, unless `--no-ai` or planless modes are used | OpenRouter API key |
| `REPAIRKIT_MODEL` | No | Model name, defaults to `openai/gpt-5.2` |
| `REPAIRKIT_OPENROUTER_APP_TITLE` | No | Optional OpenRouter app title |
| `REPAIRKIT_OPENROUTER_HTTP_REFERER` | No | Optional OpenRouter HTTP referer |

The committed `set-agent-env.example.cmd` uses placeholders only. Do not commit `set-agent-env.local.cmd`.

## Controlled Repair Scenarios

The `scripts` folder contains deterministic bug injectors for demo use:

```cmd
scripts\introduce-critical-sla-bug.cmd
scripts\introduce-closed-ticket-reopen-bug.cmd
scripts\introduce-enterprise-escalation-priority-bug.cmd
```

Restore known-good service files:

```cmd
scripts\restore-clean-services.cmd
```

Safety notes:

- These scripts intentionally overwrite local service files.
- Commit or stash work before running a scenario.
- The restore script returns the service files to the known-good versions.

## Agent Command Examples

Default full flow:

```cmd
dotnet run --project src\RepairKit.Agent
```

Run without AI planning:

```cmd
dotnet run --project src\RepairKit.Agent --no-ai
```

Build the local repository index only:

```cmd
dotnet run --project src\RepairKit.Agent --index
```

Refresh the index before a no-AI diagnostic run:

```cmd
dotnet run --project src\RepairKit.Agent --reindex --no-ai
```

Generate a plan but skip approval and patching:

```cmd
dotnet run --project src\RepairKit.Agent --plan-only
```

Plan and approval only, without patch application:

```cmd
dotnet run --project src\RepairKit.Agent --no-apply
```

Auto-approve low-risk plans only:

```cmd
dotnet run --project src\RepairKit.Agent --approve-plan
```

Force manual approval:

```cmd
dotnet run --project src\RepairKit.Agent --require-approval
```

Use an explicit config:

```cmd
dotnet run --project src\RepairKit.Agent --config repairkit.config.json
```

Target another solution:

```cmd
dotnet run --project src\RepairKit.Agent --solution H:\Projects\SomeOtherApp\SomeOtherApp.sln --repo-root H:\Projects\SomeOtherApp
```

## Dashboard Usage

Run the web app:

```cmd
dotnet run --project src\RepairKit.Web
```

Open:

```text
/agent-dashboard
```

The dashboard reads local `.agent` output generated by `RepairKit.Agent`, including:

- `.agent\history.jsonl`
- run summaries
- repair plans
- approval decisions
- patch application results
- repair reports
- Git diffs
- build and test output files
- retrieval metadata from `context-metadata.json`
- tool execution events from `tool-events.jsonl`
- token usage when available in run history
- local repo index status from `.agent\repo-index.json`

It validates run IDs, reads only known artifact names, and stays read-only.

## RAG-lite Repository Indexing

`RepairKit.Agent` can build a deterministic repository index at `.agent\repo-index.json`. This is RAG-lite retrieval: it uses file metadata, declared types, namespaces, keywords, snippets, and deterministic scoring. It does not use embeddings, a vector database, Semantic Kernel, LangChain, AutoGen, CrewAI, or OpenRouter during indexing.

The current implementation uses `JsonRepoIndexStore` and `RepoIndexContextRetriever`. The retrieval layer is abstracted behind interfaces so future implementations can add vector-backed or hybrid retrieval without changing the rest of the remediation pipeline.

The index is local and gitignored. It excludes generated output, `.git`, `.agent\runs`, `.agent\history.jsonl`, `bin`, `obj`, `.vs`, `node_modules`, test result folders, local env scripts, run artifacts, and paths containing blocked secret/config terms.

Build the index:

```cmd
dotnet run --project src\RepairKit.Agent --index
```

Refresh it before normal execution:

```cmd
dotnet run --project src\RepairKit.Agent --reindex --no-ai
```

When failures occur, `ContextRetriever` ranks indexed files against build/test output and context keywords, then `ContextBuilder` includes the highest-scoring source and test files in `context-packet.md`. If the index is missing or invalid, the old deterministic keyword fallback still runs.

Later phases may add implementations such as `VectorContextRetriever`, `HybridContextRetriever`, `QdrantContextStore`, `PgVectorContextStore`, or `AzureSearchContextStore`, but this phase intentionally stays deterministic and dependency-light.

## MCP-inspired Tool Abstraction

`RepairKit.Agent` includes an MCP-inspired internal tool abstraction. Tools expose names, descriptions, lightweight JSON input schemas, structured results, and run-local audit events in `tool-events.jsonl`.

This is not a full MCP server and does not add external MCP packages. The current implementation is local-only, but it creates a seam for future MCP hosting or LLM tool-calling integration.

Initial tools include:

- `build_solution`
- `run_tests`
- `build_repo_index`
- `build_context_packet`
- `capture_git_diff`
- `write_repair_report`
- `read_artifact`

The existing workflow now uses selected tools for repository indexing, context packet generation, Git diff capture, and repair report writing while preserving the current command-line behavior.

## Docker

Docker support is optional and does not replace normal local CMD usage. The image runs `RepairKit.Agent` in a .NET SDK container and does not bake in API keys or other secrets.

Pass OpenRouter credentials at runtime when AI planning is needed:

```cmd
docker run --rm ^
  -e OPENROUTER_API_KEY=%OPENROUTER_API_KEY% ^
  -e REPAIRKIT_MODEL=openai/gpt-5.2 ^
  dotnet-agentic-repair-kit --plan-only
```

Mounted repo mode is required when patch application should modify the host working tree:

```cmd
docker run --rm -it ^
  -v "%cd%:/workspace" ^
  -w /workspace ^
  -e OPENROUTER_API_KEY=%OPENROUTER_API_KEY% ^
  -e REPAIRKIT_MODEL=openai/gpt-5.2 ^
  dotnet-agentic-repair-kit
```

See [docs/docker.md](docs/docker.md).

## Screenshots

Place screenshots under `docs/screenshots/` when publishing:

- Agent Dashboard
- Repair Report
- Support Ticket Dashboard

## External Solution Configuration

`repairkit.config.json` controls the target solution, repo root, agent output path, command templates, edit allowlist, blocked paths, context size, history limit, repository index path, indexed extensions, and maximum retrieved files.

Example:

```cmd
dotnet run --project src\RepairKit.Agent --config repairkit.config.json
```

An external solution example is available at [docs/examples/repairkit.external-solution.config.example.json](docs/examples/repairkit.external-solution.config.example.json).

See [docs/external-solution-integration.md](docs/external-solution-integration.md).

## Repository Structure

```text
src/
  RepairKit.Agent/
  RepairKit.Core/
  RepairKit.Infrastructure/
  RepairKit.Web/
tests/
  RepairKit.Agent.Tests/
  RepairKit.Tests/
docs/
scripts/
repairkit.config.json
set-agent-env.example.cmd
```

## Current Limitations

- The repair planner depends on OpenRouter availability and model behavior.
- Patch application currently supports full-file replacements only.
- The dashboard is local and read-only.
- RAG-lite retrieval is deterministic keyword/type scoring, not embeddings or vector search.
- No MCP or deployment automation is included.
- The controlled demo scenarios cover a small support-ticket domain rather than a large production system.

## Future Roadmap

- Add CI-based demo execution.
- Add richer dashboard filtering and artifact summaries.
- Support patch/diff application in addition to full-file replacement.
- Add optional static analysis inputs.
- Add embeddings or vector retrieval for larger solutions.
- Add provider abstraction for additional model endpoints.
- Add MCP or work-item integrations after the core safety model remains stable.
