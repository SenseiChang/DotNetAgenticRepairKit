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
| Services + tests     |     | repo index            |
+----------+-----------+     | reports + artifacts   |
           ^                 +------+----------------+
           | references             ^
           |                        | writes
+----------+-----------+     +------+----------------+
| RepairKit.Infrastructure |  | RepairKit.Agent       |
| Dashboard file reading   |  | Console workflow      |
| Shared infrastructure    |  | tools/build/AI/patch  |
+--------------------------+  +-----------------------+
```

## Agent Pipeline

```text
1. Load repairkit.config.json
2. Resolve repo root, solution path, .agent output path, and repo index path
3. Build repo index and exit if --index is provided
4. Rebuild repo index first if --reindex is provided
5. Run build command
6. Run test command if build passes
7. If failure occurs, load or build .agent/repo-index.json
8. Retrieve relevant files with ContextRetriever
9. Fall back to hardcoded keyword matching if retrieval is unavailable
10. Build deterministic context packet
11. Add lightweight related-run memory from .agent/history.jsonl
12. Send context to OpenRouter for a strict JSON repair plan
13. Validate the repair plan
14. Require human approval unless a safe auto-approval mode applies
15. Validate patch paths and create backups
16. Apply full-file replacements
17. Run validation build and tests
18. Capture git diff
19. Write repair report
20. Append compact run history
```

## Context And Retrieval Flow

```text
repo files
   |
   v
RepoIndexer
   |
   v
.agent/repo-index.json
   |
   v
ContextRetriever <---- build-output.txt + test-output.txt
   |
   v
ranked relevant files
   |
   v
ContextBuilder
   |
   v
context-packet.md + context-metadata.json
```

The repository index is deterministic and local. It stores file paths, file names, extensions, sizes, hashes, declared types, namespaces, keywords, snippets, and timestamps. It does not call OpenRouter, use embeddings, or require a vector database.

The current implementation uses `JsonRepoIndexStore` and `RepoIndexContextRetriever`. `RepoIndexContextRetriever` scores indexed files using exact file-name matches, declared type matches, namespace/type references, keyword matches, test file names, source file names, and related run history targets when available. If the index cannot be read or no relevant files are retrieved, `ContextBuilder` preserves the earlier hardcoded keyword fallback.

The retrieval layer is abstracted behind `IRepoIndexer`, `IRepoIndexStore`, and `IContextRetriever`:

```text
ContextBuilder
  |
  +--> IRepoIndexer
  |      current: RepoIndexer
  |
  +--> IRepoIndexStore
  |      current: JsonRepoIndexStore
  |
  +--> IContextRetriever
         current: RepoIndexContextRetriever
         future: VectorContextRetriever / HybridContextRetriever
```

Future stores such as Qdrant, Postgres/pgvector, Chroma, or Azure AI Search can be added behind these contracts without changing the agent pipeline.

## Tool Execution Layer

`RepairKit.Agent` includes an MCP-inspired internal tool abstraction:

```text
AgentProgram
   |
   v
AgentToolRegistry
   |
   +--> build_solution
   +--> run_tests
   +--> build_repo_index
   +--> build_context_packet
   +--> capture_git_diff
   +--> write_repair_report
   +--> read_artifact
```

Each tool exposes:

- a stable name
- a description
- a JSON input schema
- a structured `AgentToolResult`
- run-local audit events in `tool-events.jsonl`

The current implementation is local-only and is not a full MCP server. The abstraction creates a seam where future MCP hosting or LLM tool-calling integration can wrap the same tool registry without changing the core remediation pipeline.

## Data And Artifact Flow

```text
.agent/repo-index.json
build-output.txt
test-output.txt
run-summary.json
tool-events.jsonl
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

- It does not index secret-like or blocked files.
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

OpenRouter does not build the repository index and does not apply patches. Patch application is a separate local step guarded by approval, path validation, backups, and post-patch validation.

## Future Extension Points

```text
Static analysis ------+
                      |
Embeddings/vector ----+--> IContextRetriever --> ContextBuilder --> AI repair planner
                      |
MCP work items -------+

CI runner ------------> Agent command execution
Docker sandbox -------> Isolated validation environment
Dashboard API --------> Remote or team-oriented review workflow
```

Future embeddings, MCP, CI, and Docker work can fit around the current pipeline without weakening the approval and patch safety boundaries.
