# Docker

Docker support is optional. It provides a predictable SDK-based environment for running `RepairKit.Agent`, but it does not replace normal local CMD usage.

The image does not contain secrets. Pass API keys at runtime with environment variables.

## Build The Image

```cmd
docker build -t dotnet-agentic-repair-kit .
```

## Run Without AI

```cmd
docker run --rm dotnet-agentic-repair-kit --no-ai
```

This runs the deterministic build/test path without requiring `OPENROUTER_API_KEY`.

## Run With OpenRouter Planning

```cmd
docker run --rm ^
  -e OPENROUTER_API_KEY=%OPENROUTER_API_KEY% ^
  -e REPAIRKIT_MODEL=openai/gpt-5.2 ^
  dotnet-agentic-repair-kit --plan-only
```

`--plan-only` can create an AI repair plan without prompting for approval or applying patches.

## Mounted Repo Mode

Use mounted repo mode when patch application should modify the host working tree:

```cmd
docker run --rm -it ^
  -v "%cd%:/workspace" ^
  -w /workspace ^
  -e OPENROUTER_API_KEY=%OPENROUTER_API_KEY% ^
  -e REPAIRKIT_MODEL=openai/gpt-5.2 ^
  dotnet-agentic-repair-kit
```

In this mode:

- The container runs from the mounted repository at `/workspace`.
- `repairkit.config.json` works with its default relative paths.
- `.agent` run artifacts are written to the host working tree.
- Approved patch application modifies host files.

## Configuration

The containerized agent respects the same options as local execution:

```cmd
docker run --rm dotnet-agentic-repair-kit --config repairkit.config.json
docker run --rm dotnet-agentic-repair-kit --solution H:\Projects\SomeOtherApp\SomeOtherApp.sln --repo-root H:\Projects\SomeOtherApp
docker run --rm dotnet-agentic-repair-kit --agent-output .agent
```

For external Windows paths, mounted repo mode is usually clearer because the container needs access to the same files.

## Git

The Dockerfile installs `git` because repair reporting can capture:

```cmd
git diff -- src tests
```

If the run does not reach patch application, Git diff capture may not be used, but `git` is available when needed.

## Demo Verification

```cmd
docker build -t dotnet-agentic-repair-kit .
docker run --rm dotnet-agentic-repair-kit --no-ai
```

Full repair loop with host-visible changes:

```cmd
scripts\introduce-critical-sla-bug.cmd

docker run --rm -it ^
  -v "%cd%:/workspace" ^
  -w /workspace ^
  -e OPENROUTER_API_KEY=%OPENROUTER_API_KEY% ^
  -e REPAIRKIT_MODEL=openai/gpt-5.2 ^
  dotnet-agentic-repair-kit
```

Type:

```cmd
APPLY
```

Then restore:

```cmd
scripts\restore-clean-services.cmd
dotnet test
```
