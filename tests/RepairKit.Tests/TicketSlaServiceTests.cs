using RepairKit.Core.Models;
using RepairKit.Core.Services;

namespace RepairKit.Tests;

public sealed class TicketSlaServiceTests
{
    private static readonly DateTime CreatedUtc = new(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc);

    private readonly TicketSlaService _service = new();

    [Theory]
    [InlineData(Severity.Critical, 2)]
    [InlineData(Severity.High, 8)]
    [InlineData(Severity.Medium, 24)]
    [InlineData(Severity.Low, 72)]
    public void TicketsAreDueExpectedHoursAfterCreatedUtc(Severity severity, int expectedHours)
    {
        var ticket = CreateTicket(severity);

        var dueUtc = _service.CalculateDueUtc(ticket);

        Assert.Equal(CreatedUtc.AddHours(expectedHours), dueUtc);
        Assert.Equal(DateTimeKind.Utc, dueUtc.Kind);
    }

    private static Ticket CreateTicket(Severity severity)
    {
        return new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "Sample ticket",
            Description = "Sample ticket description.",
            CustomerTier = CustomerTier.Standard,
            Severity = severity,
            CreatedUtc = CreatedUtc,
            Status = TicketStatus.New,
            AssignedTeam = AssignedTeam.Support,
            IsEscalated = false
        };
    }
}

