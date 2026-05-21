# Agent Workflow

This document will outline the intended remediation workflow for the console agent.

## Draft Workflow

1. Inspect the target repository and collect project metadata.
2. Identify failing tests, diagnostics, or static analysis findings.
3. Build a repair plan using core workflow contracts.
4. Apply a scoped code change through infrastructure adapters.
5. Run validation commands.
6. Capture results, logs, and patch metadata.
7. Present findings in the console and, later, the web UI.

## Planned Topics

- Supported remediation inputs.
- Safety checks before edits.
- Test and build validation strategy.
- Run output layout.
- Human review checkpoints.
- Provider-neutral AI integration boundaries.

