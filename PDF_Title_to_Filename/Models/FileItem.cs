using System.IO;
using System.ComponentModel;

namespace PdfTitleRenamer.Models
{
    public class FileItem : INotifyPropertyChanged
    {
        private string _newFileName;
        private string _status;
        private string _errorDetails;

        public FileItem(string filePath)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            _newFileName = "処理待ち...";
            _status = "待機中";
            _errorDetails = "";
        }

        public string FilePath { get; }
        public string FileName { get; }
        
        public string NewFileName
        {
            get => _newFileName;
            set
            {
                _newFileName = value;
                OnPropertyChanged(nameof(NewFileName));
            }
        }
        
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }
        
        public string ErrorDetails
        {
            get => _errorDetails;
            set
            {
                _errorDetails = value;
                OnPropertyChanged(nameof(ErrorDetails));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
