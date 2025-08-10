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

            SelectedFiles = new ObservableCollection<FileItem>();
            
            // Commands
            SelectFilesCommand = new RelayCommand(SelectFiles);
            ProcessFilesCommand = new RelayCommand(ProcessFiles, () => HasFiles && !IsProcessing);
            ClearFilesCommand = new RelayCommand(ClearFiles, () => HasFiles);
            RemoveFileCommand = new RelayCommand<FileItem>(RemoveFile);
            ClearLogCommand = new RelayCommand(ClearLog);
            ShowAboutCommand = new RelayCommand(ShowAbout);

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
                var title = await Task.Run(() => _pdfProcessingService.ExtractTitleFromPdf(fileItem.FilePath));
                
                if (string.IsNullOrWhiteSpace(title))
                {
                    fileItem.NewFileName = "タイトルなし";
                    fileItem.Status = "タイトルなし";
                    fileItem.ErrorDetails = "PDFファイルにタイトル情報が設定されていません";
                    return;
                }

                var processedTitle = title.Normalize(System.Text.NormalizationForm.FormKC);
                var validTitle = _pdfProcessingService.SanitizeFileName(processedTitle);
                var predictedFileName = $"{validTitle}.pdf";
                
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
                fileItem.ErrorDetails = $"タイトル抽出エラー: {ex.Message}";
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

        private async void ProcessFiles()
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
                        
                        var result = await _pdfProcessingService.ProcessFileAsync(fileItem.FilePath);
                        
                        if (result.IsSuccess)
                        {
                            successCount++;
                            LogText += $"✓ {result.OriginalFileName} → {result.NewFileName}\n";
                            
                            fileItem.NewFileName = result.NewFileName ?? "不明";
                            fileItem.Status = "リネーム完了";
                            fileItem.ErrorDetails = "";
                        }
                        else if (result.ErrorMessage?.Contains("タイトル情報が見つかりません") == true)
                        {
                            skippedInProcessCount++;
                            LogText += $"⊘ {result.OriginalFileName}: タイトルなし\n";
                            fileItem.Status = "タイトルなし";
                            fileItem.ErrorDetails = "PDFファイルにタイトル情報が設定されていません";
                        }
                        else if (result.ErrorMessage?.Contains("既に適切なファイル名") == true)
                        {
                            skippedInProcessCount++;
                            LogText += $"⊘ {result.OriginalFileName}: 変更不要\n";
                            fileItem.Status = "変更不要";
                            fileItem.ErrorDetails = "ファイル名は既に適切に設定されています";
                        }
                        else
                        {
                            errorCount++;
                            LogText += $"✗ {result.OriginalFileName}: {result.ErrorMessage}\n";
                            fileItem.Status = "エラー";
                            fileItem.ErrorDetails = result.ErrorMessage ?? "不明なエラーが発生しました";
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
    }
}
