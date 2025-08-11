using PdfTitleRenamer.Models;

namespace PdfTitleRenamer.Services
{
    public interface IPdfProcessingService
    {
        PdfMetadata ExtractMetadataFromPdf(string filePath);
        string SanitizeFileName(string fileName, bool applyNFKC = true);
        string GetUniqueFilePath(string filePath);
    }
}
