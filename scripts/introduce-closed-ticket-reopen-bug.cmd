@echo off
setlocal

if not exist "DotNetAgenticRepairKit.sln" (
    echo Run this script from the repository root.
    exit /b 1
)

copy /Y "scripts\scenarios\buggy\TicketStatusPolicy.ClosedReopenBug.cs" "src\RepairKit.Core\Services\TicketStatusPolicy.cs" >nul
if errorlevel 1 (
    echo Failed to introduce Closed Ticket Reopen Regression.
    exit /b 1
)

copy /B "src\RepairKit.Core\Services\TicketStatusPolicy.cs"+,, "src\RepairKit.Core\Services\TicketStatusPolicy.cs" >nul

echo Introduced Closed Ticket Reopen Regression in src\RepairKit.Core\Services\TicketStatusPolicy.cs.
echo Closed tickets are now incorrectly allowed to move to InProgress or Triaged.
echo Run dotnet test to observe the expected failing tests.

endlocal
