namespace ContractGuard.Domain.Analysis;

// F-SEC-001: text in the file that looked like instructions to the AI.
public sealed record InjectionWarning(int Page, PageRegion? Region, string Excerpt);
