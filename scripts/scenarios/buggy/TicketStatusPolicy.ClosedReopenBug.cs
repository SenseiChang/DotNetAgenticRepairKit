using RepairKit.Core.Models;

namespace RepairKit.Core.Services;

public sealed class TicketStatusPolicy
{
    public bool CanTransition(TicketStatus currentStatus, TicketStatus nextStatus)
    {
        if (currentStatus == nextStatus)
        {
            return true;
        }

        return currentStatus switch
        {
            TicketStatus.New => nextStatus == TicketStatus.Triaged,
            TicketStatus.Triaged => nextStatus == TicketStatus.InProgress,
            TicketStatus.InProgress => nextStatus == TicketStatus.Resolved,
            TicketStatus.Resolved => nextStatus == TicketStatus.Closed,
            TicketStatus.Closed => nextStatus is TicketStatus.InProgress or TicketStatus.Triaged,
            _ => false
        };
    }
}

