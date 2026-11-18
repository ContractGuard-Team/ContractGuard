using ContractGuard.Domain.Contracts;

namespace ContractGuard.Application.Contracts;

// FR-UPL-001..003: the allowed types, recognized by file name and by the first bytes of the content.
// A quick check that lets the upload step refuse a wrong file at once. It does not replace the
// worker's checks (SD-08): a .docx here is only "a ZIP file", which the worker verifies further.
public static class FileTypeDetector
{
    public const int SignatureLength = 8;

    private static readonly (FileType Type, byte[] Signature)[] Signatures =
    [
        (FileType.Pdf, "%PDF-"u8.ToArray()),
        (FileType.Docx, [0x50, 0x4B, 0x03, 0x04]),
    ];

    public static FileType? FromFileName(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => FileType.Pdf,
            ".docx" => FileType.Docx,
            _ => null
        };

    public static string ContentTypeOf(FileType type) =>
        type switch
        {
            FileType.Pdf => "application/pdf",
            FileType.Docx => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    public static FileType? FromContent(ReadOnlySpan<byte> start)
    {
        foreach (var (type, signature) in Signatures)
        {
            if (start.StartsWith(signature))
            {
                return type;
            }
        }

        return null;
    }
}
