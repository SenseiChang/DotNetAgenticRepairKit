namespace RepairKit.Agent;

public sealed record CommandResult(
    string Command,
    string WorkingDirectory,
    string StandardOutput,
    string StandardError,
    int ExitCode,
    DateTime StartedUtc,
    DateTime EndedUtc)
{
    public long DurationMs => (long)(EndedUtc - StartedUtc).TotalMilliseconds;
}

