using Microsoft.Extensions.Logging;
using System.Text;

namespace PdfTitleRenamer.Services
{
    public class LogService : ILogService
    {
        private readonly ILogger<LogService> _logger;
        private readonly StringBuilder _logBuilder = new();
        private readonly object _lock = new();

        public LogService(ILogger<LogService> logger)
        {
            _logger = logger;
        }

        public event EventHandler<string>? LogAdded;

        public void LogMessage(string message)
        {
            LogInfo(message);
        }

        public void LogError(string message, Exception? exception = null)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logMessage = $"[{timestamp}] ERROR: {message}";
            
            if (exception != null)
            {
                logMessage += $"\n{exception}";
            }

            AddToLog(logMessage);
            _logger.LogError(exception, message);
        }

        public void LogInfo(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logMessage = $"[{timestamp}] INFO: {message}";
            
            AddToLog(logMessage);
            _logger.LogInformation(message);
        }

        public void LogWarning(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logMessage = $"[{timestamp}] WARNING: {message}";
            
            AddToLog(logMessage);
            _logger.LogWarning(message);
        }

        public string GetLogText()
        {
            lock (_lock)
            {
                return _logBuilder.ToString();
            }
        }

        public void ClearLog()
        {
            lock (_lock)
            {
                _logBuilder.Clear();
            }
            
            LogAdded?.Invoke(this, "");
        }

        public void LogDebug(string message)
        {
            // 本番環境では詳細なデバッグログは出力しない
            _logger.LogDebug(message);
        }

        public void LogVerbose(string category, string message, object? data = null)
        {
            // 本番環境では詳細なVerboseログは出力しない
            _logger.LogTrace($"[{category}]: {message}");
        }

        private void AddToLog(string message)
        {
            lock (_lock)
            {
                _logBuilder.AppendLine(message);
            }

            LogAdded?.Invoke(this, GetLogText());
        }
    }
}
