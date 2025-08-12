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
        private readonly ILanguageService _languageService;

        public PdfProcessingService(ILogger<PdfProcessingService> logger, ILogService logService, ILanguageService languageService)
        {
            _logger = logger;
            _logService = logService;
            _languageService = languageService;
            
            // .NET 8でShift_JISなどの追加エンコーディングを使用可能にする
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }



        public PdfMetadata ExtractMetadataFromPdf(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogError($"{_languageService?.GetString("PDFFileNotFound") ?? "PDF file not found"}: {filePath}");
                    return new PdfMetadata();
                }

                using var document = PdfDocument.Open(filePath);
                var info = document.Information;
                
                var metadata = new PdfMetadata
                {
                    Title = info?.Title?.Trim() ?? "",
                    Author = info?.Author?.Trim() ?? "",
                    Subject = info?.Subject?.Trim() ?? "",
                    Keywords = info?.Keywords?.Trim() ?? "",
                    Creator = info?.Creator?.Trim() ?? "",
                    Producer = info?.Producer?.Trim() ?? ""
                };



                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_languageService?.GetString("PDFMetadataExtractionFailed") ?? "PDF metadata extraction failed"}: {filePath}");
                return new PdfMetadata();
            }
        }



        public string SanitizeFileName(string fileName, bool applyNFKC = true)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "Untitled";

            // NFKC正規化で全角英数字を半角に変換（オプション）
            var sanitized = applyNFKC ? fileName.Normalize(NormalizationForm.FormKC) : fileName;

            // ファイル名に使用できない文字を置換
            var invalidChars = Path.GetInvalidFileNameChars();

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

            // 長すぎるファイル名を切り詰め（Windowsの最大パス長制限を考慮）
            var maxLength = 240; // 拡張子とパスの分を考慮
            if (sanitized.Length > maxLength)
            {
                sanitized = sanitized.Substring(0, maxLength);
            }

            return sanitized;
        }

        public string GetUniqueFilePath(string filePath)
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
