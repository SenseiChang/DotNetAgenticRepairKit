namespace RepairKit.Agent;

public static class RepairPlanValidator
{
    private static readonly string[] AllowedRiskLevels = ["low", "medium", "high"];

    private static readonly string[] BlockedPathSegments =
    [
        ".git",
        ".agent",
        "bin",
        "obj",
        ".vs",
        "node_modules",
        ".env",
        "secret",
        "token",
        "password",
        "key",
        "appsettings"
    ];

    public static RepairPlanValidationResult Validate(RepairPlan? plan)
    {
        var errors = new List<string>();

        if (plan is null)
        {
            return RepairPlanValidationResult.Failure(["Repair plan is missing."]);
        }

        if (string.IsNullOrWhiteSpace(plan.Summary))
        {
            errors.Add("summary is required.");
        }

        if (!AllowedRiskLevels.Contains(plan.RiskLevel, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add("riskLevel must be low, medium, or high.");
        }

        if (plan.TargetFiles.Count == 0)
        {
            errors.Add("targetFiles must not be empty.");
        }

        if (plan.Changes.Count == 0)
        {
            errors.Add("changes must not be empty.");
        }

        foreach (var targetFile in plan.TargetFiles)
        {
            ValidatePath(targetFile, $"targetFiles path '{targetFile}'", errors);
        }

        var targetFiles = plan.TargetFiles.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var change in plan.Changes)
        {
            ValidatePath(change.FilePath, $"change.filePath '{change.FilePath}'", errors);

            if (!targetFiles.Contains(change.FilePath))
            {
                errors.Add($"change.filePath '{change.FilePath}' must be listed in targetFiles.");
            }

            if (string.IsNullOrWhiteSpace(change.FullReplacement))
            {
                errors.Add($"change.fullReplacement for '{change.FilePath}' must be non-empty.");
            }
        }

        if (!plan.ValidationCommands.Any(IsDotnetTestCommand))
        {
            errors.Add("validationCommands should include dotnet test or equivalent.");
        }

        return errors.Count == 0
            ? RepairPlanValidationResult.Success
            : RepairPlanValidationResult.Failure(errors);
    }

    private static bool IsDotnetTestCommand(string command)
    {
        return command.Contains("dotnet test", StringComparison.OrdinalIgnoreCase) ||
               command.Contains("dotnet", StringComparison.OrdinalIgnoreCase) &&
               command.Contains("test", StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidatePath(string path, string label, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add($"{label} must be non-empty.");
            return;
        }

        var normalizedPath = path.Replace('\\', '/');

        if (Path.IsPathRooted(path) || normalizedPath.StartsWith("/", StringComparison.Ordinal))
        {
            errors.Add($"{label} must not be absolute.");
        }

        if (normalizedPath.Split('/').Any(segment => segment == ".."))
        {
            errors.Add($"{label} must not traverse upward using '..'.");
        }

        foreach (var blocked in BlockedPathSegments)
        {
            if (normalizedPath.Contains(blocked, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"{label} contains blocked path segment or keyword '{blocked}'.");
            }
        }

        if (!normalizedPath.StartsWith("src/", StringComparison.OrdinalIgnoreCase) &&
            !normalizedPath.StartsWith("tests/", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"{label} should be under src/ or tests/.");
        }
    }
}

