namespace PdfTitleRenamer.Services
{
    internal interface ILogService
    {
        void LogMessage(string message);
        void LogError(string message, Exception? exception = null);
        void LogInfo(string message);
        void LogWarning(string message);
        void LogDebug(string message);
        void LogVerbose(string category, string message, object? data = null);
        string GetLogText();
        void ClearLog();
        event EventHandler<string>? LogAdded;
    }
}
