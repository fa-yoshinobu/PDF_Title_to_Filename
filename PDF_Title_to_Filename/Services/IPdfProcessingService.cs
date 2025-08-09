using PdfTitleRenamer.Models;

namespace PdfTitleRenamer.Services
{
    public interface IPdfProcessingService
    {
        Task<ProcessingResult> ProcessFileAsync(string filePath);
        string? ExtractTitleFromPdf(string filePath);
        string SanitizeFileName(string fileName);
        string ConvertFullwidthToHalfwidth(string text);
    }
}
