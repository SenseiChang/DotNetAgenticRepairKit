using System.Text;

namespace RepairKit.Agent;

public static class TestOutputFormatter
{
    public static string Format(CommandResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Command: {result.Command}");
        builder.AppendLine($"Working Directory: {result.WorkingDirectory}");
        builder.AppendLine($"Exit Code: {result.ExitCode}");
        builder.AppendLine($"Started UTC: {result.StartedUtc:O}");
        builder.AppendLine($"Ended UTC: {result.EndedUtc:O}");
        builder.AppendLine($"Duration MS: {result.DurationMs}");
        builder.AppendLine();
        builder.AppendLine("STDOUT:");
        builder.AppendLine(result.StandardOutput);
        builder.AppendLine();
        builder.AppendLine("STDERR:");
        builder.AppendLine(result.StandardError);

        return builder.ToString();
    }
}

