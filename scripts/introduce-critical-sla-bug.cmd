@echo off
setlocal

if not exist "DotNetAgenticRepairKit.sln" (
    echo Run this script from the repository root.
    exit /b 1
)

copy /Y "scripts\scenarios\buggy\TicketSlaService.CriticalSlaBug.cs" "src\RepairKit.Core\Services\TicketSlaService.cs" >nul
if errorlevel 1 (
    echo Failed to introduce Critical SLA Regression.
    exit /b 1
)

echo Introduced Critical SLA Regression in src\RepairKit.Core\Services\TicketSlaService.cs.
echo Critical tickets are now incorrectly due after 24 hours instead of 2 hours.
echo Run dotnet test to observe the expected failing tests.

endlocal

