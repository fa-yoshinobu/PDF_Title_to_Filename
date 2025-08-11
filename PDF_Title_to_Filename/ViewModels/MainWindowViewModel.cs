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
        private readonly IPdfProcessingService _pdfProcessingService;
        private readonly ILogService _logService;
        private readonly FileNameSettings _settings;

        private string _logText = "";
        private string _progressText = "準備完了";
        private double _progressValue = 0;
        private bool _isProcessing = false;

        public MainWindowViewModel(
            IPdfProcessingService pdfProcessingService,
            ILogService logService)
        {
            _pdfProcessingService = pdfProcessingService;
            _logService = logService;
            _settings = FileNameSettings.Load();

            SelectedFiles = new ObservableCollection<FileItem>();
            
            // Commands
            SelectFilesCommand = new RelayCommand(SelectFiles);
            ProcessFilesCommand = new RelayCommand(async () => await ProcessFilesAsync(), () => HasFiles && !IsProcessing);
            ClearFilesCommand = new RelayCommand(ClearFiles, () => HasFiles);
            RemoveFileCommand = new RelayCommand<FileItem>(RemoveFile);
            ClearLogCommand = new RelayCommand(ClearLog);
            ShowAboutCommand = new RelayCommand(ShowAbout);
            ShowSettingsCommand = new RelayCommand(ShowSettings);

            // Subscribe to property changes to update command states
            PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(HasFiles) || e.PropertyName == nameof(IsProcessing))
                {
                    (ProcessFilesCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (ClearFilesCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
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
            get => _progressText;
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

        public string FileCountText => $"{SelectedFiles.Count} ファイル選択済み";

        // Commands
        public ICommand SelectFilesCommand { get; }
        public ICommand ProcessFilesCommand { get; }
        public ICommand ClearFilesCommand { get; }
        public ICommand RemoveFileCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand ShowAboutCommand { get; }
        public ICommand ShowSettingsCommand { get; }

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
                        
                        _ = Task.Run(async () => await SetPredictedFileNameAsync(fileItem));
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
                
                // デバッグ用ログ
                _logService.LogInfo($"PDFメタデータ: Title='{metadata.Title}', Author='{metadata.Author}', Subject='{metadata.Subject}', Keywords='{metadata.Keywords}'");
                
                // 取得対象として設定されているメタデータがすべて空の場合は「メタデータなし」として処理
                var hasMetadata = false;
                
                // 有効なメタデータ要素をチェック
                foreach (var element in _settings.Elements.Where(e => e.IsEnabled))
                {
                    switch (element.ElementType)
                    {
                        case "Title":
                            if (!string.IsNullOrWhiteSpace(metadata.Title))
                            {
                                hasMetadata = true;
                                break;
                            }
                            break;
                        case "Author":
                            if (!string.IsNullOrWhiteSpace(metadata.Author))
                            {
                                hasMetadata = true;
                                break;
                            }
                            break;
                        case "Subject":
                            if (!string.IsNullOrWhiteSpace(metadata.Subject))
                            {
                                hasMetadata = true;
                                break;
                            }
                            break;
                        case "Keywords":
                            if (!string.IsNullOrWhiteSpace(metadata.Keywords))
                            {
                                hasMetadata = true;
                                break;
                            }
                            break;
                    }
                    
                    // メタデータが見つかったらループを抜ける
                    if (hasMetadata) break;
                }
                
                if (!hasMetadata)
                {
                    fileItem.NewFileName = "メタデータなし";
                    fileItem.Status = "メタデータなし";
                    fileItem.ErrorDetails = "PDFファイルにメタデータ情報が設定されていません";
                    return;
                }
                
                // 設定に基づいてファイル名を生成
                var generatedFileName = _settings.GenerateFileName(
                    fileItem.FileName,
                    metadata.Title,
                    metadata.Author,
                    metadata.Subject,
                    metadata.Keywords);

                _logService.LogInfo($"生成されたファイル名: '{generatedFileName}'");

                // 生成されたファイル名が空または無効な場合
                if (string.IsNullOrWhiteSpace(generatedFileName))
                {
                    fileItem.NewFileName = "タイトルなし";
                    fileItem.Status = "タイトルなし";
                    fileItem.ErrorDetails = "PDFファイルにメタデータ情報が設定されていません";
                    return;
                }

                // ファイル名をサニタイズ（NFKC正規化制御付き）
                var validFileName = SanitizeFileNameWithNFKCControl(generatedFileName);
                var predictedFileName = $"{validFileName}.pdf";
                
                fileItem.NewFileName = predictedFileName;
                
                // 元のファイル名と予想ファイル名を比較
                if (string.Equals(fileItem.FileName, predictedFileName, StringComparison.OrdinalIgnoreCase))
                {
                    fileItem.Status = "変更不要";
                    fileItem.ErrorDetails = "ファイル名は既に適切に設定されています";
                }
                else
                {
                    fileItem.Status = "リネーム予定";
                }
            }
            catch (Exception ex)
            {
                fileItem.NewFileName = "エラー";
                fileItem.Status = "エラー";
                fileItem.ErrorDetails = $"メタデータ抽出エラー: {ex.Message}";
            }
        }

        private void SelectFiles()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "PDFファイル (*.pdf)|*.pdf",
                Multiselect = true,
                Title = "処理するPDFファイルを選択してください"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                AddFiles(openFileDialog.FileNames);
            }
        }

        private async Task ProcessFilesAsync()
        {
            if (!HasFiles || IsProcessing) return;

            IsProcessing = true;
            ProgressValue = 0;
            ProgressText = "処理中...";

            try
            {
                var allFiles = SelectedFiles.ToList();
                var filesToProcess = allFiles.Where(f => 
                    f.Status != "リネーム完了" && 
                    f.Status != "タイトルなし" && 
                    f.Status != "変更不要").ToList();
                var skippedCount = allFiles.Count - filesToProcess.Count;
                var totalFiles = filesToProcess.Count;
                var processedFiles = 0;
                var successCount = 0;
                var skippedInProcessCount = 0;
                var errorCount = 0;

                LogText = $"処理開始: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
                LogText += $"対象ファイル数: {totalFiles}";
                if (skippedCount > 0)
                {
                    LogText += $" (スキップ: {skippedCount}件)";
                }
                LogText += "\n\n";

                foreach (var fileItem in filesToProcess)
                {
                    try
                    {
                        var currentFile = processedFiles + 1;
                        ProgressText = $"処理中... ({currentFile}/{totalFiles}) {Path.GetFileName(fileItem.FilePath)}";
                        fileItem.Status = "処理中...";
                        
                        // 新しい設定システムを使用してファイル名を生成
                        var metadata = await Task.Run(() => _pdfProcessingService.ExtractMetadataFromPdf(fileItem.FilePath));
                        
                        // 取得対象として設定されているメタデータがすべて空の場合は「メタデータなし」として処理
                        var hasMetadata = false;
                        
                        // 有効なメタデータ要素をチェック
                        foreach (var element in _settings.Elements.Where(e => e.IsEnabled))
                        {
                            switch (element.ElementType)
                            {
                                case "Title":
                                    if (!string.IsNullOrWhiteSpace(metadata.Title))
                                    {
                                        hasMetadata = true;
                                        break;
                                    }
                                    break;
                                case "Author":
                                    if (!string.IsNullOrWhiteSpace(metadata.Author))
                                    {
                                        hasMetadata = true;
                                        break;
                                    }
                                    break;
                                case "Subject":
                                    if (!string.IsNullOrWhiteSpace(metadata.Subject))
                                    {
                                        hasMetadata = true;
                                        break;
                                    }
                                    break;
                                case "Keywords":
                                    if (!string.IsNullOrWhiteSpace(metadata.Keywords))
                                    {
                                        hasMetadata = true;
                                        break;
                                    }
                                    break;
                            }
                            
                            // メタデータが見つかったらループを抜ける
                            if (hasMetadata) break;
                        }
                        
                        if (!hasMetadata)
                        {
                            skippedInProcessCount++;
                            LogText += $"⊘ {fileItem.FileName}: メタデータなし\n";
                            fileItem.Status = "メタデータなし";
                            fileItem.ErrorDetails = "PDFファイルにメタデータ情報が設定されていません";
                        }
                        else
                        {
                            var generatedFileName = _settings.GenerateFileName(
                                fileItem.FileName,
                                metadata.Title,
                                metadata.Author,
                                metadata.Subject,
                                metadata.Keywords);

                            if (string.IsNullOrWhiteSpace(generatedFileName))
                            {
                                skippedInProcessCount++;
                                LogText += $"⊘ {fileItem.FileName}: タイトルなし\n";
                                fileItem.Status = "タイトルなし";
                                fileItem.ErrorDetails = "PDFファイルにメタデータ情報が設定されていません";
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
                                    LogText += $"⊘ {fileItem.FileName}: 変更不要\n";
                                    fileItem.Status = "変更不要";
                                    fileItem.ErrorDetails = "ファイル名は既に適切に設定されています";
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
                                        fileItem.Status = "リネーム完了";
                                        fileItem.ErrorDetails = "";
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        LogText += $"✗ {fileItem.FileName}: {ex.Message}\n";
                                        fileItem.Status = "エラー";
                                        fileItem.ErrorDetails = $"ファイル移動エラー: {ex.Message}";
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        LogText += $"✗ {Path.GetFileName(fileItem.FilePath)}: {ex.Message}\n";
                        
                        fileItem.Status = "エラー";
                        fileItem.ErrorDetails = $"処理中にエラーが発生しました: {ex.Message}";
                    }

                    processedFiles++;
                    ProgressValue = (double)processedFiles / totalFiles * 100;
                }

                LogText += $"\n処理完了: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
                LogText += $"成功: {successCount}件, スキップ: {skippedInProcessCount}件, エラー: {errorCount}件\n";
                
                ProgressText = $"処理完了 - 成功: {successCount}件, スキップ: {skippedInProcessCount}件, エラー: {errorCount}件";
            }
            catch (Exception ex)
            {
                LogText += $"\n処理エラー: {ex.Message}\n";
                ProgressText = "処理エラーが発生しました";
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
            LogText = "";
        }

        private void ShowAbout()
        {
            var aboutWindow = new PdfTitleRenamer.Views.AboutWindow();
            aboutWindow.ShowDialog();
        }

        private void ShowSettings()
        {
            try
            {
                // 設定を再読み込みして最新の状態にする
                var currentSettings = FileNameSettings.Load();
                var settingsViewModel = new SettingsWindowViewModel(currentSettings, _logService);
                var settingsWindow = new Views.SettingsWindow(settingsViewModel);
                
                // 設定ウィンドウをモーダルで表示
                settingsWindow.Owner = Application.Current.MainWindow;
                var result = settingsWindow.ShowDialog();
                
                if (result == true)
                {
                    // 設定が保存された場合、MainWindowViewModelの設定を更新
                    _settings.CustomPrefix = currentSettings.CustomPrefix;
                    _settings.CustomSuffix = currentSettings.CustomSuffix;
                    _settings.Separator = currentSettings.Separator;
                    
                    // Elementsを更新
                    _settings.Elements.Clear();
                    foreach (var element in currentSettings.Elements)
                    {
                        _settings.Elements.Add(new FileNameElement(element.ElementType, element.IsEnabled));
                    }
                    
                    _logService.LogInfo("設定が更新されました。プレビューを更新します。");
                    UpdateAllFilePreviews();
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"設定ウィンドウの表示に失敗しました: {ex.Message}");
            }
        }

        private async void UpdateAllFilePreviews()
        {
            foreach (var fileItem in SelectedFiles)
            {
                await SetPredictedFileNameAsync(fileItem);
            }
        }

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
