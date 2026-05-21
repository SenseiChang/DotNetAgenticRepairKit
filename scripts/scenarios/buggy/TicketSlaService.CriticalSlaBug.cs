using RepairKit.Core.Models;

namespace RepairKit.Core.Services;

public sealed class TicketSlaService
{
    public DateTime CalculateDueUtc(Ticket ticket)
    {
        var createdUtc = ticket.CreatedUtc.Kind == DateTimeKind.Utc
            ? ticket.CreatedUtc
            : DateTime.SpecifyKind(ticket.CreatedUtc, DateTimeKind.Utc);

        return ticket.Severity switch
        {
            Severity.Critical => createdUtc.AddHours(24),
            Severity.High => createdUtc.AddHours(8),
            Severity.Medium => createdUtc.AddHours(24),
            Severity.Low => createdUtc.AddHours(72),
            _ => createdUtc
        };
    }
}

