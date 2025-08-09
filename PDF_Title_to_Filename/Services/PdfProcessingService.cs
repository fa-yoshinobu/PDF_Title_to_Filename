using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using PdfTitleRenamer.Models;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PdfTitleRenamer.Services
{
    public class PdfProcessingService : IPdfProcessingService
    {
        private readonly ILogger<PdfProcessingService> _logger;
        private readonly ILogService _logService;

            public PdfProcessingService(ILogger<PdfProcessingService> logger, ILogService logService)
    {
        _logger = logger;
        _logService = logService;
        
        // .NET 8でShift_JISなどの追加エンコーディングを使用可能にする
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

        public async Task<ProcessingResult> ProcessFileAsync(string filePath)
        {
            return await Task.Run(() => ProcessFile(filePath));
        }

        private ProcessingResult ProcessFile(string filePath)
        {
            try
            {
                var originalFileName = Path.GetFileName(filePath);
                var title = ExtractTitleFromPdf(filePath);
                
                if (string.IsNullOrWhiteSpace(title))
                {
                    return ProcessingResult.Skipped(originalFileName, "PDFにタイトル情報が見つかりませんでした");
                }

                var processedTitle = title.Normalize(System.Text.NormalizationForm.FormKC);
                var validTitle = SanitizeFileName(processedTitle);
                var newFileName = $"{validTitle}.pdf";
                var newFilePath = Path.Combine(Path.GetDirectoryName(filePath)!, newFileName);

                if (string.Equals(originalFileName, newFileName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(title) && title.Length > 5)
                    {
                        return ProcessingResult.Success(originalFileName, newFileName);
                    }
                    else
                    {
                        return ProcessingResult.Skipped(originalFileName, "既に適切なファイル名です");
                    }
                }

                newFilePath = GetUniqueFilePath(newFilePath);
                newFileName = Path.GetFileName(newFilePath);
                
                File.Move(filePath, newFilePath);
                
                return ProcessingResult.Success(originalFileName, newFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing file: {filePath}");
                return ProcessingResult.Failure(Path.GetFileName(filePath), ex.Message);
            }
        }

        public string? ExtractTitleFromPdf(string filePath)
        {
            try
            {
                using var document = PdfDocument.Open(filePath);
                var title = document.Information?.Title;
                
                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title.Trim();
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting title from PDF: {filePath}");
                return null;
            }
        }

        public string ConvertFullwidthToHalfwidth(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // NFKC正規化で全角英数字を半角に変換
            return text.Normalize(NormalizationForm.FormKC);
        }

        public string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "Untitled";

            // ファイル名に使用できない文字を置換
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = fileName;

            foreach (var invalidChar in invalidChars)
            {
                sanitized = sanitized.Replace(invalidChar, '_');
            }

            // 追加の無効文字パターンを処理
            sanitized = Regex.Replace(sanitized, @"[<>:""/\\|?*\x00-\x1f]", "_");

            // 連続する空白を1つにまとめる
            sanitized = Regex.Replace(sanitized, @"\s+", " ");

            // 先頭と末尾の空白を削除
            sanitized = sanitized.Trim();

            // 空文字列になった場合のフォールバック
            if (string.IsNullOrWhiteSpace(sanitized))
                return "Untitled";

            // Windowsの予約語をチェック
            var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);
            
            if (reservedNames.Contains(nameWithoutExtension.ToUpper()))
            {
                sanitized = $"_{sanitized}";
            }

            // 長すぎるファイル名を切り詰め（255文字制限）
            if (sanitized.Length > 250) // 拡張子の分を考慮
            {
                sanitized = sanitized.Substring(0, 250);
            }

            return sanitized;
        }

        private string GetUniqueFilePath(string filePath)
        {
            if (!File.Exists(filePath))
                return filePath;

            var directory = Path.GetDirectoryName(filePath)!;
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);

            var counter = 1;
            string newFilePath;

            do
            {
                var newFileName = $"{fileNameWithoutExtension}({counter}){extension}";
                newFilePath = Path.Combine(directory, newFileName);
                counter++;
            }
            while (File.Exists(newFilePath));

            return newFilePath;
        }


























    }
}
