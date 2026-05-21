namespace RepairKit.Agent;

public static class CommandTemplate
{
    public static string Expand(
        string template,
        RepairKitConfig config,
        string runFolder,
        string buildOutputPath)
    {
        return template
            .Replace("{solutionPath}", config.ResolvedSolutionPath, StringComparison.OrdinalIgnoreCase)
            .Replace("{repoRoot}", config.ResolvedRepoRoot, StringComparison.OrdinalIgnoreCase)
            .Replace("{runFolder}", runFolder, StringComparison.OrdinalIgnoreCase)
            .Replace("{buildOutputPath}", buildOutputPath, StringComparison.OrdinalIgnoreCase);
    }

    public static (string FileName, string Arguments) Split(string command)
    {
        command = command.Trim();
        if (command.Length == 0)
        {
            throw new InvalidOperationException("Command template expanded to an empty command.");
        }

        if (command[0] == '"')
        {
            var closingQuote = command.IndexOf('"', 1);
            if (closingQuote < 0)
            {
                throw new InvalidOperationException("Quoted command is missing a closing quote.");
            }

            return (command[1..closingQuote], command[(closingQuote + 1)..].TrimStart());
        }

        var firstSpace = command.IndexOf(' ');
        return firstSpace < 0
            ? (command, string.Empty)
            : (command[..firstSpace], command[(firstSpace + 1)..]);
    }
}

