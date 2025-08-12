using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using PdfTitleRenamer.Services;

namespace PdfTitleRenamer.Models
{
    public class FileNameElement : INotifyPropertyChanged
    {
        private bool _isEnabled;
        private string _elementType = string.Empty;
        public static ILanguageService? _languageService;

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
                "Title" => _languageService?.GetString("PDFTitleDisplay") ?? "PDF Title",
                "Author" => _languageService?.GetString("PDFAuthorDisplay") ?? "PDF Author",
                "Subject" => _languageService?.GetString("PDFSubjectDisplay") ?? "PDF Subject",
                "Keywords" => _languageService?.GetString("PDFKeywordsDisplay") ?? "PDF Keywords",
                "OriginalFileName" => _languageService?.GetString("OriginalFileNameDisplay") ?? "Original File Name",
                "CustomPrefix" => _languageService?.GetString("CustomPrefixDisplay") ?? "Prefix",
                "CustomSuffix" => _languageService?.GetString("CustomSuffixDisplay") ?? "Suffix",
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
