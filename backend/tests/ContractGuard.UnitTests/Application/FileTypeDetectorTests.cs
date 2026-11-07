using ContractGuard.Application.Contracts;
using ContractGuard.Domain.Contracts;

namespace ContractGuard.UnitTests.Application;

public sealed class FileTypeDetectorTests
{
    [Theory]
    [InlineData("a.pdf", FileType.Pdf)]
    [InlineData("A.PDF", FileType.Pdf)]
    [InlineData("a.docx", FileType.Docx)]
    [InlineData("a.jpg", FileType.Jpg)]
    [InlineData("a.jpeg", FileType.Jpg)]
    [InlineData("a.png", FileType.Png)]
    [InlineData("hop.dong.v2.pdf", FileType.Pdf)]
    public void Allowed_file_names(string fileName, FileType expected) =>
        Assert.Equal(expected, FileTypeDetector.FromFileName(fileName));

    [Theory]
    [InlineData("a.exe")]
    [InlineData("a.doc")]
    [InlineData("a.pdf.exe")]
    [InlineData("noextension")]
    public void Other_file_names(string fileName) =>
        Assert.Null(FileTypeDetector.FromFileName(fileName));

    [Theory]
    [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31 }, FileType.Pdf)]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, FileType.Png)]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, FileType.Jpg)]
    [InlineData(new byte[] { 0x50, 0x4B, 0x03, 0x04 }, FileType.Docx)]
    public void Known_signatures(byte[] start, FileType expected) =>
        Assert.Equal(expected, FileTypeDetector.FromContent(start));

    [Theory]
    [InlineData(new byte[] { 0x4D, 0x5A, 0x90, 0x00 })] // Windows executable
    [InlineData(new byte[] { 0x25, 0x50, 0x44 })] // Shorter than the PDF signature
    [InlineData(new byte[0])]
    public void Unknown_content(byte[] start) =>
        Assert.Null(FileTypeDetector.FromContent(start));

    [Fact]
    public void Every_file_type_has_a_content_type()
    {
        foreach (var type in Enum.GetValues<FileType>())
        {
            Assert.NotEmpty(FileTypeDetector.ContentTypeOf(type));
        }
    }
}
