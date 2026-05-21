# Portfolio Notes

## Interview Summary

DotNetAgenticRepairKit is a custom .NET agentic AI remediation skeleton. It demonstrates a controlled software repair loop for a Blazor and xUnit solution: detect a failing test, collect focused context, ask an AI model for a structured plan, require approval, apply a safe patch, validate with build and tests, and present the results in a read-only dashboard.

## Problem It Solves

Many AI coding demos skip the operational parts that matter in real engineering environments: deterministic reproduction, constrained context, approval, path safety, validation, reporting, and audit history. This project focuses on those parts.

The sample Support Ticket Triage domain is intentionally small, but the workflow is shaped like a production remediation system.

## Why A Custom Agentic Workflow

The agent is written directly in .NET instead of using an agent framework so the safety and orchestration boundaries are explicit:

- Command execution is deterministic and testable.
- OpenRouter is called through direct `HttpClient` usage.
- Repair plans must match a strict JSON schema.
- Patch application is separate from planning.
- Human approval is first-class.
- Path validation and backups are local, inspectable code.

This makes the repository easier to explain, test, and adapt.

## How Safety Is Enforced

Safety is enforced through multiple gates:

- No AI call occurs when build and tests already pass.
- Context generation is deterministic and focused on known failure keywords.
- Repair plans are validated before use.
- Approval is required before source changes.
- Patch paths must be relative, allowed, and free of blocked terms.
- Backups are created before writes.
- Build and test validation runs after patching.
- Reports, diffs, and history provide an audit trail.

## External .NET Solution Integration

`repairkit.config.json` lets the agent target a different .NET repository by changing:

- solution path
- repo root
- agent output folder
- build and test commands
- Git diff command
- allowed edit paths
- blocked path rules
- context and history limits

This keeps the demo useful as a reusable skeleton rather than a one-off application.

## Production Enhancements

Logical next steps for a production version:

- CI integration with controlled approval modes.
- Isolated validation in containers or ephemeral worktrees.
- Richer static analysis and test failure parsing.
- Repository indexing or RAG for larger codebases.
- Pull request creation after successful validation.
- Team review workflow in the dashboard.
- Provider abstraction for multiple model endpoints.
- Stronger patch rollback and merge-conflict handling.
