using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using PdfTitleRenamer.Models;
using PdfTitleRenamer.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections.Generic;

namespace PdfTitleRenamer.ViewModels
{
    public class SettingsWindowViewModel : INotifyPropertyChanged
    {
        private readonly FileNameSettings _settings;
        private readonly ILogService _logService;
        private readonly ILanguageService _languageService;

        public SettingsWindowViewModel(FileNameSettings settings, ILogService logService, ILanguageService languageService)
        {
            _settings = settings;
            _logService = logService;
            _languageService = languageService;

            // 言語変更イベントを購読
            _languageService.LanguageChanged += (s, e) => {
                OnPropertyChanged(string.Empty);
            };

            // FileNameElementにLanguageServiceを設定
            FileNameElement.SetLanguageService(_languageService);

            // Commands
            SaveCommand = new RelayCommand(Save);
            ResetCommand = new RelayCommand(Reset);
            CancelCommand = new RelayCommand(Cancel);
            MoveUpCommand = new RelayCommand<FileNameElement>(MoveUp);
            MoveDownCommand = new RelayCommand<FileNameElement>(MoveDown);

            UpdatePreview();
        }

        public FileNameSettings Settings => _settings;

        // UI文字列プロパティ
        public string SettingsWindowTitle => _languageService.GetString("SettingsWindowTitle");
        public string FileNameElementsHeader => _languageService.GetString("FileNameElementsHeader");
        public string ElementsDescription => _languageService.GetString("ElementsDescription");
        public string CustomStringSettingsHeader => _languageService.GetString("CustomStringSettingsHeader");
        public string PrefixLabel => _languageService.GetString("PrefixLabel");
        public string SuffixLabel => _languageService.GetString("SuffixLabel");
        public string PrefixToolTip => _languageService.GetString("PrefixToolTip");
        public string SuffixToolTip => _languageService.GetString("SuffixToolTip");
        public string SeparatorSettingsHeader => _languageService.GetString("SeparatorSettingsHeader");
        public string SeparatorLabel => _languageService.GetString("SeparatorLabel");
        public string SeparatorToolTip => _languageService.GetString("SeparatorToolTip");
        public string PreviewHeader => _languageService.GetString("PreviewHeader");
        public string PreviewLabel => _languageService.GetString("PreviewLabel");
        public string SaveButton => _languageService.GetString("SaveButton");
        public string ResetButton => _languageService.GetString("ResetButton");
        public string CancelButton => _languageService.GetString("CancelButton");
        // FileNameElement display names
        public string OriginalFileNameDisplay => _languageService.GetString("OriginalFileNameDisplay");
        public string PDFTitleDisplay => _languageService.GetString("PDFTitleDisplay");
        public string PDFAuthorDisplay => _languageService.GetString("PDFAuthorDisplay");
        public string PDFSubjectDisplay => _languageService.GetString("PDFSubjectDisplay");
        public string PDFKeywordsDisplay => _languageService.GetString("PDFKeywordsDisplay");
        public string CustomPrefixDisplay => _languageService.GetString("CustomPrefixDisplay");
        public string CustomSuffixDisplay => _languageService.GetString("CustomSuffixDisplay");

        private string _previewFileName = "";
        public string PreviewFileName
        {
            get => _previewFileName;
            set => SetProperty(ref _previewFileName, value);
        }

        // Commands
        public ICommand SaveCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }

        // 設定保存イベント
        public event EventHandler? SettingsSaved;
        // ウィンドウを閉じるイベント
        public event EventHandler? WindowClosed;

        private void Save()
        {
            try
            {
                _settings.Save();
                _logService.LogInfo(_languageService.GetString("SettingsUpdatedLog"));
                // 設定保存イベントを発生
                SettingsSaved?.Invoke(this, EventArgs.Empty);
                // ウィンドウを閉じるイベントを発生
                WindowClosed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logService.LogError($"{_languageService.GetString("SettingsWindowError")}: {ex.Message}");
            }
        }

        private void Reset()
        {
            try
            {
                // デフォルト設定を現在の設定にコピー
                var defaultSettings = FileNameSettings.Default;
                _settings.CustomPrefix = defaultSettings.CustomPrefix;
                _settings.CustomSuffix = defaultSettings.CustomSuffix;
                _settings.Separator = defaultSettings.Separator;
                
                _settings.Elements.Clear();
                foreach (var element in defaultSettings.Elements)
                {
                    _settings.Elements.Add(new FileNameElement(element.ElementType, element.IsEnabled));
                }
                
                UpdatePreview();
                _logService.LogInfo(_languageService.GetString("SettingsUpdatedLog"));
            }
            catch (Exception ex)
            {
                _logService.LogError($"{_languageService.GetString("SettingsWindowError")}: {ex.Message}");
            }
        }

        private void Cancel()
        {
            // ウィンドウを閉じるイベントを発生
            WindowClosed?.Invoke(this, EventArgs.Empty);
        }

        private void MoveUp(FileNameElement? element)
        {
            if (element != null)
            {
                var index = _settings.Elements.IndexOf(element);
                if (index > 0)
                {
                    _settings.Elements.Move(index, index - 1);
                    UpdatePreview();
                }
            }
        }

        private void MoveDown(FileNameElement? element)
        {
            if (element != null)
            {
                var index = _settings.Elements.IndexOf(element);
                if (index < _settings.Elements.Count - 1)
                {
                    _settings.Elements.Move(index, index + 1);
                    UpdatePreview();
                }
            }
        }

        public void UpdatePreview()
        {
            PreviewFileName = _settings.GeneratePreviewFileName();
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
