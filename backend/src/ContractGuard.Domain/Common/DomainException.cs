namespace ContractGuard.Domain.Common;

// A business rule was violated. ErrorCode is an ERR-* code of SRS §14.1,
// which the API turns into a Problem Details response.
public sealed class DomainException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
