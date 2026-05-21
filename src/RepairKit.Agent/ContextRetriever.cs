using System.Text.RegularExpressions;

namespace RepairKit.Agent;

public sealed class RepoIndexContextRetriever : IContextRetriever
{
    private readonly IRepoIndexStore _indexStore;

    private static readonly IReadOnlyList<string> SupportingModelFiles =
    [
        "src/RepairKit.Core/Models/Ticket.cs",
        "src/RepairKit.Core/Models/CustomerTier.cs",
        "src/RepairKit.Core/Models/Severity.cs",
        "src/RepairKit.Core/Models/TicketStatus.cs",
        "src/RepairKit.Core/Models/AssignedTeam.cs"
    ];

    private static readonly Dictionary<string, string> ServiceToTestFile = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TicketSlaService"] = "tests/RepairKit.Tests/TicketSlaServiceTests.cs",
        ["TicketStatusPolicy"] = "tests/RepairKit.Tests/TicketStatusPolicyTests.cs",
        ["TicketPriorityService"] = "tests/RepairKit.Tests/TicketPriorityServiceTests.cs"
    };

    public RepoIndexContextRetriever()
        : this(new JsonRepoIndexStore())
    {
    }

    public RepoIndexContextRetriever(IRepoIndexStore indexStore)
    {
        _indexStore = indexStore;
    }

    public async Task<ContextRetrievalResult> RetrieveAsync(
        ContextRetrievalRequest request,
        CancellationToken cancellationToken = default)
    {
        var index = await _indexStore.ReadAsync(request.Config, cancellationToken);
        return Retrieve(
            request.FailureText,
            request.DetectedKeywords,
            index,
            request.MaxFiles,
            request.RelatedHistoryTargetFiles);
    }

    public ContextRetrievalResult Retrieve(
        string failureText,
        IReadOnlyList<string> detectedKeywords,
        RepoIndex index,
        int maxFiles,
        IReadOnlyList<string>? relatedHistoryTargetFiles = null)
    {
        var terms = ExtractSearchTerms(failureText, detectedKeywords);
        var retrieved = new List<RetrievedContextFile>();
        var excluded = new List<string>();
        var priorityFiles = GetPriorityTicketFiles(failureText, index);

        foreach (var entry in index.Entries)
        {
            var (score, reasons) = Score(entry, failureText, terms);
            if (relatedHistoryTargetFiles?.Any(file =>
                    string.Equals(file, entry.FilePath, StringComparison.OrdinalIgnoreCase)) == true)
            {
                score += 200;
                reasons = reasons.Concat(["recent related history target"]).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            }

            if (priorityFiles.Contains(entry.FilePath))
            {
                score += 500;
                reasons = reasons.Concat(["known ticket repair target"]).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            }

            if (entry.FilePath.StartsWith("tests/RepairKit.Agent.Tests/", StringComparison.OrdinalIgnoreCase))
            {
                score -= 150;
                reasons = reasons.Concat(["agent test de-prioritized"]).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            }

            if (score > 0)
            {
                retrieved.Add(new RetrievedContextFile(entry.FilePath, score, reasons));
            }
            else
            {
                excluded.Add(entry.FilePath);
            }
        }

        var included = priorityFiles.ToList();
        var initialIncludedSet = included.ToHashSet(StringComparer.OrdinalIgnoreCase);

        included.AddRange(retrieved
            .OrderByDescending(file => file.Score)
            .ThenBy(file => file.FilePath, StringComparer.OrdinalIgnoreCase)
            .Select(file => file.FilePath)
            .Where(file => !initialIncludedSet.Contains(file))
            .Where(file => priorityFiles.Count == 0 || IsUsefulSupplementalFile(file))
            .Take(Math.Max(1, maxFiles) - included.Count));

        IncludeRelatedTicketFiles(included, index, maxFiles);

        var includedSet = included.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var finalRetrieved = retrieved
            .Where(file => includedSet.Contains(file.FilePath))
            .OrderByDescending(file => file.Score)
            .ThenBy(file => file.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var file in included)
        {
            if (finalRetrieved.All(item => !string.Equals(item.FilePath, file, StringComparison.OrdinalIgnoreCase)))
            {
                finalRetrieved.Add(new RetrievedContextFile(file, 25, ["related ticket file"]));
            }
        }

        return new ContextRetrievalResult(
            terms.Where(term => IsTicketKeyword(term)).OrderBy(term => term, StringComparer.OrdinalIgnoreCase).ToArray(),
            included,
            finalRetrieved,
            excluded);
    }

    private static (int Score, IReadOnlyList<string> Reasons) Score(
        RepoIndexEntry entry,
        string failureText,
        IReadOnlySet<string> terms)
    {
        var score = 0;
        var reasons = new List<string>();
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(entry.FileName);

        if (failureText.Contains(entry.FileName, StringComparison.OrdinalIgnoreCase) ||
            failureText.Contains(fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
        {
            score += 100;
            reasons.Add("file name match");
        }

        foreach (var type in entry.DeclaredTypes)
        {
            if (failureText.Contains(type, StringComparison.OrdinalIgnoreCase))
            {
                score += 90;
                reasons.Add($"declared type match: {type}");
            }
        }

        foreach (var ns in entry.Namespaces)
        {
            if (failureText.Contains(ns, StringComparison.OrdinalIgnoreCase))
            {
                score += 30;
                reasons.Add($"namespace match: {ns}");
            }
        }

        var keywordMatches = entry.Keywords.Count(keyword => terms.Contains(keyword));
        if (keywordMatches > 0)
        {
            score += keywordMatches * 8;
            reasons.Add($"keyword matches: {keywordMatches}");
        }

        if (entry.FilePath.StartsWith("tests/", StringComparison.OrdinalIgnoreCase) &&
            terms.Contains("tests"))
        {
            score += 15;
            reasons.Add("test path match");
        }

        if (entry.FilePath.Contains("/Services/", StringComparison.OrdinalIgnoreCase) &&
            terms.Contains("service"))
        {
            score += 10;
            reasons.Add("service path match");
        }

        return (score, reasons.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static void IncludeRelatedTicketFiles(List<string> included, RepoIndex index, int maxFiles)
    {
        var includedSet = included.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var detectedService = ServiceToTestFile.Keys.FirstOrDefault(service =>
            included.Any(file => file.Contains(service, StringComparison.OrdinalIgnoreCase)));

        if (detectedService is null)
        {
            return;
        }

        var serviceFile = $"src/RepairKit.Core/Services/{detectedService}.cs";
        AddIfIndexed(serviceFile);
        AddIfIndexed(ServiceToTestFile[detectedService]);

        foreach (var modelFile in SupportingModelFiles)
        {
            AddIfIndexed(modelFile);
        }

        while (included.Count > maxFiles && included.Count > SupportingModelFiles.Count + 2)
        {
            included.RemoveAt(included.Count - 1);
        }

        void AddIfIndexed(string file)
        {
            if (includedSet.Contains(file))
            {
                return;
            }

            if (index.Entries.Any(entry => string.Equals(entry.FilePath, file, StringComparison.OrdinalIgnoreCase)))
            {
                included.Add(file);
                includedSet.Add(file);
            }
        }
    }

    private static IReadOnlyList<string> GetPriorityTicketFiles(string failureText, RepoIndex index)
    {
        var service = ServiceToTestFile.Keys.FirstOrDefault(service =>
            failureText.Contains(service, StringComparison.OrdinalIgnoreCase) ||
            failureText.Contains(service + "Tests", StringComparison.OrdinalIgnoreCase));

        if (service is null)
        {
            return [];
        }

        var candidates = new[]
        {
            $"src/RepairKit.Core/Services/{service}.cs",
            ServiceToTestFile[service]
        }.Concat(SupportingModelFiles);

        var indexed = index.Entries
            .Select(entry => entry.FilePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return candidates
            .Where(indexed.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsUsefulSupplementalFile(string filePath)
    {
        if (filePath.StartsWith("tests/RepairKit.Agent.Tests/", StringComparison.OrdinalIgnoreCase) ||
            filePath.Equals("repairkit.config.json", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var extension = Path.GetExtension(filePath);
        return extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".razor", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlySet<string> ExtractSearchTerms(
        string failureText,
        IReadOnlyList<string> detectedKeywords)
    {
        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var keyword in detectedKeywords)
        {
            AddTermParts(terms, keyword);
        }

        foreach (Match match in Regex.Matches(failureText, @"[A-Za-z_][A-Za-z0-9_]*"))
        {
            AddTermParts(terms, match.Value);
        }

        return terms
            .Where(term => term.Length > 2)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static void AddTermParts(HashSet<string> terms, string value)
    {
        foreach (Match match in Regex.Matches(value, @"[A-Z]?[a-z]+|[A-Z]+(?![a-z])|\d+"))
        {
            terms.Add(match.Value.ToLowerInvariant());
        }

        if (value.Length > 2)
        {
            terms.Add(value.ToLowerInvariant());
        }
    }

    private static bool IsTicketKeyword(string term)
    {
        return term.Contains("ticket", StringComparison.OrdinalIgnoreCase) ||
            term.Contains("sla", StringComparison.OrdinalIgnoreCase) ||
            term.Contains("status", StringComparison.OrdinalIgnoreCase) ||
            term.Contains("priority", StringComparison.OrdinalIgnoreCase) ||
            term.Contains("service", StringComparison.OrdinalIgnoreCase) ||
            term.Contains("policy", StringComparison.OrdinalIgnoreCase);
    }
}
