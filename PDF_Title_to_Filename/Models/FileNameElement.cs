using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace PdfTitleRenamer.Models
{
    public class FileNameElement : INotifyPropertyChanged
    {
        private bool _isEnabled;
        private string _elementType = string.Empty;

        public FileNameElement(string elementType, bool isEnabled = true)
        {
            ElementType = elementType;
            IsEnabled = isEnabled;
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
                "Title" => "PDFのタイトル",
                "Author" => "PDFの作成者",
                "Subject" => "PDFのサブタイトル",
                "Keywords" => "PDFのキーワード",
                "OriginalFileName" => "変更前のファイル名",
                "CustomPrefix" => "プレフィックス",
                "CustomSuffix" => "サフィックス",
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
