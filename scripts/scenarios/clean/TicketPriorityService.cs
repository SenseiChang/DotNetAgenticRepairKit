using RepairKit.Core.Models;

namespace RepairKit.Core.Services;

public sealed class TicketPriorityService
{
    public int CalculatePriority(Ticket ticket)
    {
        var priority = ticket.Severity switch
        {
            Severity.Low => 10,
            Severity.Medium => 25,
            Severity.High => 50,
            Severity.Critical => 100,
            _ => 0
        };

        priority += ticket.CustomerTier switch
        {
            CustomerTier.Standard => 0,
            CustomerTier.Premium => 15,
            CustomerTier.Enterprise => 30,
            _ => 0
        };

        if (ticket.IsEscalated)
        {
            priority += 40;
        }

        if (ticket.AssignedTeam == AssignedTeam.Security)
        {
            priority += 20;
        }

        return priority;
    }
}

