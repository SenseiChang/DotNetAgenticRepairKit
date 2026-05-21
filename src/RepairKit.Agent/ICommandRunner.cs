namespace RepairKit.Agent;

public interface ICommandRunner
{
    Task<CommandResult> RunAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken = default);
}

