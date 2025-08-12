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
            _newFileName = "ProcessingPending";
            _status = "Waiting"; // 定数を使用する場合は、MainWindowViewModelから定数を参照する必要があります
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
                OnPropertyChanged(nameof(NewFileNameDisplay));
            }
        }

        // 表示用のファイル名（ローカライズ対応）
        public string NewFileNameDisplay
        {
            get
            {
                if (FileNameElement._languageService != null)
                {
                    return FileNameElement._languageService.GetString(_newFileName) ?? _newFileName;
                }
                return _newFileName;
            }
        }
        
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusDisplay));
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

        // 表示用の状態文字列（ローカライズ対応）
        public string StatusDisplay
        {
            get
            {
                if (FileNameElement._languageService != null)
                {
                    return FileNameElement._languageService.GetString(Status) ?? Status;
                }
                return Status;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // 言語変更時にStatusDisplayとNewFileNameDisplayを更新するための公開メソッド
        public void UpdateStatusDisplay()
        {
            OnPropertyChanged(nameof(StatusDisplay));
            OnPropertyChanged(nameof(NewFileNameDisplay));
        }
    }
}
