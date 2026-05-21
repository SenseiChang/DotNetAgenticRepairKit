# External Solution Integration

DotNetAgenticRepairKit can target another .NET solution by loading a `repairkit.config.json` file or by passing command-line overrides.

## Basic Steps

1. Copy `repairkit.config.json` or start from `docs/examples/repairkit.external-solution.config.example.json`.
2. Set `repoRoot` to the target repository root.
3. Set `solutionPath` to the target `.sln` file.
4. Set `agentOutputPath` to a local output folder, usually `<repo>/.agent`.
5. Narrow `allowedEditPaths` to the smallest set of source and test folders the agent may modify.
6. Keep `repoIndexPath` inside a local ignored output folder such as `.agent/repo-index.json`.

## Example

```cmd
dotnet run --project src\RepairKit.Agent --config docs\examples\repairkit.external-solution.config.example.json
```

Or override paths directly:

```cmd
dotnet run --project src\RepairKit.Agent --solution H:\Projects\SomeOtherApp\SomeOtherApp.sln --repo-root H:\Projects\SomeOtherApp
```

## Safety Recommendations

Keep `allowedEditPaths` narrow. Prefer `src/` and `tests/`, or even specific project folders, instead of allowing the entire repository.

Secrets and configuration files are blocked by default using path terms such as `.env`, `secret`, `token`, `password`, `key`, and `appsettings`. The agent should not rewrite credentials, deployment settings, local machine settings, or generated build output.

Patch application remains approval-gated. Even with a valid AI repair plan, the agent requires approval before applying changes unless a low-risk plan is explicitly auto-approved with `--approve-plan`.

## Repository Indexing

The agent can build a deterministic repository index for the target solution:

```cmd
dotnet run --project src\RepairKit.Agent --config repairkit.config.json --index
```

For external solutions, keep `allowedEditPaths` and `indexedExtensions` narrow so retrieval focuses on source and tests. The index is local, gitignored, and does not include secrets, generated files, or agent run artifacts.
