namespace ContractGuard.Application.Analysis;

// Port: hands a job to the analysis worker. Implemented by SqsAnalysisJobQueue (Infrastructure).
public interface IAnalysisJobQueue
{
    Task PublishAsync(AnalysisRequestMessage message, CancellationToken cancellationToken);
}
