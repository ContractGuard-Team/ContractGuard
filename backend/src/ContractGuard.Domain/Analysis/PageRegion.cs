namespace ContractGuard.Domain.Analysis;

// A rectangle on one page. Coordinates are fractions of the page size (0..1), top-left origin,
// so the viewer can scale them to any zoom level.
public sealed record PageRegion(int Page, double X, double Y, double Width, double Height);
