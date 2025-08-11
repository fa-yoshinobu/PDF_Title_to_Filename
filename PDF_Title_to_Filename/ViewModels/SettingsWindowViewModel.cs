using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using PdfTitleRenamer.Models;
using PdfTitleRenamer.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace PdfTitleRenamer.ViewModels
{
    public class SettingsWindowViewModel : INotifyPropertyChanged
    {
        private readonly FileNameSettings _settings;
        private readonly ILogService _logService;
        private string _previewFileName = "";

        public SettingsWindowViewModel(FileNameSettings settings, ILogService logService)
        {
            _settings = settings;
            _logService = logService;

            // コマンドの初期化
            SaveCommand = new RelayCommand(SaveSettings);
            ResetCommand = new RelayCommand(ResetSettings);
            CancelCommand = new RelayCommand(Cancel);
            MoveUpCommand = new RelayCommand<FileNameElement>(MoveUp, CanMoveUp);
            MoveDownCommand = new RelayCommand<FileNameElement>(MoveDown, CanMoveDown);

            // 設定変更時のプレビュー更新
            _settings.PropertyChanged += (s, e) => 
            {
                // CustomPrefix、CustomSuffix、Separatorの変更時もプレビューを更新
                if (e.PropertyName == nameof(_settings.CustomPrefix) || 
                    e.PropertyName == nameof(_settings.CustomSuffix) || 
                    e.PropertyName == nameof(_settings.Separator))
                {
                    UpdatePreview();
                }
            };
            
            // Elementsの変更も監視
            _settings.Elements.CollectionChanged += (s, e) => 
            {
                UpdatePreview();
                // 新しい要素が追加された場合、その要素の変更も監視
                if (e.NewItems != null)
                {
                    foreach (FileNameElement element in e.NewItems)
                    {
                        element.PropertyChanged += (sender, args) => UpdatePreview();
                    }
                }
            };
            
            // 既存の各要素の変更も監視
            foreach (var element in _settings.Elements)
            {
                element.PropertyChanged += (s, e) => UpdatePreview();
            }
            
            // 初期プレビューの更新
            UpdatePreview();
        }

        public FileNameSettings Settings => _settings;

        public string PreviewFileName
        {
            get => _previewFileName;
            set => SetProperty(ref _previewFileName, value);
        }

        // コマンド
        public ICommand SaveCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }

        private void SaveSettings()
        {
            try
            {
                _settings.Save();
                _logService.LogInfo("設定を保存しました");
                
                // ダイアログを閉じる（実際の実装ではWindow.Close()を呼び出す）
                // ここではイベントで通知
                SettingsSaved?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logService.LogError($"設定の保存に失敗しました: {ex.Message}");
            }
        }

        private void ResetSettings()
        {
            try
            {
                var defaultSettings = FileNameSettings.Default;
                
                // デフォルト設定を現在の設定にコピー
                _settings.CustomPrefix = defaultSettings.CustomPrefix;
                _settings.CustomSuffix = defaultSettings.CustomSuffix;
                _settings.Separator = defaultSettings.Separator;
                
                // 要素リストをリセット
                _settings.Elements.Clear();
                foreach (var element in defaultSettings.Elements)
                {
                    _settings.Elements.Add(new FileNameElement(element.ElementType, element.IsEnabled));
                }

                _logService.LogInfo("設定をデフォルトにリセットしました");
            }
            catch (Exception ex)
            {
                _logService.LogError($"設定のリセットに失敗しました: {ex.Message}");
            }
        }

        private void MoveUp(FileNameElement? element)
        {
            if (element == null) return;
            
            var index = _settings.Elements.IndexOf(element);
            if (index > 0)
            {
                _settings.Elements.Move(index, index - 1);
                UpdatePreview();
            }
        }

        private bool CanMoveUp(FileNameElement? element)
        {
            if (element == null) return false;
            var index = _settings.Elements.IndexOf(element);
            return index > 0;
        }

        private void MoveDown(FileNameElement? element)
        {
            if (element == null) return;
            
            var index = _settings.Elements.IndexOf(element);
            if (index < _settings.Elements.Count - 1)
            {
                _settings.Elements.Move(index, index + 1);
                UpdatePreview();
            }
        }

        private bool CanMoveDown(FileNameElement? element)
        {
            if (element == null) return false;
            var index = _settings.Elements.IndexOf(element);
            return index < _settings.Elements.Count - 1;
        }

        private void Cancel()
        {
            // ダイアログを閉じる（実際の実装ではWindow.Close()を呼び出す）
            // ここではイベントで通知
            SettingsCancelled?.Invoke(this, EventArgs.Empty);
        }

        private void UpdatePreview()
        {
            try
            {
                var preview = _settings.GeneratePreviewFileName();
                PreviewFileName = preview + ".pdf";
            }
            catch (Exception ex)
            {
                PreviewFileName = "プレビュー生成エラー";
                _logService.LogError($"プレビューの生成に失敗しました: {ex.Message}");
            }
        }

        public void UpdatePreviewPublic()
        {
            UpdatePreview();
        }

        // イベント
        public event EventHandler? SettingsSaved;
        public event EventHandler? SettingsCancelled;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
