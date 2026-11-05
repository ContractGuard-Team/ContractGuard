namespace ContractGuard.Domain.Analysis;

// Where a clause sits in the original file (ATTR-DATA-035).
public sealed record ClauseLocation(int PageStart, int PageEnd, List<PageRegion> Regions);
