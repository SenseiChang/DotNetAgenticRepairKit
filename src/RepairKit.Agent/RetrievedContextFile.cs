namespace RepairKit.Agent;

public sealed record RetrievedContextFile(
    string FilePath,
    int Score,
    IReadOnlyList<string> Reasons);
