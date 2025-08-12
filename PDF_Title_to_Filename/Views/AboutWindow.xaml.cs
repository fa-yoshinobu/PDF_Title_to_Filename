using System.Windows;
using System.Diagnostics;
using PdfTitleRenamer.Helpers;
using PdfTitleRenamer.ViewModels;
using PdfTitleRenamer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace PdfTitleRenamer.Views
{
    /// <summary>
    /// AboutWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AboutWindow : Window
    {
        private readonly ILanguageService _languageService;

        public AboutWindow()
        {
            InitializeComponent();
            
            // ViewModelを設定
            _languageService = GetLanguageService();
            DataContext = new AboutWindowViewModel(_languageService);
            
            SetVersionInfo();
        }

        public AboutWindow(Window owner)
        {
            InitializeComponent();
            Owner = owner;
            
            // ViewModelを設定
            _languageService = GetLanguageService();
            DataContext = new AboutWindowViewModel(_languageService);
            
            SetVersionInfo();
        }

        private ILanguageService GetLanguageService()
        {
            try
            {
                if (App.Current is App app && app.Services != null)
                {
                    return app.Services.GetService<ILanguageService>() ?? new LanguageService();
                }
            }
            catch
            {
                // エラーが発生した場合はデフォルトのLanguageServiceを作成
            }
            
            return new LanguageService();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.ToString(),
                    UseShellExecute = true
                });
                e.Handled = true;
            }
            catch
            {
                // エラーが発生した場合は何もしない
            }
        }

        private void SetVersionInfo()
        {
            try
            {
                var version = VersionHelper.GetVersion();
                var fileVersion = VersionHelper.GetFileVersion();
                
                // バージョン情報をUIに設定
                if (VersionTextBlock != null)
                {
                    VersionTextBlock.Text = $"{_languageService?.GetString("VersionPrefix") ?? "Version"} {version}";
                }
                
                if (VersionDetailTextBlock != null)
                {
                    VersionDetailTextBlock.Text = version;
                }
            }
            catch
            {
                // エラーが発生した場合はデフォルト値を設定
            }
        }
    }
}
