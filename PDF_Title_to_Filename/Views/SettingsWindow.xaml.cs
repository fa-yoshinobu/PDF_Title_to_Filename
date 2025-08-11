using System.Windows;
using System.Windows.Controls;
using PdfTitleRenamer.ViewModels;

namespace PdfTitleRenamer.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(SettingsWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // イベントハンドラーの設定
            viewModel.SettingsSaved += (s, e) => 
            {
                DialogResult = true;
                Close();
            };

            viewModel.SettingsCancelled += (s, e) => 
            {
                DialogResult = false;
                Close();
            };

            // ドラッグ&ドロップイベントの設定
            Loaded += SettingsWindow_Loaded;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // ドラッグ&ドロップ機能は削除し、矢印ボタンのみを使用
        }



        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                // プレビューを更新
                var viewModel = DataContext as SettingsWindowViewModel;
                if (viewModel != null)
                {
                    viewModel.UpdatePreviewPublic();
                }
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // プレビューを更新
                var viewModel = DataContext as SettingsWindowViewModel;
                if (viewModel != null)
                {
                    viewModel.UpdatePreviewPublic();
                }
            }
        }
    }
}
