namespace RepairKit.Core.Models;

public sealed class Ticket
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public CustomerTier CustomerTier { get; init; }

    public Severity Severity { get; init; }

    public DateTime CreatedUtc { get; init; }

    public TicketStatus Status { get; init; }

    public AssignedTeam AssignedTeam { get; init; }

    public bool IsEscalated { get; init; }
}

