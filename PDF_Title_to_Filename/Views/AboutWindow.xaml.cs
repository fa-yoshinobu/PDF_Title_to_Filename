using System.Windows;
using System.Diagnostics;
using PdfTitleRenamer.Helpers;

namespace PdfTitleRenamer.Views
{
    /// <summary>
    /// AboutWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            SetVersionInfo();
        }

        public AboutWindow(Window owner)
        {
            InitializeComponent();
            Owner = owner;
            SetVersionInfo();
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
                    VersionTextBlock.Text = $"Version {version}";
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
