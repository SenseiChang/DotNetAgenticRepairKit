namespace RepairKit.Agent;

public sealed record AgentToolContext(
    RepairKitConfig Config,
    string RepoRoot,
    string RunId,
    string RunFolder,
    ICommandRunner CommandRunner,
    IUserOutput? UserOutput = null);
