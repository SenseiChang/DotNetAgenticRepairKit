namespace RepairKit.Agent;

public interface IAiRepairPlanner
{
    Task<RepairPlanResult> CreateRepairPlanAsync(
        string contextPacket,
        string runFolder,
        CancellationToken cancellationToken);
}

