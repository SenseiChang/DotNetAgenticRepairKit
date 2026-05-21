namespace RepairKit.Agent;

public sealed record GitDiffCaptureResult(
    bool Succeeded,
    string? DiffFile,
    string? ErrorFile,
    string? ErrorMessage);

