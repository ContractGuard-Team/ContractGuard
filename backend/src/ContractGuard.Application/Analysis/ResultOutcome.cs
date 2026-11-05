namespace ContractGuard.Application.Analysis;

// What AnalysisResultHandler did with one analysis-results message. In all three cases the
// consumer deletes the message: handling it again would not change anything.
public enum ResultOutcome
{
    // State changed and saved.
    Applied,

    // Nothing to do: duplicate, late or unknown job. The message is deleted.
    Ignored,

    // The worker's result was invalid; the run is marked failed. The message is deleted.
    Rejected
}
