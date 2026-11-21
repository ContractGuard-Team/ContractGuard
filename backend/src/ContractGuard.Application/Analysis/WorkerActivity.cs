namespace ContractGuard.Application.Analysis;

// Finer progress of a running analysis, read from the AI worker's own job table (docs/04 C8): percent 0..100 and the
// worker's internal stage code (e.g. "agent_2_risk_scoring.tool.web_search"), which the web app turns into words.
public sealed record WorkerActivity(int Percent, string Stage);
