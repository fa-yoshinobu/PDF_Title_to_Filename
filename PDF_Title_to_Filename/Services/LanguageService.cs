using System.Globalization;
using System.Resources;
using System.IO;
using PdfTitleRenamer.Models;

namespace PdfTitleRenamer.Services
{
    public class LanguageService : ILanguageService
    {
        private readonly ResourceManager _resourceManager;
        private readonly FileNameSettings _settings;
        private CultureInfo _currentCulture;

        public event EventHandler? LanguageChanged;

        public string CurrentLanguage => _settings.CurrentLanguage;

        public LanguageService()
        {
            _resourceManager = new ResourceManager("PdfTitleRenamer.Resources.Strings", typeof(LanguageService).Assembly);
            _settings = FileNameSettings.Load();
            _currentCulture = new CultureInfo(_settings.CurrentLanguage);
        }

        public string GetString(string key)
        {
            try
            {
                // 直接的な文字列マッピングを使用
                var result = GetLocalizedString(key, _currentCulture.Name);
                return result ?? key;
            }
            catch
            {
                return key;
            }
        }

        private string? GetLocalizedString(string key, string culture)
        {
            return culture switch
            {
                "ja" => GetJapaneseString(key),
                "en" => GetEnglishString(key),
                _ => GetEnglishString(key) // デフォルトは英語
            };
        }

        private string? GetJapaneseString(string key)
        {
            return key switch
            {
                "FileSelection" => "ファイル選択",
                "ProcessStart" => "処理開始",
                "ClearList" => "リストクリア",
                "CurrentName" => "現在の名前",
                "NewName" => "新しい名前",
                "Status" => "ステータス",
                "Operation" => "操作",
                "ProcessingResults" => "処理結果",
                "DragDropFiles" => "PDFファイルまたはフォルダをここにドラッグ＆ドロップ",
                "DragDropSubText" => "フォルダ内のPDFファイルも自動検索されます",
                "Or" => "または",
                "ClickToSelect" => "クリックして選択",
                "FilesSelected" => "ファイル選択済み",
                "SettingsToolTip" => "ファイル名設定",
                "AboutToolTip" => "アプリについて・ライセンス情報",
                "LanguageToolTip" => "言語切り替え",
                "DeleteToolTip" => "削除",
                "ClearLogToolTip" => "ログをクリア",
                "AboutWindowTitle" => "アプリについて",
                "AppDescription" => "PDFファイルのメタデータからタイトルを抽出し、ファイル名として自動設定するアプリケーションです",
                "AboutTab" => "アプリについて",
                "LicenseTab" => "オープンソースライセンス",
                "OperationHeader" => "操作",
                "CurrentNameHeader" => "現在の名前",
                "NewNameHeader" => "新しい名前",
                "StatusHeader" => "ステータス",
                "NoMetadata" => "メタデータなし",
                "NoTitle" => "タイトルなし",
                "RenameComplete" => "リネーム完了",
                "RenameScheduled" => "リネーム予定",
                "Processing" => "処理中...",
                "NoChangeNeeded" => "変更不要",
                "Waiting" => "待機中",
                "Error" => "エラー",
                "NoMetadataError" => "PDFファイルにメタデータ情報が設定されていません",
                "AlreadyProperlySet" => "ファイル名は既に適切に設定されています",
                "MetadataExtractionError" => "メタデータ抽出エラー",
                "FileMoveError" => "ファイル移動エラー",
                "ProcessingError" => "処理中にエラーが発生しました",
                // SettingsWindow
                "SettingsWindowTitle" => "ファイル名設定",
                "FileNameElementsHeader" => "ファイル名に含める要素（矢印ボタンで順序変更可能）",
                "ElementsDescription" => "チェックボックスで有効/無効を切り替え、矢印ボタンで順序を変更できます。プレフィックスとサフィックスも位置を変更可能です。",
                "CustomStringSettingsHeader" => "カスタム文字列設定",
                "PrefixLabel" => "プレフィックス:",
                "SuffixLabel" => "サフィックス:",
                "PrefixToolTip" => "プレフィックスとして使用する文字列（上記の要素リストで有効にしてください）",
                "SuffixToolTip" => "サフィックスとして使用する文字列（上記の要素リストで有効にしてください）",
                "SeparatorSettingsHeader" => "セパレータ設定",
                "SeparatorLabel" => "要素間の区切り文字:",
                "SeparatorToolTip" => "複数の要素を結合する際の区切り文字",
                "PreviewHeader" => "プレビュー",
                "PreviewLabel" => "生成されるファイル名:",
                "SaveButton" => "保存",
                "ResetButton" => "リセット",
                "CancelButton" => "キャンセル",
                // AboutWindow
                "AppInfoHeader" => "📱 アプリケーション情報",
                "VersionLabel" => "バージョン:",
                "AuthorLabel" => "作者:",
                "LinkLabel" => "リンク:",
                "DevelopmentLanguageLabel" => "開発言語:",
                "UIFrameworkLabel" => "UI フレームワーク:",
                "DesignLabel" => "デザイン:",
                "SupportedOSLabel" => "対応OS:",
                "MainFeaturesHeader" => "⚡ 主な機能",
                "Feature1" => "• PDFファイルのメタデータからタイトル情報を自動抽出",
                "Feature2" => "• 抽出したタイトルをファイル名として自動設定",
                "Feature3" => "• 複数ファイルの一括処理対応",
                "Feature4" => "• 処理状況のリアルタイム表示",
                "Feature5" => "• エラーハンドリングとログ機能",
                "Feature6" => "• ドラッグ&ドロップ対応",
                "UsageHeader" => "📖 使用方法",
                "Usage1" => "1. 「ファイル選択」ボタンでPDFファイルを選択",
                "Usage2" => "2. または、PDFファイルをウィンドウにドラッグ&ドロップ",
                "Usage3" => "3. 「処理開始」ボタンでファイル名の変更を実行",
                "Usage4" => "4. 処理状況と結果をログで確認",
                "LicenseLabel" => "ライセンス:",
                // MainWindowViewModel
                "SelectPDFFilesTitle" => "処理するPDFファイルを選択してください",
                "ProcessingStartLog" => "処理開始",
                "PDFMetadataLog" => "PDFメタデータ",
                "SettingsUpdatedLog" => "設定が更新されました。プレビューを更新します。",
                "SettingsWindowError" => "設定ウィンドウの表示に失敗しました",
                // FileNameElement
                "PDFTitleDisplay" => "PDFのタイトル",
                "PDFSubjectDisplay" => "PDFのサブタイトル",
                "PDFAuthorDisplay" => "PDFの作成者",
                "OriginalFileNameDisplay" => "変更前のファイル名",
                "PDFKeywordsDisplay" => "PDFのキーワード",
                "CustomPrefixDisplay" => "カスタムプレフィックス",
                "CustomSuffixDisplay" => "カスタムサフィックス",
                // App.xaml.cs
                "AppStartupError" => "アプリケーションの起動に失敗しました。",
                "StartupErrorTitle" => "起動エラー",
                "AppError" => "アプリケーションでエラーが発生しました。",
                "AppErrorTitle" => "アプリケーションエラー",
                // MainWindow.xaml.cs
                "DragDropError" => "ドラッグ&ドロップエラー",
                "ErrorTitle" => "エラー",
                // Progress text
                "Ready" => "準備完了",
                // File dialog
                "PDFFileFilter" => "PDFファイル (*.pdf)|*.pdf",
                // Log messages
                "GeneratedFileNameLog" => "生成されたファイル名",
                "TargetFileCountLog" => "対象ファイル数",
                "SkippedCountLog" => "スキップ",
                "FilesUnit" => "件",
                "ProcessingCompleteLog" => "処理完了",
                "SuccessCountLog" => "成功",
                "ErrorCountLog" => "エラー",
                "ProcessingErrorLog" => "処理エラー",
                "ProcessingErrorOccurred" => "処理エラーが発生しました",
                "AboutWindowDisplayError" => "AboutWindowの表示に失敗しました",
                // Preview values
                "SampleTitle" => "サンプルタイトル",
                "SampleAuthor" => "作成者名",
                "SampleSubject" => "サブタイトル",
                "SampleKeywords" => "キーワード",
                "ProcessingPending" => "処理待ち...",
                // AboutWindow values
                "DevelopmentLanguageValue" => "C# (.NET 8.0)",
                "UIFrameworkValue" => "WPF (Windows Presentation Foundation)",
                "DesignValue" => "Material Design 風",
                "SupportedOSValue" => "Windows 10/11 (x64)",
                // Log messages
                "PDFFileNotFound" => "PDFファイルが存在しません",
                "PDFMetadataExtractionFailed" => "PDFメタデータの抽出に失敗しました",
                // Error messages
                "ErrorPrefix" => "エラー:",
                // AboutWindow
                "VersionPrefix" => "バージョン",
                _ => null
            };
        }

        private string? GetEnglishString(string key)
        {
            return key switch
            {
                "FileSelection" => "Select Files",
                "ProcessStart" => "Start Processing",
                "ClearList" => "Clear List",
                "CurrentName" => "Current Name",
                "NewName" => "New Name",
                "Status" => "Status",
                "Operation" => "Operation",
                "ProcessingResults" => "Processing Results",
                "DragDropFiles" => "Drag and drop PDF files or folders here",
                "DragDropSubText" => "PDF files in folders are automatically detected",
                "Or" => "or",
                "ClickToSelect" => "Click to select",
                "FilesSelected" => "files selected",
                "SettingsToolTip" => "File name settings",
                "AboutToolTip" => "About application & license information",
                "LanguageToolTip" => "Language switch",
                "DeleteToolTip" => "Delete",
                "ClearLogToolTip" => "Clear log",
                "AboutWindowTitle" => "About Application",
                "AppDescription" => "An application that extracts titles from PDF metadata and automatically sets them as filenames",
                "AboutTab" => "About Application",
                "LicenseTab" => "Open Source License",
                "OperationHeader" => "Operation",
                "CurrentNameHeader" => "Current Name",
                "NewNameHeader" => "New Name",
                "StatusHeader" => "Status",
                "NoMetadata" => "No Metadata",
                "NoTitle" => "No Title",
                "RenameComplete" => "Rename Complete",
                "RenameScheduled" => "Rename Scheduled",
                "Processing" => "Processing...",
                "NoChangeNeeded" => "No Change Needed",
                "Waiting" => "Waiting",
                "Error" => "Error",
                "NoMetadataError" => "No metadata information is set in the PDF file",
                "AlreadyProperlySet" => "The filename is already properly set",
                "MetadataExtractionError" => "Metadata extraction error",
                "FileMoveError" => "File move error",
                "ProcessingError" => "An error occurred during processing",
                // SettingsWindow
                "SettingsWindowTitle" => "File Name Settings",
                "FileNameElementsHeader" => "Elements to include in filename (use arrow buttons to change order)",
                "ElementsDescription" => "Use checkboxes to enable/disable and arrow buttons to change order. Prefix and suffix positions can also be changed.",
                "CustomStringSettingsHeader" => "Custom String Settings",
                "PrefixLabel" => "Prefix:",
                "SuffixLabel" => "Suffix:",
                "PrefixToolTip" => "String to use as prefix (enable in the element list above)",
                "SuffixToolTip" => "String to use as suffix (enable in the element list above)",
                "SeparatorSettingsHeader" => "Separator Settings",
                "SeparatorLabel" => "Separator between elements:",
                "SeparatorToolTip" => "Character to separate multiple elements",
                "PreviewHeader" => "Preview",
                "PreviewLabel" => "Generated filename:",
                "SaveButton" => "Save",
                "ResetButton" => "Reset",
                "CancelButton" => "Cancel",
                // AboutWindow
                "AppInfoHeader" => "📱 Application Information",
                "VersionLabel" => "Version:",
                "AuthorLabel" => "Author:",
                "LinkLabel" => "Link:",
                "DevelopmentLanguageLabel" => "Development Language:",
                "UIFrameworkLabel" => "UI Framework:",
                "DesignLabel" => "Design:",
                "SupportedOSLabel" => "Supported OS:",
                "MainFeaturesHeader" => "⚡ Main Features",
                "Feature1" => "• Automatically extract title information from PDF metadata",
                "Feature2" => "• Automatically set extracted titles as filenames",
                "Feature3" => "• Batch processing support for multiple files",
                "Feature4" => "• Real-time processing status display",
                "Feature5" => "• Error handling and logging functionality",
                "Feature6" => "• Drag & drop support",
                "UsageHeader" => "📖 How to Use",
                "Usage1" => "1. Select PDF files using the \"Select Files\" button",
                "Usage2" => "2. Or drag and drop PDF files into the window",
                "Usage3" => "3. Execute filename changes using the \"Start Processing\" button",
                "Usage4" => "4. Check processing status and results in the log",
                "LicenseLabel" => "License:",
                // MainWindowViewModel
                "SelectPDFFilesTitle" => "Select PDF files to process",
                "ProcessingStartLog" => "Processing started",
                "PDFMetadataLog" => "PDF metadata",
                "SettingsUpdatedLog" => "Settings updated. Updating preview.",
                "SettingsWindowError" => "Failed to display settings window",
                // FileNameElement
                "PDFTitleDisplay" => "PDF Title",
                "PDFSubjectDisplay" => "PDF Subject",
                "PDFAuthorDisplay" => "PDF Author",
                "OriginalFileNameDisplay" => "Original File Name",
                "PDFKeywordsDisplay" => "PDF Keywords",
                "CustomPrefixDisplay" => "Custom Prefix",
                "CustomSuffixDisplay" => "Custom Suffix",
                // App.xaml.cs
                "AppStartupError" => "Application startup failed.",
                "StartupErrorTitle" => "Startup Error",
                "AppError" => "An error occurred in the application.",
                "AppErrorTitle" => "Application Error",
                // MainWindow.xaml.cs
                "DragDropError" => "Drag & Drop Error",
                "ErrorTitle" => "Error",
                // Progress text
                "Ready" => "Ready",
                // File dialog
                "PDFFileFilter" => "PDF files (*.pdf)|*.pdf",
                // Log messages
                "GeneratedFileNameLog" => "Generated filename",
                "TargetFileCountLog" => "Target file count",
                "SkippedCountLog" => "Skipped",
                "FilesUnit" => "files",
                "ProcessingCompleteLog" => "Processing complete",
                "SuccessCountLog" => "Success",
                "ErrorCountLog" => "Error",
                "ProcessingErrorLog" => "Processing error",
                "ProcessingErrorOccurred" => "Processing error occurred",
                "AboutWindowDisplayError" => "Failed to display AboutWindow",
                // Preview values
                "SampleTitle" => "Sample Title",
                "SampleAuthor" => "Author Name",
                "SampleSubject" => "Subject",
                "SampleKeywords" => "Keywords",
                "ProcessingPending" => "Processing pending...",
                // AboutWindow values
                "DevelopmentLanguageValue" => "C# (.NET 8.0)",
                "UIFrameworkValue" => "WPF (Windows Presentation Foundation)",
                "DesignValue" => "Material Design Style",
                "SupportedOSValue" => "Windows 10/11 (x64)",
                // Log messages
                "PDFFileNotFound" => "PDF file not found",
                "PDFMetadataExtractionFailed" => "PDF metadata extraction failed",
                // Error messages
                "ErrorPrefix" => "Error:",
                // AboutWindow
                "VersionPrefix" => "Version",
                _ => null
            };
        }

        public void SetLanguage(string languageCode)
        {
            if (_settings.CurrentLanguage != languageCode)
            {
                _settings.CurrentLanguage = languageCode;
                _currentCulture = new CultureInfo(languageCode);
                _settings.Save();
                LanguageChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
