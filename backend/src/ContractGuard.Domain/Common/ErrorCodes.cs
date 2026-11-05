namespace ContractGuard.Domain.Common;

// Error codes of SRS §14.1. Kept as constants so tests and the frontend can match on them.
public static class ErrorCodes
{
    public const string EmailTaken = "ERR-AUTH-001";
    public const string PasswordPolicy = "ERR-AUTH-002";
    public const string InvalidCredentials = "ERR-AUTH-003";
    public const string LinkExpired = "ERR-AUTH-004";
    public const string LockedOut = "ERR-AUTH-005";
    public const string TooManyRequests = "ERR-AUTH-006";

    public const string UnsupportedFileType = "ERR-UPL-001";
    public const string FileTypeMismatch = "ERR-UPL-002";
    public const string FileTooLarge = "ERR-UPL-003";
    public const string TooManyPages = "ERR-UPL-004";
    public const string MalwareDetected = "ERR-UPL-005";

    public const string InvitationInvalid = "ERR-ORG-002";
    public const string LastOwner = "ERR-ORG-003";

    public const string ShareNotOrgMember = "ERR-SHARE-001";
    public const string ShareNoAccount = "ERR-SHARE-002";

    public const string Forbidden = "ERR-SEC-001";

    public const string InvalidStatusTransition = "ERR-CTR-001";
    public const string ContextIncomplete = "ERR-CTR-002";
    public const string ContractBusy = "ERR-CTR-003";

    public const string AnalysisFailed = "ERR-ANA-001";
    public const string ExportFailed = "ERR-EXP-001";
    public const string ChatFailed = "ERR-CHAT-001";
    public const string EmailSendFailed = "ERR-INTG-001";

    // Not in the SRS catalogue: generic validation and not-found responses.
    public const string Validation = "ERR-GEN-VALIDATION";
    public const string NotFound = "ERR-GEN-NOT-FOUND";
}
