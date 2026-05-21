namespace RepairKit.Agent;

public sealed record AppliedPatch(
    string FilePath,
    string BackupFile);

