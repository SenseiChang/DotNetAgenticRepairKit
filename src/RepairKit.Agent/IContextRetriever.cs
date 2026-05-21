namespace RepairKit.Agent;

public interface IContextRetriever
{
    Task<ContextRetrievalResult> RetrieveAsync(
        ContextRetrievalRequest request,
        CancellationToken cancellationToken = default);
}
