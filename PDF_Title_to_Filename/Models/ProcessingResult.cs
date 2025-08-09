namespace PdfTitleRenamer.Models
{
    public class ProcessingResult
    {
        public ProcessingResult(bool isSuccess, string originalFileName, string? newFileName = null, string? errorMessage = null)
        {
            IsSuccess = isSuccess;
            OriginalFileName = originalFileName;
            NewFileName = newFileName;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; }
        public string OriginalFileName { get; }
        public string? NewFileName { get; }
        public string? ErrorMessage { get; }

        public static ProcessingResult Success(string originalFileName, string newFileName)
        {
            return new ProcessingResult(true, originalFileName, newFileName);
        }

        public static ProcessingResult Failure(string originalFileName, string errorMessage)
        {
            return new ProcessingResult(false, originalFileName, null, errorMessage);
        }

        public static ProcessingResult Skipped(string originalFileName, string reason)
        {
            return new ProcessingResult(true, originalFileName, originalFileName, reason);
        }
    }
}
