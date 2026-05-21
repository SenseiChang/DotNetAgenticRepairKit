# Controlled Repair Scenarios

These scenarios intentionally introduce small, focused regressions for future agentic AI remediation demos.

The main branch should remain green by default. Run a scenario script only when you want to create a controlled failure, then run `scripts\restore-clean-services.cmd` to return the service files to their known-good versions.

## Scenario 1: Critical SLA Regression

- Bug: Critical tickets are incorrectly due after 24 hours instead of 2 hours.
- Expected failing tests: Critical SLA tests.
- Target file: `src/RepairKit.Core/Services/TicketSlaService.cs`
- Correct behavior: Critical tickets are due 2 hours after `CreatedUtc`.

Trigger:

```cmd
scripts\introduce-critical-sla-bug.cmd
dotnet test
```

## Scenario 2: Closed Ticket Reopen Regression

- Bug: Closed tickets are incorrectly allowed to transition back to `InProgress` or `Triaged`.
- Expected failing tests: Closed-ticket transition tests.
- Target file: `src/RepairKit.Core/Services/TicketStatusPolicy.cs`
- Correct behavior: Closed tickets cannot move to any other status, except same-status `Closed` to `Closed`.

Trigger:

```cmd
scripts\introduce-closed-ticket-reopen-bug.cmd
dotnet test
```

## Scenario 3: Enterprise Escalation Priority Regression

- Bug: Enterprise escalated tickets do not receive the proper combined priority boost.
- Expected failing tests: Enterprise and escalated priority tests.
- Target file: `src/RepairKit.Core/Services/TicketPriorityService.cs`
- Correct behavior: Enterprise adds 30 and escalated adds 40.

Trigger:

```cmd
scripts\introduce-enterprise-escalation-priority-bug.cmd
dotnet test
```

## Restore

Restore the known-good service files:

```cmd
scripts\restore-clean-services.cmd
dotnet test
```

## Safety Notes

- These scripts are intentionally destructive to local working files.
- Commit or stash work before running a scenario.
- The restore script returns the service files to the known-good versions checked into `scripts\scenarios\clean`.

