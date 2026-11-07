using ContractGuard.Application.Abstractions;
using ContractGuard.Application.Analysis;
using ContractGuard.Domain.Common;
using ContractGuard.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContractGuard.Application.Contracts;

// F-CTR-001, first half: the file goes straight to quarantine/ in storage (SD-08),
// then the contract is created from what actually arrived there, never from what the browser says.
public sealed partial class ContractUploadService(
    IApplicationDbContext db,
    IObjectStorage storage,
    ICurrentWorkspace workspace,
    TimeProvider clock,
    ILogger<ContractUploadService> logger)
{
    private static readonly TimeSpan UploadLinkLifetime = TimeSpan.FromMinutes(15);

    public async Task<UploadTicket> RequestUploadAsync(string fileName, long sizeBytes, CancellationToken cancellationToken)
    {
        RequireAllowedType(fileName);
        if (sizeBytes <= 0)
        {
            throw new DomainException(ErrorCodes.Validation, "The file is empty.");
        }

        if (sizeBytes > ContractFile.MaxSizeBytes)
        {
            throw new DomainException(ErrorCodes.FileTooLarge, "The file is larger than 20 MB.");
        }

        var uploadId = Guid.NewGuid();
        var upload = await storage.CreateUploadAsync(KeyOf(uploadId, fileName), ContractFile.MaxSizeBytes, UploadLinkLifetime, cancellationToken);
        return new UploadTicket(uploadId, upload.Url, upload.Fields);
    }

    public async Task<CreatedContract> CreateContractAsync(Guid uploadId, string fileName, CancellationToken cancellationToken)
    {
        var declaredType = RequireAllowedType(fileName);
        var key = KeyOf(uploadId, fileName);

        // Checked before the file checks below, which delete a bad file: it may be in use already.
        if (await IsKeyUsedAsync(key, cancellationToken))
        {
            throw UploadAlreadyUsed();
        }

        var size = await storage.GetSizeAsync(key, cancellationToken)
            ?? throw new DomainException(ErrorCodes.NotFound, "The uploaded file was not found.");
        if (size > ContractFile.MaxSizeBytes)
        {
            await storage.DeleteAsync(key, cancellationToken);
            throw new DomainException(ErrorCodes.FileTooLarge, "The file is larger than 20 MB.");
        }

        var actualType = FileTypeDetector.FromContent(await storage.ReadStartAsync(key, FileTypeDetector.SignatureLength, cancellationToken));
        if (actualType != declaredType)
        {
            await storage.DeleteAsync(key, cancellationToken);
            if (actualType is null)
            {
                throw new DomainException(ErrorCodes.UnsupportedFileType, "The file content is not a PDF, DOCX, JPG or PNG.");
            }

            // Security event (ERR-UPL-002), without any file content.
            LogTypeMismatch(declaredType, actualType.Value);
            throw new DomainException(ErrorCodes.FileTypeMismatch, "The file content does not match its extension.");
        }

        var contract = Contract.Create(
            Path.GetFileNameWithoutExtension(fileName),
            workspace.OrganizationId,
            workspace.UserId,
            new ContractFile(key, fileName, declaredType, size),
            clock.GetUtcNow());
        db.Contracts.Add(contract);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two requests can pass the check above at the same moment; the unique index refuses the second.
            db.DiscardChanges();
            if (await IsKeyUsedAsync(key, cancellationToken))
            {
                throw UploadAlreadyUsed();
            }

            throw;
        }

        return new CreatedContract(contract.ContractId, contract.LatestVersion.ContractVersionId);
    }

    // Past the query filters: the same user may have used this upload in another workspace.
    private Task<bool> IsKeyUsedAsync(string key, CancellationToken cancellationToken) =>
        db.ContractVersions.IgnoreQueryFilters().AnyAsync(v => v.SourceFileKey == key, cancellationToken);

    private static DomainException UploadAlreadyUsed() =>
        new(ErrorCodes.Validation, "This upload already belongs to a contract.");

    // The key is built here from the signed-in user, so nobody can claim another person's upload.
    private string KeyOf(Guid uploadId, string fileName) =>
        AnalysisStorageKeys.QuarantineKey(workspace.UserId, uploadId, Path.GetExtension(fileName).ToLowerInvariant());

    private static FileType RequireAllowedType(string fileName)
    {
        if (string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(fileName)) || fileName.Length > ContractVersion.FileNameMaxLength)
        {
            throw new DomainException(ErrorCodes.Validation, "The file name is empty or too long.");
        }

        return FileTypeDetector.FromFileName(fileName)
            ?? throw new DomainException(ErrorCodes.UnsupportedFileType, "Only PDF, DOCX, JPG and PNG files are accepted.");
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Upload refused: declared {DeclaredType} but content is {ActualType}")]
    private partial void LogTypeMismatch(FileType declaredType, FileType actualType);
}
