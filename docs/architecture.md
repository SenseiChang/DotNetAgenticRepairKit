# Architecture

This document will describe the major components of DotNetAgenticRepairKit and how they collaborate.

## Initial Shape

- `RepairKit.Core` owns domain models, interfaces, workflow contracts, and remediation result types.
- `RepairKit.Infrastructure` implements adapters for files, repositories, command execution, persistence, and future AI provider integrations.
- `RepairKit.Agent` hosts command-line repair workflows and orchestration.
- `RepairKit.Web` presents repair sessions, findings, proposed patches, and run history.
- Test projects validate core behavior and agent orchestration boundaries.

## Planned Topics

- Domain model boundaries.
- Agent orchestration flow.
- Repository and workspace safety model.
- Patch generation and validation.
- Test execution and reporting.
- Secret handling and provider configuration.

