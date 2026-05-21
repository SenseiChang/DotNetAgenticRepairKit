using System.Diagnostics;

namespace RepairKit.Agent;

public sealed class CommandRunner
{
    public async Task<CommandResult> RunAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken = default)
    {
        var startedUtc = DateTime.UtcNow;

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        var endedUtc = DateTime.UtcNow;

        return new CommandResult(
            $"{fileName} {arguments}",
            workingDirectory,
            stdout,
            stderr,
            process.ExitCode,
            startedUtc,
            endedUtc);
    }
}

