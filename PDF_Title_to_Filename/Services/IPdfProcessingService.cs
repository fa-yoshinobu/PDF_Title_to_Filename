using PdfTitleRenamer.Models;

namespace PdfTitleRenamer.Services
{
    internal interface IPdfProcessingService
    {
        PdfMetadata ExtractMetadataFromPdf(string filePath);
        string SanitizeFileName(string fileName, bool applyNFKC = true);
        string GetUniqueFilePath(string filePath);
    }
}
