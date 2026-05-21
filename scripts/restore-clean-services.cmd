@echo off
setlocal

if not exist "DotNetAgenticRepairKit.sln" (
    echo Run this script from the repository root.
    exit /b 1
)

copy /Y "scripts\scenarios\clean\TicketSlaService.cs" "src\RepairKit.Core\Services\TicketSlaService.cs" >nul
if errorlevel 1 (
    echo Failed to restore TicketSlaService.cs.
    exit /b 1
)
copy /B "src\RepairKit.Core\Services\TicketSlaService.cs"+,, "src\RepairKit.Core\Services\TicketSlaService.cs" >nul

copy /Y "scripts\scenarios\clean\TicketStatusPolicy.cs" "src\RepairKit.Core\Services\TicketStatusPolicy.cs" >nul
if errorlevel 1 (
    echo Failed to restore TicketStatusPolicy.cs.
    exit /b 1
)
copy /B "src\RepairKit.Core\Services\TicketStatusPolicy.cs"+,, "src\RepairKit.Core\Services\TicketStatusPolicy.cs" >nul

copy /Y "scripts\scenarios\clean\TicketPriorityService.cs" "src\RepairKit.Core\Services\TicketPriorityService.cs" >nul
if errorlevel 1 (
    echo Failed to restore TicketPriorityService.cs.
    exit /b 1
)
copy /B "src\RepairKit.Core\Services\TicketPriorityService.cs"+,, "src\RepairKit.Core\Services\TicketPriorityService.cs" >nul

echo Restored known-good service files in src\RepairKit.Core\Services.
echo Run dotnet test to verify the repository is passing.

endlocal
