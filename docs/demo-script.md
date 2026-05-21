# 5-Minute Demo Script

This script uses CMD commands and assumes you are starting from the repository root.

## 1. Start From A Clean Repo

```cmd
scripts\restore-clean-services.cmd
dotnet test
```

Expected: all tests pass.

## 2. Load Local OpenRouter Settings

Create `set-agent-env.local.cmd` from `set-agent-env.example.cmd`, then run:

```cmd
call set-agent-env.local.cmd
```

Do not commit `set-agent-env.local.cmd`.

## 3. Introduce A Controlled Bug

```cmd
scripts\introduce-critical-sla-bug.cmd
```

This changes `TicketSlaService.cs` so critical tickets are due in 24 hours instead of 2 hours.

## 4. Run The Agent

```cmd
dotnet run --project src\RepairKit.Agent
```

Expected:

- Build passes.
- Tests fail.
- The agent writes a context packet.
- OpenRouter returns a repair plan.
- The agent prints the proposed repair and asks for approval.

## 5. Approve The Repair

Type:

```cmd
APPLY
```

Expected:

- The agent writes `approval-decision.json`.
- The agent creates backups.
- The agent applies the full-file replacement.
- Validation build and tests pass.
- The agent writes a Git diff and repair report.

## 6. Show The Repair Report

Use the run ID printed by the agent:

```cmd
type .agent\runs\<runId>\repair-report.md
```

Point out:

- Initial failure.
- AI repair plan summary.
- Approval decision.
- Patch application result.
- Validation result.
- Git diff path.

## 7. Open The Agent Dashboard

```cmd
dotnet run --project src\RepairKit.Web
```

Browse to:

```text
/agent-dashboard
```

Select the latest run and show the read-only artifacts.

## 8. Restore The Clean Demo State

```cmd
scripts\restore-clean-services.cmd
dotnet test
```

Expected: all tests pass.
