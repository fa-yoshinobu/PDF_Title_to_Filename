using System.IO;
using System.ComponentModel;

namespace PdfTitleRenamer.Models
{
    internal class FileItem : INotifyPropertyChanged
    {
        private string _newFileName;
        private string _status;
        private string _errorDetails;
        private string? _renamedFilePath;
        private bool _isManuallyEdited;

        public FileItem(string filePath)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            _newFileName = "ProcessingPending";
            _status = "Waiting"; // 定数を使用する場合は、MainWindowViewModelから定数を参照する必要があります
            _errorDetails = "";
        }

        /// <summary>一覧で名前を手修正したときに通知する（サニタイズは ViewModel 側で行う）</summary>
        public event EventHandler<string>? ManualNameEdited;

        /// <summary>追加時のパス（リネーム後も変わらない）</summary>
        public string FilePath { get; }

        /// <summary>追加時のファイル名（リネーム後も変わらない）</summary>
        public string FileName { get; }

        /// <summary>リネーム後のパス。リネームしていない場合は null</summary>
        public string? RenamedFilePath
        {
            get => _renamedFilePath;
            set
            {
                _renamedFilePath = value;
                OnPropertyChanged(nameof(RenamedFilePath));
                OnPropertyChanged(nameof(CurrentFilePath));
            }
        }

        /// <summary>現在ディスク上にあるパス</summary>
        public string CurrentFilePath => RenamedFilePath ?? FilePath;

        // 有効なメタデータ項目に値があるか（メタデータ読み取り前は true）
        public bool HasMetadata { get; set; } = true;

        /// <summary>新しい名前が一覧で手修正されたか</summary>
        public bool IsManuallyEdited
        {
            get => _isManuallyEdited;
            set
            {
                _isManuallyEdited = value;
                OnPropertyChanged(nameof(IsManuallyEdited));
            }
        }

        public string NewFileName
        {
            get => _newFileName;
            set
            {
                _newFileName = value;
                OnPropertyChanged(nameof(NewFileName));
                OnPropertyChanged(nameof(NewFileNameDisplay));
                OnPropertyChanged(nameof(EditableFileName));
            }
        }

        // 表示用のファイル名（ローカライズ対応）
        public string NewFileNameDisplay
        {
            get
            {
                if (FileNameElement.LanguageService != null)
                {
                    return FileNameElement.LanguageService.GetString(_newFileName) ?? _newFileName;
                }
                return _newFileName;
            }
        }

        /// <summary>
        /// 編集用の名前。"NoMetadata" などの状態を表す値のときは空欄から入力させる。
        /// </summary>
        public string EditableFileName
        {
            get => _newFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? _newFileName : string.Empty;
            set
            {
                if (!string.Equals(value, EditableFileName, StringComparison.Ordinal))
                {
                    ManualNameEdited?.Invoke(this, value ?? string.Empty);
                }
            }
        }

        /// <summary>名前を編集できる状態か（リネーム済み・処理中は不可）</summary>
        public bool CanEditName => Status != "RenameComplete" && Status != "Processing";

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusDisplay));
                OnPropertyChanged(nameof(CanEditName));
            }
        }

        public string ErrorDetails
        {
            get => _errorDetails;
            set
            {
                _errorDetails = value;
                OnPropertyChanged(nameof(ErrorDetails));
                OnPropertyChanged(nameof(HasErrorDetails));
            }
        }

        public bool HasErrorDetails => !string.IsNullOrWhiteSpace(_errorDetails);

        // 表示用の状態文字列（ローカライズ対応）
        public string StatusDisplay
        {
            get
            {
                if (FileNameElement.LanguageService != null)
                {
                    return FileNameElement.LanguageService.GetString(Status) ?? Status;
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
