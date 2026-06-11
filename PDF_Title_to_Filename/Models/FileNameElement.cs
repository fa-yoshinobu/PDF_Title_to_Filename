using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using PdfTitleRenamer.Services;

namespace PdfTitleRenamer.Models
{
    internal class FileNameElement : INotifyPropertyChanged
    {
        private bool _isEnabled;
        private string _elementType = string.Empty;
        private static ILanguageService? _languageService;

        internal static ILanguageService? LanguageService => _languageService;

        public FileNameElement(string elementType, bool isEnabled = true)
        {
            ElementType = elementType;
            IsEnabled = isEnabled;
        }

        public static void SetLanguageService(ILanguageService languageService)
        {
            _languageService = languageService;
        }

        public string ElementType
        {
            get => _elementType;
            set => SetProperty(ref _elementType, value);
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public string DisplayName
        {
            get => ElementType switch
            {
                "Title" => LanguageService?.GetString("PDFTitleDisplay") ?? "PDF Title",
                "Author" => LanguageService?.GetString("PDFAuthorDisplay") ?? "PDF Author",
                "Subject" => LanguageService?.GetString("PDFSubjectDisplay") ?? "PDF Subject",
                "Keywords" => LanguageService?.GetString("PDFKeywordsDisplay") ?? "PDF Keywords",
                "OriginalFileName" => LanguageService?.GetString("OriginalFileNameDisplay") ?? "Original File Name",
                "CustomPrefix" => LanguageService?.GetString("CustomPrefixDisplay") ?? "Prefix",
                "CustomSuffix" => LanguageService?.GetString("CustomSuffixDisplay") ?? "Suffix",
                _ => ElementType
            };
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set 
            {
                SetProperty(ref _isEnabled, value);
            }
        }

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

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
