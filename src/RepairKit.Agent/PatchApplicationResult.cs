namespace RepairKit.Agent;

public sealed record PatchApplicationResult(
    string RunId,
    DateTime AppliedUtc,
    bool Approved,
    bool Applied,
    IReadOnlyList<string> ChangedFiles,
    IReadOnlyList<string> BackupFiles,
    string? SkippedReason,
    int? ValidationBuildExitCode,
    int? ValidationTestExitCode,
    bool ValidationBuildPassed,
    bool ValidationTestsPassed,
    bool ValidationOverallPassed,
    string ValidationBuildOutputFile,
    string ValidationTestOutputFile);

