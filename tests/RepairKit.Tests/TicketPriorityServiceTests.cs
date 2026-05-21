using RepairKit.Core.Models;
using RepairKit.Core.Services;

namespace RepairKit.Tests;

public sealed class TicketPriorityServiceTests
{
    private readonly TicketPriorityService _service = new();

    [Fact]
    public void CriticalSeverityProducesHighestBasePriority()
    {
        var low = CreateTicket(severity: Severity.Low);
        var medium = CreateTicket(severity: Severity.Medium);
        var high = CreateTicket(severity: Severity.High);
        var critical = CreateTicket(severity: Severity.Critical);

        Assert.True(_service.CalculatePriority(critical) > _service.CalculatePriority(high));
        Assert.True(_service.CalculatePriority(high) > _service.CalculatePriority(medium));
        Assert.True(_service.CalculatePriority(medium) > _service.CalculatePriority(low));
        Assert.Equal(100, _service.CalculatePriority(critical));
    }

    [Fact]
    public void EnterpriseTierIncreasesPriority()
    {
        var standard = CreateTicket(customerTier: CustomerTier.Standard);
        var enterprise = CreateTicket(customerTier: CustomerTier.Enterprise);

        Assert.Equal(
            _service.CalculatePriority(standard) + 30,
            _service.CalculatePriority(enterprise));
    }

    [Fact]
    public void EscalatedTicketsIncreasePriority()
    {
        var normal = CreateTicket(isEscalated: false);
        var escalated = CreateTicket(isEscalated: true);

        Assert.Equal(
            _service.CalculatePriority(normal) + 40,
            _service.CalculatePriority(escalated));
    }

    [Fact]
    public void SecurityTicketsIncreasePriority()
    {
        var support = CreateTicket(assignedTeam: AssignedTeam.Support);
        var security = CreateTicket(assignedTeam: AssignedTeam.Security);

        Assert.Equal(
            _service.CalculatePriority(support) + 20,
            _service.CalculatePriority(security));
    }

    private static Ticket CreateTicket(
        CustomerTier customerTier = CustomerTier.Standard,
        Severity severity = Severity.Low,
        AssignedTeam assignedTeam = AssignedTeam.Support,
        bool isEscalated = false)
    {
        return new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "Sample ticket",
            Description = "Sample ticket description.",
            CustomerTier = customerTier,
            Severity = severity,
            CreatedUtc = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc),
            Status = TicketStatus.New,
            AssignedTeam = assignedTeam,
            IsEscalated = isEscalated
        };
    }
}

