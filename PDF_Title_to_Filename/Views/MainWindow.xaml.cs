using PdfTitleRenamer.Models;
using PdfTitleRenamer.Services;
using PdfTitleRenamer.ViewModels;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PdfTitleRenamer.Views
{
    internal partial class MainWindow : Window
    {
        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;
        private readonly ILanguageService? _languageService;
        private bool _restoreMaximized;

        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            _languageService = viewModel.GetLanguageService();

            // ファイル数の変化に応じてVisibilityを制御
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(viewModel.HasFiles))
                {
                    UpdateVisibility();
                }
            };

            UpdateVisibility();
            RestoreWindowPlacement();

            SourceInitialized += (s, e) =>
            {
                // App.OnStartup が WindowState を Normal に戻すため、最大化はウィンドウ生成後に復元する
                if (_restoreMaximized)
                {
                    WindowState = WindowState.Maximized;
                }
            };
            Closing += MainWindow_Closing;
        }

        private void UpdateVisibility()
        {
            var hasFiles = ViewModel.HasFiles;
            EmptyDropZone.Visibility = hasFiles ? Visibility.Collapsed : Visibility.Visible;
            FileDataGrid.Visibility = hasFiles ? Visibility.Visible : Visibility.Collapsed;
            FilterBar.Visibility = hasFiles ? Visibility.Visible : Visibility.Collapsed;
        }

        #region ウィンドウ位置・サイズの保存と復元

        private void RestoreWindowPlacement()
        {
            try
            {
                var settings = FileNameSettings.Load();

                if (settings.WindowWidth is double width && settings.WindowHeight is double height &&
                    settings.WindowLeft is double left && settings.WindowTop is double top &&
                    width >= MinWidth && height >= MinHeight)
                {
                    // 保存時と画面構成が変わっていても、ウィンドウが画面内に見える場合だけ復元する
                    var screen = new Rect(
                        SystemParameters.VirtualScreenLeft,
                        SystemParameters.VirtualScreenTop,
                        SystemParameters.VirtualScreenWidth,
                        SystemParameters.VirtualScreenHeight);
                    var titleBar = new Rect(left, top, width, 40);

                    if (screen.IntersectsWith(titleBar))
                    {
                        WindowStartupLocation = WindowStartupLocation.Manual;
                        Left = left;
                        Top = top;
                        Width = width;
                        Height = height;
                    }
                }

                _restoreMaximized = settings.WindowMaximized;

                if (settings.LogPanelHeight is double logHeight && logHeight >= LogRow.MinHeight && logHeight < 2000)
                {
                    LogRow.Height = new GridLength(logHeight);
                }
            }
            catch (Exception)
            {
                // 復元に失敗しても既定の位置で起動する
            }
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            try
            {
                // 他の画面で保存された設定を上書きしないよう、最新の設定を読み直してから保存する
                var settings = FileNameSettings.Load();
                var bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;

                if (!bounds.IsEmpty && bounds.Width > 0 && bounds.Height > 0)
                {
                    settings.WindowLeft = bounds.Left;
                    settings.WindowTop = bounds.Top;
                    settings.WindowWidth = bounds.Width;
                    settings.WindowHeight = bounds.Height;
                }

                settings.WindowMaximized = WindowState == WindowState.Maximized;
                settings.LogPanelHeight = LogRow.ActualHeight;
                settings.Save();
            }
            catch (Exception)
            {
                // 保存に失敗しても終了は妨げない
            }
        }

        #endregion

        #region ドラッグ＆ドロップ

        private void Window_Drop(object sender, DragEventArgs e)
        {
            DropOverlay.Visibility = Visibility.Collapsed;

            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var items = (string[])e.Data.GetData(DataFormats.FileDrop);
                    var allPdfFiles = new List<string>();

                    foreach (var item in items)
                    {
                        if (File.Exists(item))
                        {
                            if (item.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                            {
                                allPdfFiles.Add(item);
                            }
                        }
                        else if (Directory.Exists(item))
                        {
                            // フォルダの場合、再帰的にPDFファイルを検索
                            allPdfFiles.AddRange(GetPdfFilesFromDirectory(item));
                        }
                    }

                    if (allPdfFiles.Count > 0)
                    {
                        ViewModel.AddFiles(allPdfFiles.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{_languageService?.GetString("DragDropError")}: {ex.Message}",
                              _languageService?.GetString("ErrorTitle") ?? "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }

            e.Handled = true;
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            var hasValidItems = false;

            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var items = (string[])e.Data.GetData(DataFormats.FileDrop);
                    hasValidItems = items.Any(item =>
                        (File.Exists(item) && item.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) ||
                        Directory.Exists(item));
                }
            }
            catch (Exception)
            {
                hasValidItems = false;
            }

            e.Effects = hasValidItems ? DragDropEffects.Copy : DragDropEffects.None;
            DropOverlay.Visibility = hasValidItems ? Visibility.Visible : Visibility.Collapsed;
            e.Handled = true;
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            // 子要素の境界をまたぐだけでも DragLeave が来るので、カーソルがウィンドウ外に出たときだけ消す
            var position = e.GetPosition(this);
            if (position.X <= 0 || position.Y <= 0 || position.X >= ActualWidth || position.Y >= ActualHeight)
            {
                DropOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void EmptyDropZone_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.SelectFilesCommand.CanExecute(null))
            {
                ViewModel.SelectFilesCommand.Execute(null);
            }
        }

        /// <summary>
        /// 指定されたディレクトリから再帰的にPDFファイルを検索
        /// </summary>
        private static List<string> GetPdfFilesFromDirectory(string directoryPath)
        {
            var pdfFiles = new List<string>();

            try
            {
                pdfFiles.AddRange(Directory.GetFiles(directoryPath, "*.pdf", SearchOption.TopDirectoryOnly));

                foreach (var subDir in Directory.GetDirectories(directoryPath))
                {
                    pdfFiles.AddRange(GetPdfFilesFromDirectory(subDir));
                }
            }
            catch (Exception)
            {
                // アクセス権限エラーなどは無視して続行
            }

            return pdfFiles;
        }

        #endregion

        #region 一覧・ログ

        private void FileDataGrid_BeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
        {
            // リネーム済み・処理中のファイルは名前を編集させない
            if (e.Row.Item is FileItem fileItem && (!fileItem.CanEditName || ViewModel.IsProcessing))
            {
                e.Cancel = true;
            }
        }

        private void EditTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            // 編集開始時にすぐ入力できるよう、拡張子より前を選択した状態にする
            if (sender is TextBox textBox)
            {
                textBox.Focus();
                var text = textBox.Text ?? string.Empty;
                var length = text.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? text.Length - 4 : text.Length;
                textBox.Select(0, length);
            }
        }

        private void LogTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LogTextBox.ScrollToEnd();
        }

        #endregion
    }
}
