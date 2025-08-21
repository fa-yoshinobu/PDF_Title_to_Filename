// ログ出力の制御
#if DEBUG
#define ENABLE_DETAILED_LOGGING
#endif

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.IO;
using Microsoft.Win32;
using PdfTitleRenamer.Models;
using PdfTitleRenamer.Services;
using System.Windows;

namespace PdfTitleRenamer.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        // 状態定数（言語に依存しない）
        private const string STATUS_WAITING = "Waiting";
        private const string STATUS_PROCESSING = "Processing";
        private const string STATUS_RENAME_COMPLETE = "RenameComplete";
        private const string STATUS_RENAME_SCHEDULED = "RenameScheduled";
        private const string STATUS_NO_TITLE = "NoTitle";
        private const string STATUS_NO_METADATA = "NoMetadata";
        private const string STATUS_NO_CHANGE_NEEDED = "NoChangeNeeded";
        private const string STATUS_ERROR = "Error";

        private readonly IPdfProcessingService _pdfProcessingService;
        private readonly ILogService _logService;
        private readonly ILanguageService _languageService;
        private readonly FileNameSettings _settings;

        private string _logText = "";
        private string _progressText = "";
        private double _progressValue = 0;
        private bool _isProcessing = false;

        public MainWindowViewModel(
            IPdfProcessingService pdfProcessingService,
            ILogService logService,
            ILanguageService languageService)
        {
            _pdfProcessingService = pdfProcessingService ?? throw new ArgumentNullException(nameof(pdfProcessingService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _languageService = languageService ?? throw new ArgumentNullException(nameof(languageService));
            _settings = FileNameSettings.Load();

            // 言語変更イベントを購読
#pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です
            _languageService.LanguageChanged += (s, e) => {
                OnPropertyChanged(string.Empty);
                // FileItemのStatusDisplayも更新
                foreach (var fileItem in SelectedFiles)
                {
                    fileItem.UpdateStatusDisplay();
                }
            };
#pragma warning restore CS8602

            // ログサービスのイベントを購読
            _logService.LogAdded += (s, e) => {
                LogText = e;
            };

            // FileNameElementにLanguageServiceを設定
            FileNameElement.SetLanguageService(_languageService);

            SelectedFiles = new ObservableCollection<FileItem>();
            
            // Commands
            SelectFilesCommand = new RelayCommand(SelectFiles);
            ProcessFilesCommand = new RelayCommand(async () => await ProcessFilesAsync(), () => HasFiles && !IsProcessing && HasValidMetadataSettings());
            ClearFilesCommand = new RelayCommand(ClearFiles, () => HasFiles);
            RemoveFileCommand = new RelayCommand<FileItem>(RemoveFile);
            ClearLogCommand = new RelayCommand(ClearLog);
            ShowAboutCommand = new RelayCommand(ShowAbout);
            ShowSettingsCommand = new RelayCommand(ShowSettings);
            ChangeLanguageCommand = new RelayCommand<string>(ChangeLanguage);

            // Subscribe to property changes to update command states
            PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(HasFiles) || e.PropertyName == nameof(IsProcessing))
                {
                    (ProcessFilesCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (ClearFilesCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            };
            
            // 設定変更時にコマンドの状態を更新
            _settings.Elements.CollectionChanged += (s, e) => {
                (ProcessFilesCommand as RelayCommand)?.RaiseCanExecuteChanged();
            };
        }

        public ObservableCollection<FileItem> SelectedFiles { get; }

        public string LogText
        {
            get => _logText;
            set => SetProperty(ref _logText, value);
        }

        public string ProgressText
        {
            get => string.IsNullOrEmpty(_progressText) ? _languageService?.GetString("Ready") ?? "Ready" : _progressText;
            set => SetProperty(ref _progressText, value);
        }

        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public bool HasFiles => SelectedFiles.Count > 0;

        public string FileCountText => $"{SelectedFiles.Count} {_languageService?.GetString("FilesSelected") ?? "Files Selected"}";

        // UI文字列プロパティ
        public string FileSelectionText => _languageService.GetString("FileSelection");
        public string ProcessStartText => _languageService.GetString("ProcessStart");
        public string ClearListText => _languageService.GetString("ClearList");
        public string CurrentNameText => _languageService.GetString("CurrentName");
        public string NewNameText => _languageService.GetString("NewName");
        public string StatusText => _languageService.GetString("Status");
        public string OperationText => _languageService.GetString("Operation");
        public string ProcessingLogText => _languageService.GetString("ProcessingResults");
        public string DragDropFilesText => _languageService.GetString("DragDropFiles");
        public string DragDropSubText => _languageService.GetString("DragDropSubText");
        public string OrText => _languageService.GetString("Or");
        public string ClickToSelectText => _languageService.GetString("ClickToSelect");
        public string SettingsToolTip => _languageService.GetString("SettingsToolTip");
        public string AboutToolTip => _languageService.GetString("AboutToolTip");
        public string LanguageToolTip => _languageService.GetString("LanguageToolTip");
        public string DeleteToolTip => _languageService.GetString("DeleteToolTip");
        public string ClearLogToolTip => _languageService.GetString("ClearLogToolTip");
        public string OperationHeader => _languageService.GetString("OperationHeader");
        public string CurrentNameHeader => _languageService.GetString("CurrentNameHeader");
        public string NewNameHeader => _languageService.GetString("NewNameHeader");
        public string StatusHeader => _languageService.GetString("StatusHeader");

        // 現在の言語を表示するプロパティ
        public string CurrentLanguageDisplay => _languageService.CurrentLanguage == "ja" ? "Japanese" : "English";

        // Status strings for DataGrid triggers
        public string Error => _languageService.GetString("Error");
        public string NoTitle => _languageService.GetString("NoTitle");
        public string NoMetadata => _languageService.GetString("NoMetadata");
        public string RenameComplete => _languageService.GetString("RenameComplete");
        public string RenameScheduled => _languageService.GetString("RenameScheduled");
        public string Processing => _languageService.GetString("Processing");
        public string NoChangeNeeded => _languageService.GetString("NoChangeNeeded");
        public string Waiting => _languageService.GetString("Waiting");

        // Commands
        public ICommand SelectFilesCommand { get; }
        public ICommand ProcessFilesCommand { get; }
        public ICommand ClearFilesCommand { get; }
        public ICommand RemoveFileCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand ShowAboutCommand { get; }
        public ICommand ShowSettingsCommand { get; }
        public ICommand ChangeLanguageCommand { get; }

        public void AddFiles(string[] filePaths)
        {
            var addedCount = 0;
            foreach (var filePath in filePaths)
            {
                if (File.Exists(filePath) && filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    if (!SelectedFiles.Any(f => f.FilePath == filePath))
                    {
                        var fileItem = new FileItem(filePath);
                        SelectedFiles.Add(fileItem);
                        addedCount++;
                        
                        _ = SetPredictedFileNameAsync(fileItem);
                    }
                }
            }
            
            OnPropertyChanged(nameof(HasFiles));
            OnPropertyChanged(nameof(FileCountText));
        }

        private async Task SetPredictedFileNameAsync(FileItem fileItem)
        {
            try
            {
                var metadata = await Task.Run(() => _pdfProcessingService.ExtractMetadataFromPdf(fileItem.FilePath));
                
#if ENABLE_DETAILED_LOGGING
                // デバッグ用ログ
                _logService.LogInfo($"{_languageService.GetString("PDFMetadataLog")}: Title='{metadata.Title}', Author='{metadata.Author}', Subject='{metadata.Subject}', Keywords='{metadata.Keywords}'");
                
                // 有効な要素のデバッグログ
                var enabledElements = _settings.Elements.Where(e => e.IsEnabled).ToList();
                _logService.LogInfo($"Enabled elements: {string.Join(", ", enabledElements.Select(e => e.ElementType))}");
                _logService.LogInfo($"Total elements: {_settings.Elements.Count}, Enabled: {enabledElements.Count}");
                
                // メタデータの詳細ログ
                _logService.LogInfo($"Metadata details - Title: '{metadata.Title}' (Length: {metadata.Title?.Length ?? 0})");
                _logService.LogInfo($"Metadata details - Author: '{metadata.Author}' (Length: {metadata.Author?.Length ?? 0})");
                _logService.LogInfo($"Metadata details - Subject: '{metadata.Subject}' (Length: {metadata.Subject?.Length ?? 0})");
                _logService.LogInfo($"Metadata details - Keywords: '{metadata.Keywords}' (Length: {metadata.Keywords?.Length ?? 0})");
                
                // ファイル名生成の詳細ログ
                _logService.LogInfo($"GenerateFileName input - Title: '{metadata.Title ?? string.Empty}', Author: '{metadata.Author ?? string.Empty}', Subject: '{metadata.Subject ?? string.Empty}', Keywords: '{metadata.Keywords ?? string.Empty}'");
                
                // 設定の詳細ログ
                _logService.LogInfo($"Settings - Separator: '{_settings.Separator}', CustomPrefix: '{_settings.CustomPrefix}', CustomSuffix: '{_settings.CustomSuffix}'");
                _logService.LogInfo($"Settings Elements:");
                foreach (var element in _settings.Elements)
                {
                    _logService.LogInfo($"  - {element.ElementType}: Enabled={element.IsEnabled}");
                }
#endif
                

                
                // 設定に基づいてファイル名を生成（メタデータチェックも含む）
                var generatedFileName = _settings.GenerateFileName(
                    fileItem.FileName,
                    metadata.Title ?? string.Empty,
                    metadata.Author ?? string.Empty,
                    metadata.Subject ?? string.Empty,
                    metadata.Keywords ?? string.Empty);

#if ENABLE_DETAILED_LOGGING
                // ファイル名生成の詳細ログ
                _logService.LogInfo($"Generated filename: '{generatedFileName}'");
                _logService.LogInfo($"Enabled elements count: {_settings.Elements.Count(e => e.IsEnabled)}");
                foreach (var element in _settings.Elements.Where(e => e.IsEnabled))
                {
                    var elementValue = element.ElementType switch
                    {
                        "Title" => metadata.Title,
                        "Author" => metadata.Author,
                        "Subject" => metadata.Subject,
                        "Keywords" => metadata.Keywords,
                        "OriginalFileName" => Path.GetFileNameWithoutExtension(fileItem.FileName),
                        "CustomPrefix" => _settings.CustomPrefix,
                        "CustomSuffix" => _settings.CustomSuffix,
                        _ => "Unknown"
                    };
                    _logService.LogInfo($"  Element '{element.ElementType}': '{elementValue}' (IsEmpty: {string.IsNullOrWhiteSpace(elementValue)})");
                }
#endif

                _logService.LogInfo($"{_languageService.GetString("GeneratedFileNameLog")}: '{generatedFileName}'");

                // 生成されたファイル名が空の場合は「メタデータなし」
                if (string.IsNullOrWhiteSpace(generatedFileName))
                {
                    fileItem.NewFileName = "NoMetadata";
                    fileItem.Status = STATUS_NO_METADATA;
                    fileItem.ErrorDetails = _languageService.GetString("NoMetadataError");
                    return;
                }

                // ファイル名をサニタイズ（NFKC正規化制御付き）
                var validFileName = SanitizeFileNameWithNFKCControl(generatedFileName);
                var predictedFileName = $"{validFileName}.pdf";
                
                fileItem.NewFileName = predictedFileName;
                
                // 元のファイル名と予想ファイル名を比較
                if (string.Equals(fileItem.FileName, predictedFileName, StringComparison.OrdinalIgnoreCase))
                {
                    fileItem.Status = STATUS_NO_CHANGE_NEEDED;
                    fileItem.ErrorDetails = _languageService.GetString("AlreadyProperlySet");
                }
                else
                {
                    fileItem.Status = STATUS_RENAME_SCHEDULED;
                }
            }
            catch (Exception ex)
            {
                fileItem.NewFileName = "Error";
                fileItem.Status = STATUS_ERROR;
                fileItem.ErrorDetails = $"{_languageService.GetString("MetadataExtractionError")}: {ex.Message}";
            }
        }

        private void SelectFiles()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = _languageService.GetString("PDFFileFilter"),
                Multiselect = true,
                Title = _languageService.GetString("SelectPDFFilesTitle")
            };

            if (openFileDialog.ShowDialog() == true)
            {
                AddFiles(openFileDialog.FileNames);
            }
        }

        private async Task ProcessFilesAsync()
        {
            if (!HasFiles || IsProcessing) return;

            // PDFメタデータ項目がすべて無効な場合の警告
            if (!HasValidMetadataSettings())
            {
                MessageBox.Show(
                    _languageService.GetString("NoMetadataSettingsWarning"),
                    _languageService.GetString("NoMetadataSettingsWarningTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            IsProcessing = true;
            ProgressValue = 0;
            ProgressText = _languageService.GetString("Processing");

            try
            {
                var allFiles = SelectedFiles.ToList();
                // 状態比較は固定文字列を使用（言語に依存しない）
                var filesToProcess = allFiles.Where(f => 
                    f.Status != STATUS_RENAME_COMPLETE && 
                    f.Status != STATUS_NO_TITLE && 
                    f.Status != STATUS_NO_CHANGE_NEEDED).ToList();
                var skippedCount = allFiles.Count - filesToProcess.Count;
                var totalFiles = filesToProcess.Count;
                var processedFiles = 0;
                var successCount = 0;
                var skippedInProcessCount = 0;
                var errorCount = 0;

                LogText = $"{_languageService.GetString("ProcessingStartLog")}: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
                LogText += $"{_languageService.GetString("TargetFileCountLog")}: {totalFiles}";
                if (skippedCount > 0)
                {
                    LogText += $" ({_languageService.GetString("SkippedCountLog")}: {skippedCount}{_languageService.GetString("FilesUnit")})";
                }
                LogText += "\n\n";

                foreach (var fileItem in filesToProcess)
                {
                    try
                    {
                        var currentFile = processedFiles + 1;
                        ProgressText = $"{_languageService.GetString("Processing")} ({currentFile}/{totalFiles}) {Path.GetFileName(fileItem.FilePath)}";
                        fileItem.Status = STATUS_PROCESSING;
                        
                        // 新しい設定システムを使用してファイル名を生成
                        var metadata = await Task.Run(() => _pdfProcessingService.ExtractMetadataFromPdf(fileItem.FilePath));
                        

                        
                        // 設定に基づいてファイル名を生成（メタデータチェックも含む）
                        var generatedFileName = _settings.GenerateFileName(
                            fileItem.FileName,
                            metadata.Title ?? string.Empty,
                            metadata.Author ?? string.Empty,
                            metadata.Subject ?? string.Empty,
                            metadata.Keywords ?? string.Empty);

                        if (string.IsNullOrWhiteSpace(generatedFileName))
                        {
                            skippedInProcessCount++;
                            LogText += $"⊘ {fileItem.FileName}: {_languageService.GetString("NoMetadata")}\n";
                            fileItem.Status = STATUS_NO_METADATA;
                            fileItem.ErrorDetails = _languageService.GetString("NoMetadataError");
                        }
                        else
                        {
                            // ファイル名の各部分を個別にサニタイズ（NFKC正規化の制御付き）
                            var sanitizedFileName = SanitizeFileNameWithNFKCControl(generatedFileName);
                            var newFileName = $"{sanitizedFileName}.pdf";
                            var newFilePath = Path.Combine(Path.GetDirectoryName(fileItem.FilePath)!, newFileName);

                            if (string.Equals(fileItem.FileName, newFileName, StringComparison.OrdinalIgnoreCase))
                            {
                                skippedInProcessCount++;
                                LogText += $"⊘ {fileItem.FileName}: {_languageService.GetString("NoChangeNeeded")}\n";
                                fileItem.Status = STATUS_NO_CHANGE_NEEDED;
                                fileItem.ErrorDetails = _languageService.GetString("AlreadyProperlySet");
                            }
                            else
                            {
                                try
                                {
                                    // 重複ファイル名の処理
                                    var uniqueFilePath = _pdfProcessingService.GetUniqueFilePath(newFilePath);
                                    var finalFileName = Path.GetFileName(uniqueFilePath);
                                    
                                    File.Move(fileItem.FilePath, uniqueFilePath);
                                    
                                    successCount++;
                                    LogText += $"✓ {fileItem.FileName} → {finalFileName}\n";
                                    
                                    fileItem.NewFileName = finalFileName;
                                    fileItem.Status = STATUS_RENAME_COMPLETE;
                                    fileItem.ErrorDetails = "";
                                }
                                catch (Exception ex)
                                {
                                    errorCount++;
                                    LogText += $"✗ {fileItem.FileName}: {ex.Message}\n";
                                    fileItem.Status = STATUS_ERROR;
                                    fileItem.ErrorDetails = $"{_languageService.GetString("FileMoveError")}: {ex.Message}";
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        LogText += $"✗ {Path.GetFileName(fileItem.FilePath)}: {ex.Message}\n";
                        
                        fileItem.Status = STATUS_ERROR;
                        fileItem.ErrorDetails = $"{_languageService.GetString("ProcessingError")}: {ex.Message}";
                    }

                    processedFiles++;
                    ProgressValue = (double)processedFiles / totalFiles * 100;
                }

                LogText += $"\n{_languageService.GetString("ProcessingCompleteLog")}: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
                LogText += $"{_languageService.GetString("SuccessCountLog")}: {successCount}{_languageService.GetString("FilesUnit")}, {_languageService.GetString("SkippedCountLog")}: {skippedInProcessCount}{_languageService.GetString("FilesUnit")}, {_languageService.GetString("ErrorCountLog")}: {errorCount}{_languageService.GetString("FilesUnit")}\n";
                
                ProgressText = $"{_languageService.GetString("ProcessingCompleteLog")} - {_languageService.GetString("SuccessCountLog")}: {successCount}{_languageService.GetString("FilesUnit")}, {_languageService.GetString("SkippedCountLog")}: {skippedInProcessCount}{_languageService.GetString("FilesUnit")}, {_languageService.GetString("ErrorCountLog")}: {errorCount}{_languageService.GetString("FilesUnit")}";
            }
            catch (Exception ex)
            {
                LogText += $"\n{_languageService.GetString("ProcessingErrorLog")}: {ex.Message}\n";
                ProgressText = _languageService.GetString("ProcessingErrorOccurred");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void ClearFiles()
        {
            SelectedFiles.Clear();
            OnPropertyChanged(nameof(HasFiles));
            OnPropertyChanged(nameof(FileCountText));
        }

        private void RemoveFile(FileItem? fileItem)
        {
            if (fileItem != null)
            {
                SelectedFiles.Remove(fileItem);
                OnPropertyChanged(nameof(HasFiles));
                OnPropertyChanged(nameof(FileCountText));
            }
        }

        private void ClearLog()
        {
            _logService.ClearLog();
            LogText = "";
        }

        private void ShowAbout()
        {
            try
            {
                var aboutWindow = new PdfTitleRenamer.Views.AboutWindow();
                aboutWindow.Owner = Application.Current.MainWindow;
                aboutWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                _logService.LogError($"{_languageService.GetString("AboutWindowDisplayError")}: {ex.Message}");
            }
        }

        private void ChangeLanguage(string? languageCode)
        {
            if (!string.IsNullOrEmpty(languageCode))
            {
                _languageService.SetLanguage(languageCode);
            }
        }

        private void ShowSettings()
        {
            try
            {
                // 設定ファイルから最新の設定を読み込む
                var currentSettings = FileNameSettings.Load();
                var settingsViewModel = new SettingsWindowViewModel(currentSettings, _logService, _languageService);
                var settingsWindow = new Views.SettingsWindow(settingsViewModel);
                
                // 設定保存イベントを購読
                settingsViewModel.SettingsSaved += (s, e) => {
                    // 設定が保存された場合、設定ファイルから最新の設定を再読み込み
                    var updatedSettings = FileNameSettings.Load();
                    
                    // MainWindowViewModelの設定を更新
                    _settings.CustomPrefix = updatedSettings.CustomPrefix;
                    _settings.CustomSuffix = updatedSettings.CustomSuffix;
                    _settings.Separator = updatedSettings.Separator;
                    _settings.CurrentLanguage = updatedSettings.CurrentLanguage;
                    
                    // Elementsを更新
                    _settings.Elements.Clear();
                    foreach (var element in updatedSettings.Elements)
                    {
                        _settings.Elements.Add(new FileNameElement(element.ElementType, element.IsEnabled));
                    }
                    
                    _logService.LogInfo(_languageService.GetString("SettingsUpdatedLog"));
                    UpdateAllFilePreviews();
                    
                    // コマンドの状態を更新
                    (ProcessFilesCommand as RelayCommand)?.RaiseCanExecuteChanged();
                };
                
                // ウィンドウを閉じるイベントを購読（キャンセル時用）
                settingsViewModel.WindowClosed += (s, e) => {
                    // キャンセル時は何もしない
                };
                
                // 設定ウィンドウをモーダルで表示
                settingsWindow.Owner = Application.Current.MainWindow;
                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                _logService.LogError($"{_languageService.GetString("SettingsWindowError")}: {ex.Message}");
            }
        }

        private async Task UpdateAllFilePreviewsAsync()
        {
            foreach (var fileItem in SelectedFiles)
            {
                await SetPredictedFileNameAsync(fileItem);
            }
        }

        private async void UpdateAllFilePreviews()
        {
            await UpdateAllFilePreviewsAsync();
        }

        public ILanguageService GetLanguageService() => _languageService;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }



        private bool HasValidMetadataSettings()
        {
            // PDFメタデータ項目（Title, Author, Subject, Keywords）のいずれかが有効かチェック
            var metadataElements = new[] { "Title", "Author", "Subject", "Keywords" };
            return _settings.Elements.Any(e => e.IsEnabled && metadataElements.Contains(e.ElementType));
        }

        private string SanitizeFileNameWithNFKCControl(string fileName)
        {
            // 生成されたファイル名をセパレータで分割
            var parts = fileName.Split(new[] { _settings.Separator }, StringSplitOptions.None);
            var sanitizedParts = new List<string>();
            var enabledElements = _settings.Elements.Where(e => e.IsEnabled).ToList();

            // 各部分を個別にサニタイズ
            for (int i = 0; i < parts.Length && i < enabledElements.Count; i++)
            {
                var part = parts[i];
                var element = enabledElements[i];

                // NFKC正規化の適用制御
                bool applyNFKC = element.ElementType switch
                {
                    "OriginalFileName" => false, // 変更前のファイル名は除外
                    "CustomPrefix" => false,     // プレフィックスは除外
                    "CustomSuffix" => false,     // サフィックスは除外
                    _ => true                    // その他は適用
                };

                var sanitizedPart = _pdfProcessingService.SanitizeFileName(part, applyNFKC);
                sanitizedParts.Add(sanitizedPart);
            }

            // セパレータで結合
            return string.Join(_settings.Separator, sanitizedParts);
        }
    }
}
