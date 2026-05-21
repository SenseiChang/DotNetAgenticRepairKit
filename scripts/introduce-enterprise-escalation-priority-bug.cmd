@echo off
setlocal

if not exist "DotNetAgenticRepairKit.sln" (
    echo Run this script from the repository root.
    exit /b 1
)

copy /Y "scripts\scenarios\buggy\TicketPriorityService.EnterpriseEscalationBug.cs" "src\RepairKit.Core\Services\TicketPriorityService.cs" >nul
if errorlevel 1 (
    echo Failed to introduce Enterprise Escalation Priority Regression.
    exit /b 1
)

copy /B "src\RepairKit.Core\Services\TicketPriorityService.cs"+,, "src\RepairKit.Core\Services\TicketPriorityService.cs" >nul

echo Introduced Enterprise Escalation Priority Regression in src\RepairKit.Core\Services\TicketPriorityService.cs.
echo Enterprise escalated tickets now miss the proper combined priority boost.
echo Run dotnet test to observe the expected failing tests.

endlocal
