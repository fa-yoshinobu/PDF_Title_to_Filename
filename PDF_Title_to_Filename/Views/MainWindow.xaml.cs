using PdfTitleRenamer.ViewModels;
using PdfTitleRenamer.Services;
using System.Windows;
using System.Linq;
using System;
using System.IO;

namespace PdfTitleRenamer.Views
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;
        private ILanguageService? _languageService;

        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            _languageService = viewModel.GetLanguageService();
            
            // ファイル数の変化に応じてVisibilityを制御
            viewModel.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(viewModel.HasFiles))
                {
                    UpdateVisibility();
                }
            };
            
            UpdateVisibility();
        }
        
        private void UpdateVisibility()
        {
            EmptyDropZone.Visibility = ViewModel.HasFiles ? Visibility.Collapsed : Visibility.Visible;
            FileDataGrid.Visibility = ViewModel.HasFiles ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] items = (string[])e.Data.GetData(DataFormats.FileDrop);
                    var allPdfFiles = new List<string>();
                    
                    foreach (var item in items)
                    {
                        if (File.Exists(item))
                        {
                            // ファイルの場合
                            if (item.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                            {
                                allPdfFiles.Add(item);
                            }
                        }
                        else if (Directory.Exists(item))
                        {
                            // フォルダの場合、再帰的にPDFファイルを検索
                            var pdfFilesInFolder = GetPdfFilesFromDirectory(item);
                            allPdfFiles.AddRange(pdfFilesInFolder);
                        }
                    }
                    
                    if (allPdfFiles.Any())
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
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] items = (string[])e.Data.GetData(DataFormats.FileDrop);
                    bool hasValidItems = items.Any(item => 
                        (File.Exists(item) && item.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) ||
                        Directory.Exists(item));
                    
                    e.Effects = hasValidItems ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
                }
                else
                {
                    e.Effects = System.Windows.DragDropEffects.None;
                }
                
                e.Handled = true;
            }
            catch
            {
                e.Effects = System.Windows.DragDropEffects.None;
                e.Handled = true;
            }
        }
        
        private void DataGrid_Drop(object sender, DragEventArgs e)
        {
            Window_Drop(sender, e);
        }

        private void DataGrid_DragOver(object sender, DragEventArgs e)
        {
            Window_DragOver(sender, e);
        }

        /// <summary>
        /// 指定されたディレクトリから再帰的にPDFファイルを検索
        /// </summary>
        private List<string> GetPdfFilesFromDirectory(string directoryPath)
        {
            var pdfFiles = new List<string>();
            
            try
            {
                // 現在のディレクトリのPDFファイルを検索
                var files = Directory.GetFiles(directoryPath, "*.pdf", SearchOption.TopDirectoryOnly);
                pdfFiles.AddRange(files);
                
                // サブディレクトリも再帰的に検索
                var subDirectories = Directory.GetDirectories(directoryPath);
                foreach (var subDir in subDirectories)
                {
                    var subDirFiles = GetPdfFilesFromDirectory(subDir);
                    pdfFiles.AddRange(subDirFiles);
                }
            }
            catch (Exception)
            {
                // アクセス権限エラーなどは無視して続行
            }
            
            return pdfFiles;
        }
    }
}
