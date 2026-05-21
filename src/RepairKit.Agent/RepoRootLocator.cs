namespace RepairKit.Agent;

public static class RepoRootLocator
{
    public const string SolutionFileName = "DotNetAgenticRepairKit.sln";

    public static string FindRepoRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);

        while (directory is not null)
        {
            var solutionPath = Path.Combine(directory.FullName, SolutionFileName);
            if (File.Exists(solutionPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate {SolutionFileName}. Start directory: {startDirectory}");
    }
}

