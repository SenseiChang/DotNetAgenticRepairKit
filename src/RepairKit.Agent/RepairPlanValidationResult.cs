namespace RepairKit.Agent;

public sealed record RepairPlanValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors)
{
    public static RepairPlanValidationResult Success { get; } = new(true, []);

    public static RepairPlanValidationResult Failure(IEnumerable<string> errors)
    {
        return new RepairPlanValidationResult(false, errors.ToArray());
    }
}

