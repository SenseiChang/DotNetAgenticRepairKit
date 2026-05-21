# Architecture

DotNetAgenticRepairKit is split into a small sample application and a deterministic remediation agent. The sample application gives the agent realistic code to repair. The agent records every major step as local artifacts so a human can inspect what happened.

## High-Level Architecture

```text
                         +----------------------+
                         |  RepairKit.Web       |
                         |  Blazor UI           |
                         |  - Tickets           |
                         |  - Agent Dashboard   |
                         +----------+-----------+
                                    |
                                    | read-only local files
                                    v
+----------------------+     +------+----------------+
| RepairKit.Core       |     | .agent/               |
| Ticket domain        |     | runs + history        |
| Services + tests     |     | reports + artifacts   |
+----------+-----------+     +------+----------------+
           ^                        ^
           | references             | writes
           |                        |
+----------+-----------+     +------+----------------+
| RepairKit.Infrastructure |  | RepairKit.Agent       |
| Dashboard file reading   |  | Console workflow      |
| Shared infrastructure    |  | build/test/AI/patch   |
+--------------------------+  +-----------------------+
```

## Agent Pipeline

```text
1. Load repairkit.config.json
2. Resolve repo root, solution path, and .agent output path
3. Run build command
4. Run test command if build passes
5. If failure occurs, build deterministic context packet
6. Add lightweight related-run memory from .agent/history.jsonl
7. Send context to OpenRouter for a strict JSON repair plan
8. Validate the repair plan
9. Require human approval unless a safe auto-approval mode applies
10. Validate patch paths and create backups
11. Apply full-file replacements
12. Run validation build and tests
13. Capture git diff
14. Write repair report
15. Append compact run history
```

## Data And Artifact Flow

```text
build-output.txt
test-output.txt
run-summary.json
        |
        v
context-packet.md
context-metadata.json
        |
        v
model-request.json       model-response.raw.txt
        \                 /
         v               v
          repair-plan.json
                 |
                 v
        approval-decision.json
                 |
                 v
        patch-application.json
                 |
                 v
validation-build-output.txt
validation-test-output.txt
git-diff.patch
repair-report.md
history.jsonl
```

## Safety Boundaries

The agent is intentionally conservative:

- It only edits files after a valid repair plan and approval decision exist.
- It rejects absolute paths and `..` traversal.
- It only allows configured source/test paths.
- It blocks generated folders, agent output, scripts, docs, and secret/config terms.
- It creates backups before writing replacements.
- It validates the patch with build and tests.
- It keeps `.agent/history.jsonl` compact and avoids full prompts, source code, model responses, diffs, and secrets.
- The Blazor Agent Dashboard is read-only and only reads known artifact file names.

## Where OpenRouter Fits

OpenRouter is used only for repair planning. The agent sends a deterministic context packet and asks for strict JSON matching the repair-plan schema. The model response is stored as raw text, parsed, validated, and saved as `repair-plan.json`.

OpenRouter does not apply patches. Patch application is a separate local step guarded by approval, path validation, backups, and post-patch validation.

## Future Extension Points

```text
Static analysis ----+
                    |
RAG / repo index ---+--> Context builder --> AI repair planner
                    |
MCP work items -----+

CI runner -----------> Agent command execution
Docker sandbox ------> Isolated validation environment
Dashboard API -------> Remote or team-oriented review workflow
```

Future RAG, MCP, CI, and Docker work can fit around the current pipeline without weakening the approval and patch safety boundaries.
