using System.Globalization;

namespace RepairKit.Agent;

public static class RunIdGenerator
{
    public const string Format = "yyyyMMdd-HHmmss";

    public static string Create()
    {
        return Create(DateTime.UtcNow);
    }

    public static string Create(DateTime utcNow)
    {
        return utcNow.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture);
    }

    public static bool IsValid(string runId)
    {
        return DateTime.TryParseExact(
            runId,
            Format,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
    }
}

