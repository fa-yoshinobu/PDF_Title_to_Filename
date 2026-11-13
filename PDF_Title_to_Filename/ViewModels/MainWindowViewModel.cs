// ログ出力の制御
#if DEBUG
#define ENABLE_DETAILED_LOGGING
#endif

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using System.IO;
using System.Text;
using Microsoft.Win32;
using PdfTitleRenamer.Models;
using PdfTitleRenamer.Services;
using System.Windows;

namespace PdfTitleRenamer.ViewModels
{
    internal class MainWindowViewModel : INotifyPropertyChanged
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

        // 絞り込みの種類
        private const string FILTER_ALL = "All";
        private const string FILTER_SCHEDULED = "Scheduled";
        private const string FILTER_NO_METADATA = "NoMetadata";
        private const string FILTER_ERROR = "Error";
        private const string FILTER_DONE = "Done";

        // 拡張子を除いたファイル名の最大文字数
        private const int MaxFileNameLength = 240;

        private readonly IPdfProcessingService _pdfProcessingService;
        private readonly ILogService _logService;
        private readonly ILanguageService _languageService;
        private readonly FileNameSettings _settings;

        private string _logText = "";
        private string _progressText = "";
        private double _progressValue;
        private bool _isProcessing;
        private string _statusFilter = FILTER_ALL;
        private bool _refreshQueued;

        // 直前のリネーム（元に戻す用）
        private List<RenameRecord> _lastRenameBatch = new();

        private sealed record RenameRecord(FileItem Item, string OriginalPath, string RenamedPath);

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
            _languageService.LanguageChanged += (s, e) =>
            {
                OnPropertyChanged(string.Empty);
                // FileItemのStatusDisplayも更新
                foreach (var fileItem in SelectedFiles)
                {
                    fileItem.UpdateStatusDisplay();
                }
            };
#pragma warning restore CS8602

            // ログサービスのイベントを購読
            _logService.LogAdded += (s, e) =>
            {
                LogText = e;
            };

            // FileNameElementにLanguageServiceを設定
            FileNameElement.SetLanguageService(_languageService);

            SelectedFiles = new ObservableCollection<FileItem>();
            SelectedFiles.CollectionChanged += OnSelectedFilesChanged;

            // 確認ダイアログ（テストでは差し替える）
            ConfirmAction = (message, title) =>
                MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

            // Commands
            SelectFilesCommand = new RelayCommand(SelectFiles, () => !IsProcessing);
            ProcessFilesCommand = new RelayCommand(async () => await ProcessFilesAsync(), () => HasFiles && !IsProcessing && HasValidMetadataSettings());
            ClearFilesCommand = new RelayCommand(ClearFiles, () => HasFiles && !IsProcessing);
            RemoveFileCommand = new RelayCommand<FileItem>(RemoveFile, _ => !IsProcessing);
            RemoveNoMetadataFilesCommand = new RelayCommand(RemoveNoMetadataFiles, () => !IsProcessing && SelectedFiles.Any(f => !f.HasMetadata));
            RemoveSelectedCommand = new RelayCommand<object>(RemoveSelected, p => !IsProcessing && ToFileItems(p).Count > 0);
            OpenFileCommand = new RelayCommand<object>(OpenFile, p => ToFileItems(p).Count > 0);
            OpenFolderCommand = new RelayCommand<object>(OpenFolder, p => ToFileItems(p).Count > 0);
            CopyNewNameCommand = new RelayCommand<object>(CopyNewName, p => ToFileItems(p).Count > 0);
            RefreshPreviewCommand = new RelayCommand(UpdateAllFilePreviews, () => HasFiles && !IsProcessing);
            UndoCommand = new RelayCommand(UndoLastRename, () => CanUndo);
            ClearLogCommand = new RelayCommand(ClearLog);
            ShowAboutCommand = new RelayCommand(ShowAbout);
            ShowSettingsCommand = new RelayCommand(ShowSettings, () => !IsProcessing);
            ChangeLanguageCommand = new RelayCommand<string>(ChangeLanguage);

            // Subscribe to property changes to update command states
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(HasFiles) || e.PropertyName == nameof(IsProcessing))
                {
                    RaiseAllCanExecuteChanged();
                }
            };

            // 設定変更時にコマンドの状態を更新
            _settings.Elements.CollectionChanged += (s, e) =>
            {
                (ProcessFilesCommand as RelayCommand)?.RaiseCanExecuteChanged();
            };
        }

        public ObservableCollection<FileItem> SelectedFiles { get; }

        /// <summary>はい/いいえの確認。message, title を受け取り、はいなら true</summary>
        internal Func<string, string, bool> ConfirmAction { get; set; }

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

        public bool CanUndo => !IsProcessing && _lastRenameBatch.Count > 0;

        public string FileCountText => $"{SelectedFiles.Count} {_languageService?.GetString("FilesSelected") ?? "Files Selected"}";

        // UI文字列プロパティ
        public string FileSelectionText => _languageService.GetString("FileSelection");
        public string FileSelectionToolTip => _languageService.GetString("FileSelectionToolTip");
        public string ProcessStartText => _languageService.GetString("ProcessStart");
        public string ProcessStartToolTip => _languageService.GetString("ProcessStartToolTip");
        public string ClearListText => _languageService.GetString("ClearList");
        public string ClearListToolTip => _languageService.GetString("ClearListToolTip");
        public string RemoveNoMetadataText => _languageService.GetString("RemoveNoMetadata");
        public string RemoveNoMetadataToolTip => _languageService.GetString("RemoveNoMetadataToolTip");
        public string UndoText => _languageService.GetString("UndoText");
        public string UndoToolTip => _languageService.GetString("UndoToolTip");
        public string CurrentNameText => _languageService.GetString("CurrentName");
        public string NewNameText => _languageService.GetString("NewName");
        public string StatusText => _languageService.GetString("Status");
        public string OperationText => _languageService.GetString("Operation");
        public string ProcessingLogText => _languageService.GetString("ProcessingResults");
        public string DragDropFilesText => _languageService.GetString("DragDropFiles");
        public string DragDropSubText => _languageService.GetString("DragDropSubText");
        public string DragDropOverlayText => _languageService.GetString("DragDropOverlay");
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
        public string NewNameEditToolTip => _languageService.GetString("NewNameEditToolTip");
        public string ManuallyEditedText => _languageService.GetString("ManuallyEditedNote");
        public string ContextOpenFileText => _languageService.GetString("ContextOpenFile");
        public string ContextOpenFolderText => _languageService.GetString("ContextOpenFolder");
        public string ContextCopyNewNameText => _languageService.GetString("ContextCopyNewName");
        public string ContextRemoveFromListText => _languageService.GetString("ContextRemoveFromList");

        // 現在の言語を表示するプロパティ
        public string CurrentLanguageDisplay => _languageService.CurrentLanguage == "ja" ? "Japanese" : "English";

        // 言語ボタン: 現在の言語をその言語の表記で表示し、押すともう一方へ切り替える
        public string LanguageButtonText => _languageService.CurrentLanguage == "ja" ? "日本語" : "English";
        public string NextLanguageCode => _languageService.CurrentLanguage == "ja" ? "en" : "ja";

        // Status strings for DataGrid triggers
        public string Error => _languageService.GetString("Error");
        public string NoTitle => _languageService.GetString("NoTitle");
        public string NoMetadata => _languageService.GetString("NoMetadata");
        public string RenameComplete => _languageService.GetString("RenameComplete");
        public string RenameScheduled => _languageService.GetString("RenameScheduled");
        public string Processing => _languageService.GetString("Processing");
        public string NoChangeNeeded => _languageService.GetString("NoChangeNeeded");
        public string Waiting => _languageService.GetString("Waiting");

        #region ステータス別の件数と絞り込み

        public int ScheduledCount => SelectedFiles.Count(f => MatchesFilter(f, FILTER_SCHEDULED));
        public int NoMetadataCount => SelectedFiles.Count(f => MatchesFilter(f, FILTER_NO_METADATA));
        public int ErrorCount => SelectedFiles.Count(f => MatchesFilter(f, FILTER_ERROR));
        public int DoneCount => SelectedFiles.Count(f => MatchesFilter(f, FILTER_DONE));

        public string FilterAllText => $"{_languageService.GetString("FilterAll")} ({SelectedFiles.Count})";
        public string FilterScheduledText => $"{_languageService.GetString("FilterScheduled")} ({ScheduledCount})";
        public string FilterNoMetadataText => $"{_languageService.GetString("FilterNoMetadata")} ({NoMetadataCount})";
        public string FilterErrorText => $"{_languageService.GetString("FilterError")} ({ErrorCount})";
        public string FilterDoneText => $"{_languageService.GetString("FilterDone")} ({DoneCount})";

        public bool IsFilterAll
        {
            get => _statusFilter == FILTER_ALL;
            set { if (value) SetStatusFilter(FILTER_ALL); }
        }

        public bool IsFilterScheduled
        {
            get => _statusFilter == FILTER_SCHEDULED;
            set { if (value) SetStatusFilter(FILTER_SCHEDULED); }
        }

        public bool IsFilterNoMetadata
        {
            get => _statusFilter == FILTER_NO_METADATA;
            set { if (value) SetStatusFilter(FILTER_NO_METADATA); }
        }

        public bool IsFilterError
        {
            get => _statusFilter == FILTER_ERROR;
            set { if (value) SetStatusFilter(FILTER_ERROR); }
        }

        public bool IsFilterDone
        {
            get => _statusFilter == FILTER_DONE;
            set { if (value) SetStatusFilter(FILTER_DONE); }
        }

        internal string StatusFilter => _statusFilter;

        private static bool MatchesFilter(FileItem item, string filter)
        {
            return filter switch
            {
                FILTER_SCHEDULED => item.Status == STATUS_RENAME_SCHEDULED,
                FILTER_NO_METADATA => item.Status == STATUS_NO_METADATA || item.Status == STATUS_NO_TITLE,
                FILTER_ERROR => item.Status == STATUS_ERROR,
                FILTER_DONE => item.Status == STATUS_RENAME_COMPLETE || item.Status == STATUS_NO_CHANGE_NEEDED,
                _ => true
            };
        }

        private void SetStatusFilter(string filter)
        {
            if (_statusFilter == filter) return;

            _statusFilter = filter;
            OnPropertyChanged(nameof(IsFilterAll));
            OnPropertyChanged(nameof(IsFilterScheduled));
            OnPropertyChanged(nameof(IsFilterNoMetadata));
            OnPropertyChanged(nameof(IsFilterError));
            OnPropertyChanged(nameof(IsFilterDone));

            var view = CollectionViewSource.GetDefaultView(SelectedFiles);
            if (view != null)
            {
                view.Filter = filter == FILTER_ALL ? null : o => o is FileItem item && MatchesFilter(item, _statusFilter);
            }
        }

        private void RaiseCountsChanged()
        {
            OnPropertyChanged(nameof(FilterAllText));
            OnPropertyChanged(nameof(FilterScheduledText));
            OnPropertyChanged(nameof(FilterNoMetadataText));
            OnPropertyChanged(nameof(FilterErrorText));
            OnPropertyChanged(nameof(FilterDoneText));
        }

        /// <summary>
        /// 絞り込み中にステータスが変わった行を出し入れする。
        /// 編集中に Refresh すると例外になるため、UIスレッドで後回しにして編集中なら見送る。
        /// </summary>
        private void QueueFilterRefresh()
        {
            if (_statusFilter == FILTER_ALL || _refreshQueued) return;

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null) return;

            _refreshQueued = true;
            _ = dispatcher.InvokeAsync(() =>
            {
                _refreshQueued = false;
                var view = CollectionViewSource.GetDefaultView(SelectedFiles);
                if (view is IEditableCollectionView editable && (editable.IsEditingItem || editable.IsAddingNew))
                {
                    return;
                }
                view?.Refresh();
            });
        }

        private void OnSelectedFilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (FileItem item in e.OldItems)
                {
                    item.PropertyChanged -= OnFileItemPropertyChanged;
                    item.ManualNameEdited -= OnManualNameEdited;
                }
            }

            if (e.NewItems != null)
            {
                foreach (FileItem item in e.NewItems)
                {
                    item.PropertyChanged += OnFileItemPropertyChanged;
                    item.ManualNameEdited += OnManualNameEdited;
                }
            }

            // リストが空になったら絞り込みを解除する（次に追加したファイルが見えなくならないように）
            if (SelectedFiles.Count == 0 && _statusFilter != FILTER_ALL)
            {
                SetStatusFilter(FILTER_ALL);
            }

            OnPropertyChanged(nameof(HasFiles));
            OnPropertyChanged(nameof(FileCountText));
            RaiseCountsChanged();
            RaiseAllCanExecuteChanged();
        }

        private void OnFileItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FileItem.Status))
            {
                RaiseCountsChanged();
                QueueFilterRefresh();
            }
        }

        #endregion

        // Commands
        public ICommand SelectFilesCommand { get; }
        public ICommand ProcessFilesCommand { get; }
        public ICommand ClearFilesCommand { get; }
        public ICommand RemoveFileCommand { get; }
        public ICommand RemoveNoMetadataFilesCommand { get; }
        public ICommand RemoveSelectedCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand CopyNewNameCommand { get; }
        public ICommand RefreshPreviewCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand ShowAboutCommand { get; }
        public ICommand ShowSettingsCommand { get; }
        public ICommand ChangeLanguageCommand { get; }

        private void RaiseAllCanExecuteChanged()
        {
            foreach (var command in new[]
            {
                SelectFilesCommand, ProcessFilesCommand, ClearFilesCommand, RemoveNoMetadataFilesCommand,
                RefreshPreviewCommand, UndoCommand, ShowSettingsCommand
            })
            {
                (command as RelayCommand)?.RaiseCanExecuteChanged();
            }

            foreach (var command in new[] { RemoveFileCommand })
            {
                (command as RelayCommand<FileItem>)?.RaiseCanExecuteChanged();
            }

            foreach (var command in new[] { RemoveSelectedCommand, OpenFileCommand, OpenFolderCommand, CopyNewNameCommand })
            {
                (command as RelayCommand<object>)?.RaiseCanExecuteChanged();
            }

            OnPropertyChanged(nameof(CanUndo));
        }

        public void AddFiles(string[] filePaths)
        {
            if (IsProcessing) return;

            foreach (var filePath in filePaths)
            {
                if (File.Exists(filePath) && filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    // Windowsのパスは大文字小文字を区別しないため、同じファイルの二重登録を防ぐ
                    if (!SelectedFiles.Any(f => string.Equals(f.CurrentFilePath, filePath, StringComparison.OrdinalIgnoreCase)))
                    {
                        var fileItem = new FileItem(filePath);
                        SelectedFiles.Add(fileItem);

                        _ = SetPredictedFileNameAsync(fileItem);
                    }
                }
            }
        }

        private async Task SetPredictedFileNameAsync(FileItem fileItem)
        {
            // 手修正した名前は自動生成で上書きしない
            if (fileItem.IsManuallyEdited) return;

            try
            {
                var metadata = await Task.Run(() => _pdfProcessingService.ExtractMetadataFromPdf(fileItem.FilePath));

                // 読み取り中に手修正された場合は結果を捨てる
                if (fileItem.IsManuallyEdited) return;

#if ENABLE_DETAILED_LOGGING
                _logService.LogInfo($"{_languageService.GetString("PDFMetadataLog")}: Title='{metadata.Title}', Author='{metadata.Author}', Subject='{metadata.Subject}', Keywords='{metadata.Keywords}'");
                _logService.LogInfo($"Enabled elements: {string.Join(", ", _settings.Elements.Where(e => e.IsEnabled).Select(e => e.ElementType))}");
#endif

                // 設定に基づいてファイル名を生成（メタデータチェックも含む）
                var generatedParts = _settings.GenerateFileNameParts(
                    fileItem.FileName,
                    metadata.Title ?? string.Empty,
                    metadata.Author ?? string.Empty,
                    metadata.Subject ?? string.Empty,
                    metadata.Keywords ?? string.Empty);
                var generatedFileName = JoinFileNameParts(generatedParts);
                fileItem.HasMetadata = HasUsableMetadata(metadata);

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
                var validFileName = SanitizeFileNameWithNFKCControl(generatedParts);
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
                    fileItem.ErrorDetails = "";
                }
            }
            catch (Exception ex)
            {
                fileItem.NewFileName = "Error";
                fileItem.Status = STATUS_ERROR;
                fileItem.ErrorDetails = $"{_languageService.GetString("MetadataExtractionError")}: {ex.Message}";
            }
            finally
            {
                (RemoveNoMetadataFilesCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        #region 名前の手修正

        private void OnManualNameEdited(object? sender, string text)
        {
            if (sender is FileItem fileItem)
            {
                ApplyManualName(fileItem, text);
            }
        }

        /// <summary>
        /// 一覧で入力された名前を反映する。空にした場合は自動生成の名前に戻す。
        /// </summary>
        internal void ApplyManualName(FileItem fileItem, string? text)
        {
            if (IsProcessing || !fileItem.CanEditName) return;

            var name = (text ?? string.Empty).Trim();
            if (name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                name = name[..^4].TrimEnd();
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                fileItem.IsManuallyEdited = false;
                _ = SetPredictedFileNameAsync(fileItem);
                return;
            }

            // 手入力の名前はユーザーの意図どおりにするため NFKC 正規化はしない
            var sanitized = _pdfProcessingService.SanitizeFileName(name, applyNFKC: false);
            var newFileName = $"{sanitized}.pdf";

            fileItem.IsManuallyEdited = true;
            fileItem.NewFileName = newFileName;

            if (string.Equals(fileItem.FileName, newFileName, StringComparison.OrdinalIgnoreCase))
            {
                fileItem.Status = STATUS_NO_CHANGE_NEEDED;
                fileItem.ErrorDetails = _languageService.GetString("AlreadyProperlySet");
            }
            else
            {
                fileItem.Status = STATUS_RENAME_SCHEDULED;
                fileItem.ErrorDetails = _languageService.GetString("ManuallyEditedNote");
            }
        }

        #endregion

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

            // 状態比較は固定文字列を使用（言語に依存しない）
            var filesToProcess = SelectedFiles.Where(f =>
                f.Status != STATUS_RENAME_COMPLETE &&
                f.Status != STATUS_NO_TITLE &&
                f.Status != STATUS_NO_CHANGE_NEEDED).ToList();
            var skippedCount = SelectedFiles.Count - filesToProcess.Count;

            // メタデータなしと判明しているものはリネームされないので確認の件数から除く
            var renameCandidateCount = filesToProcess.Count(f => f.Status != STATUS_NO_METADATA);
            if (renameCandidateCount == 0)
            {
                ProgressText = _languageService.GetString("NothingToProcess");
                return;
            }

            // リネーム前の確認
            if (!ConfirmAction(
                    string.Format(CultureInfo.CurrentCulture, _languageService.GetString("ConfirmRenameMessage"), renameCandidateCount),
                    _languageService.GetString("ConfirmRenameTitle")))
            {
                return;
            }

            IsProcessing = true;
            ProgressValue = 0;
            ProgressText = _languageService.GetString("Processing");
            var logBuilder = new StringBuilder();
            var renameBatch = new List<RenameRecord>();

            void AppendLog(string value)
            {
                logBuilder.Append(value);
                LogText = logBuilder.ToString();
            }

            try
            {
                var totalFiles = filesToProcess.Count;
                var processedFiles = 0;
                var successCount = 0;
                var skippedInProcessCount = 0;
                var errorCount = 0;

                AppendLog($"{_languageService.GetString("ProcessingStartLog")}: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
                AppendLog($"{_languageService.GetString("TargetFileCountLog")}: {totalFiles}");
                if (skippedCount > 0)
                {
                    AppendLog($" ({_languageService.GetString("SkippedCountLog")}: {skippedCount}{_languageService.GetString("FilesUnit")})");
                }
                AppendLog("\n\n");

                foreach (var fileItem in filesToProcess)
                {
                    try
                    {
                        var currentFile = processedFiles + 1;
                        ProgressText = $"{_languageService.GetString("Processing")} ({currentFile}/{totalFiles}) {fileItem.FileName}";
                        fileItem.Status = STATUS_PROCESSING;

                        string? newFileName;

                        if (fileItem.IsManuallyEdited)
                        {
                            // 一覧で手修正された名前をそのまま使う（ApplyManualName でサニタイズ済み）
                            newFileName = fileItem.NewFileName;
                        }
                        else
                        {
                            var metadata = await Task.Run(() => _pdfProcessingService.ExtractMetadataFromPdf(fileItem.FilePath));

                            // 設定に基づいてファイル名を生成（メタデータチェックも含む）
                            var generatedParts = _settings.GenerateFileNameParts(
                                fileItem.FileName,
                                metadata.Title ?? string.Empty,
                                metadata.Author ?? string.Empty,
                                metadata.Subject ?? string.Empty,
                                metadata.Keywords ?? string.Empty);
                            fileItem.HasMetadata = HasUsableMetadata(metadata);

                            newFileName = string.IsNullOrWhiteSpace(JoinFileNameParts(generatedParts))
                                ? null
                                : $"{SanitizeFileNameWithNFKCControl(generatedParts)}.pdf";
                        }

                        if (newFileName == null)
                        {
                            skippedInProcessCount++;
                            AppendLog($"⊘ {fileItem.FileName}: {_languageService.GetString("NoMetadata")}\n");
                            fileItem.NewFileName = "NoMetadata";
                            fileItem.Status = STATUS_NO_METADATA;
                            fileItem.ErrorDetails = _languageService.GetString("NoMetadataError");
                        }
                        else if (string.Equals(fileItem.FileName, newFileName, StringComparison.OrdinalIgnoreCase))
                        {
                            skippedInProcessCount++;
                            AppendLog($"⊘ {fileItem.FileName}: {_languageService.GetString("NoChangeNeeded")}\n");
                            fileItem.NewFileName = newFileName;
                            fileItem.Status = STATUS_NO_CHANGE_NEEDED;
                            fileItem.ErrorDetails = _languageService.GetString("AlreadyProperlySet");
                        }
                        else
                        {
                            try
                            {
                                var newFilePath = Path.Combine(Path.GetDirectoryName(fileItem.FilePath)!, newFileName);

                                // 重複ファイル名の処理
                                var uniqueFilePath = _pdfProcessingService.GetUniqueFilePath(newFilePath);
                                var finalFileName = Path.GetFileName(uniqueFilePath);

                                File.Move(fileItem.FilePath, uniqueFilePath);
                                renameBatch.Add(new RenameRecord(fileItem, fileItem.FilePath, uniqueFilePath));

                                successCount++;
                                AppendLog($"✓ {fileItem.FileName} → {finalFileName}\n");

                                fileItem.RenamedFilePath = uniqueFilePath;
                                fileItem.NewFileName = finalFileName;
                                fileItem.Status = STATUS_RENAME_COMPLETE;
                                fileItem.ErrorDetails = "";
                            }
                            catch (Exception ex)
                            {
                                errorCount++;
                                AppendLog($"✗ {fileItem.FileName}: {ex.Message}\n");
                                fileItem.Status = STATUS_ERROR;
                                fileItem.ErrorDetails = $"{_languageService.GetString("FileMoveError")}: {ex.Message}";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        AppendLog($"✗ {fileItem.FileName}: {ex.Message}\n");

                        fileItem.Status = STATUS_ERROR;
                        fileItem.ErrorDetails = $"{_languageService.GetString("ProcessingError")}: {ex.Message}";
                    }

                    processedFiles++;
                    ProgressValue = (double)processedFiles / totalFiles * 100;
                }

                var summary = $"{_languageService.GetString("SuccessCountLog")}: {successCount}{_languageService.GetString("FilesUnit")}, {_languageService.GetString("SkippedCountLog")}: {skippedInProcessCount}{_languageService.GetString("FilesUnit")}, {_languageService.GetString("ErrorCountLog")}: {errorCount}{_languageService.GetString("FilesUnit")}";
                AppendLog($"\n{_languageService.GetString("ProcessingCompleteLog")}: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
                AppendLog($"{summary}\n");

                ProgressText = $"{_languageService.GetString("ProcessingCompleteLog")} - {summary}";
            }
            catch (Exception ex)
            {
                AppendLog($"\n{_languageService.GetString("ProcessingErrorLog")}: {ex.Message}\n");
                ProgressText = _languageService.GetString("ProcessingErrorOccurred");
            }
            finally
            {
                // 1件でもリネームしていれば、その回を「元に戻す」の対象にする
                if (renameBatch.Count > 0)
                {
                    _lastRenameBatch = renameBatch;
                }

                IsProcessing = false;
                RaiseAllCanExecuteChanged();
            }
        }

        #region 元に戻す

        /// <summary>
        /// 直前の処理でリネームしたファイルを元の名前に戻す。
        /// </summary>
        private void UndoLastRename()
        {
            if (!CanUndo) return;

            var batch = _lastRenameBatch;
            if (!ConfirmAction(
                    string.Format(CultureInfo.CurrentCulture, _languageService.GetString("ConfirmUndoMessage"), batch.Count),
                    _languageService.GetString("ConfirmUndoTitle")))
            {
                return;
            }

            var restored = 0;
            var failed = 0;

            // 連番付与などの依存があり得るため、リネームと逆順で戻す
            foreach (var record in Enumerable.Reverse(batch))
            {
                try
                {
                    if (!File.Exists(record.RenamedPath))
                    {
                        throw new FileNotFoundException(_languageService.GetString("UndoSourceMissing"), record.RenamedPath);
                    }

                    if (File.Exists(record.OriginalPath))
                    {
                        throw new IOException(_languageService.GetString("UndoTargetExists"));
                    }

                    File.Move(record.RenamedPath, record.OriginalPath);
                    restored++;
                    _logService.LogInfo($"{_languageService.GetString("UndoCompleteLog")}: {Path.GetFileName(record.RenamedPath)} → {Path.GetFileName(record.OriginalPath)}");

                    var item = record.Item;
                    item.RenamedFilePath = null;

                    if (item.IsManuallyEdited)
                    {
                        // 手修正した名前は残し、もう一度リネームできる状態にする
                        item.NewFileName = Path.GetFileName(record.RenamedPath);
                        item.Status = STATUS_RENAME_SCHEDULED;
                        item.ErrorDetails = _languageService.GetString("ManuallyEditedNote");
                    }
                    else
                    {
                        item.NewFileName = "ProcessingPending";
                        item.Status = STATUS_WAITING;
                        item.ErrorDetails = "";
                        if (SelectedFiles.Contains(item))
                        {
                            _ = SetPredictedFileNameAsync(item);
                        }
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    failed++;
                    _logService.LogError($"{_languageService.GetString("UndoFailedLog")}: {Path.GetFileName(record.RenamedPath)} - {ex.Message}");
                }
            }

            _lastRenameBatch = new List<RenameRecord>();
            ProgressText = string.Format(CultureInfo.CurrentCulture, _languageService.GetString("UndoSummary"), restored, failed);
            RaiseAllCanExecuteChanged();
        }

        #endregion

        private void ClearFiles()
        {
            if (IsProcessing) return;
            SelectedFiles.Clear();
        }

        private void RemoveFile(FileItem? fileItem)
        {
            if (fileItem != null && !IsProcessing)
            {
                SelectedFiles.Remove(fileItem);
            }
        }

        /// <summary>
        /// メタデータのないPDFをリストから除外する（ディスク上のファイルは変更しない）
        /// </summary>
        private void RemoveNoMetadataFiles()
        {
            if (IsProcessing) return;

            var targets = SelectedFiles.Where(f => !f.HasMetadata).ToList();
            if (targets.Count == 0) return;

            foreach (var fileItem in targets)
            {
                SelectedFiles.Remove(fileItem);
            }

            _logService.LogInfo($"{_languageService.GetString("RemovedNoMetadataLog")}: {targets.Count}{_languageService.GetString("FilesUnit")}");
        }

        #region 選択行の操作（右クリックメニュー・Deleteキー）

        private static List<FileItem> ToFileItems(object? parameter)
        {
            return parameter switch
            {
                FileItem item => new List<FileItem> { item },
                IEnumerable items => items.OfType<FileItem>().ToList(),
                _ => new List<FileItem>()
            };
        }

        private void RemoveSelected(object? parameter)
        {
            if (IsProcessing) return;

            // SelectedItems は削除中に変化するので先にコピーする
            foreach (var item in ToFileItems(parameter))
            {
                SelectedFiles.Remove(item);
            }
        }

        private void OpenFile(object? parameter)
        {
            var item = ToFileItems(parameter).FirstOrDefault();
            if (item == null) return;

            try
            {
                Process.Start(new ProcessStartInfo(item.CurrentFilePath) { UseShellExecute = true });
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or FileNotFoundException)
            {
                _logService.LogError($"{_languageService.GetString("OpenFileError")}: {ex.Message}");
            }
        }

        private void OpenFolder(object? parameter)
        {
            var item = ToFileItems(parameter).FirstOrDefault();
            if (item == null) return;

            try
            {
                // エクスプローラーでファイルを選択した状態で開く
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{item.CurrentFilePath}\"") { UseShellExecute = true });
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
            {
                _logService.LogError($"{_languageService.GetString("OpenFileError")}: {ex.Message}");
            }
        }

        private void CopyNewName(object? parameter)
        {
            var names = ToFileItems(parameter)
                .Select(f => f.EditableFileName)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();
            if (names.Count == 0) return;

            try
            {
                Clipboard.SetText(string.Join(Environment.NewLine, names));
            }
            catch (System.Runtime.InteropServices.ExternalException ex)
            {
                // 他のアプリがクリップボードを使用中の場合など
                _logService.LogError(ex.Message);
            }
        }

        #endregion

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
                settingsViewModel.SettingsSaved += (s, e) =>
                {
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
            // 更新中にリストが変更されても例外にならないようスナップショットを使う。
            // リネーム済みのファイルは元のパスに存在しないため、プレビューを再計算しない。
            var targets = SelectedFiles.Where(f => f.Status != STATUS_RENAME_COMPLETE).ToList();
            foreach (var fileItem in targets)
            {
                await SetPredictedFileNameAsync(fileItem);
            }
        }

#pragma warning disable CA1031 // async void boundary: log unexpected preview refresh failures.
        private async void UpdateAllFilePreviews()
        {
            try
            {
#pragma warning disable CA2007 // Continue on the UI context for preview refresh updates.
                await UpdateAllFilePreviewsAsync();
#pragma warning restore CA2007
            }
            catch (Exception ex)
            {
                _logService.LogError($"{_languageService.GetString("ProcessingErrorLog")}: {ex.Message}", ex);
            }
        }
#pragma warning restore CA1031

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

        private bool HasUsableMetadata(PdfMetadata metadata)
        {
            // 有効なPDFメタデータ項目のいずれかに値があるかチェック
            return _settings.Elements.Any(e => e.IsEnabled && !string.IsNullOrWhiteSpace(e.ElementType switch
            {
                "Title" => metadata.Title,
                "Author" => metadata.Author,
                "Subject" => metadata.Subject,
                "Keywords" => metadata.Keywords,
                _ => null
            }));
        }

        private string JoinFileNameParts(IReadOnlyList<FileNamePart> parts)
        {
            return string.Join(_settings.Separator, parts.Select(part => part.Value));
        }

        private string SanitizeFileNameWithNFKCControl(IReadOnlyList<FileNamePart> parts)
        {
            var sanitizedParts = new List<string>();

            // 各部分を個別にサニタイズ
            foreach (var part in parts)
            {
                // NFKC正規化の適用制御
                bool applyNFKC = part.ElementType switch
                {
                    "OriginalFileName" => false, // 変更前のファイル名は除外
                    "CustomPrefix" => false,     // プレフィックスは除外
                    "CustomSuffix" => false,     // サフィックスは除外
                    _ => true                    // その他は適用
                };

                var sanitizedPart = _pdfProcessingService.SanitizeFileName(part.Value, applyNFKC);
                sanitizedParts.Add(sanitizedPart);
            }

            // セパレータで結合
            var joined = string.Join(_settings.Separator, sanitizedParts);

            // 各要素は240文字以内でも、結合後にWindowsのファイル名上限を超えることがあるため全体も切り詰める
            if (joined.Length > MaxFileNameLength)
            {
                joined = joined.Substring(0, MaxFileNameLength).TrimEnd();
            }

            return joined;
        }
    }
}
