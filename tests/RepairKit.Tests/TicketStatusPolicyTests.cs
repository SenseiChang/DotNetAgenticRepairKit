using RepairKit.Core.Models;
using RepairKit.Core.Services;

namespace RepairKit.Tests;

public sealed class TicketStatusPolicyTests
{
    private readonly TicketStatusPolicy _policy = new();

    [Theory]
    [InlineData(TicketStatus.New, TicketStatus.Triaged)]
    [InlineData(TicketStatus.Triaged, TicketStatus.InProgress)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Resolved)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Closed)]
    public void ValidStatusTransitionsReturnTrue(TicketStatus currentStatus, TicketStatus nextStatus)
    {
        Assert.True(_policy.CanTransition(currentStatus, nextStatus));
    }

    [Theory]
    [InlineData(TicketStatus.New, TicketStatus.InProgress)]
    [InlineData(TicketStatus.Triaged, TicketStatus.Resolved)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Closed)]
    [InlineData(TicketStatus.Resolved, TicketStatus.New)]
    public void InvalidStatusTransitionsReturnFalse(TicketStatus currentStatus, TicketStatus nextStatus)
    {
        Assert.False(_policy.CanTransition(currentStatus, nextStatus));
    }

    [Theory]
    [InlineData(TicketStatus.New)]
    [InlineData(TicketStatus.Triaged)]
    [InlineData(TicketStatus.InProgress)]
    [InlineData(TicketStatus.Resolved)]
    public void ClosedTicketsCannotMoveToAnyOtherStatus(TicketStatus nextStatus)
    {
        Assert.False(_policy.CanTransition(TicketStatus.Closed, nextStatus));
    }

    [Theory]
    [InlineData(TicketStatus.New)]
    [InlineData(TicketStatus.Triaged)]
    [InlineData(TicketStatus.InProgress)]
    [InlineData(TicketStatus.Resolved)]
    [InlineData(TicketStatus.Closed)]
    public void SameStatusTransitionsReturnTrue(TicketStatus status)
    {
        Assert.True(_policy.CanTransition(status, status));
    }
}

