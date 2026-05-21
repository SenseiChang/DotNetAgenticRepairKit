namespace RepairKit.Agent;

public static class DefaultAgentTools
{
    public static AgentToolRegistry Create(
        IRepoIndexer repoIndexer,
        IRepoIndexStore repoIndexStore,
        IContextRetriever contextRetriever)
    {
        var registry = new AgentToolRegistry();
        registry.Register(new BuildSolutionTool());
        registry.Register(new RunTestsTool());
        registry.Register(new BuildRepoIndexTool(repoIndexer));
        registry.Register(new BuildContextPacketTool(new ContextBuilder(repoIndexer, repoIndexStore, contextRetriever)));
        registry.Register(new CaptureGitDiffTool());
        registry.Register(new WriteRepairReportTool());
        registry.Register(new ReadArtifactTool());
        return registry;
    }
}
