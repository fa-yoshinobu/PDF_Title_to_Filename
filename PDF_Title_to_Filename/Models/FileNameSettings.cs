using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.IO;
using System.Collections.ObjectModel;

namespace PdfTitleRenamer.Models
{
    public class FileNameSettings : INotifyPropertyChanged
    {
        private string _customPrefix = "";
        private string _customSuffix = "";
        private string _separator = " - ";
        private ObservableCollection<FileNameElement> _elements = new();
        private string _currentLanguage = "en";

        public string CustomPrefix
        {
            get => _customPrefix;
            set => SetProperty(ref _customPrefix, value);
        }

        public string CustomSuffix
        {
            get => _customSuffix;
            set => SetProperty(ref _customSuffix, value);
        }



        public string Separator
        {
            get => _separator;
            set => SetProperty(ref _separator, value);
        }

        public ObservableCollection<FileNameElement> Elements
        {
            get => _elements;
            set => SetProperty(ref _elements, value);
        }

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set => SetProperty(ref _currentLanguage, value);
        }



        // 設定ファイルパスを取得
        public static string GetSettingsFilePath()
        {
            // 優先: 実行ファイルと同じディレクトリ
            string? exeDirectory = null;
            
            try
            {
                var processPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(processPath))
                {
                    exeDirectory = Path.GetDirectoryName(processPath);
                }
            }
            catch (Exception)
            {
                // エラーが発生した場合は次の方法を試す
            }
            
            if (string.IsNullOrEmpty(exeDirectory))
            {
                var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                exeDirectory = Path.GetDirectoryName(exePath);
            }
            
            if (string.IsNullOrEmpty(exeDirectory))
            {
                exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }
            
            if (!string.IsNullOrEmpty(exeDirectory))
            {
                return Path.Combine(exeDirectory, "PDF_Title_to_Filename.json");
            }
            
            // フォールバック1: Windows標準のアプリケーション設定保存場所
            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrEmpty(localAppDataPath))
            {
                var appFolder = Path.Combine(localAppDataPath, "PDF_Title_to_Filename");
                return Path.Combine(appFolder, "PDF_Title_to_Filename.json");
            }
            
            // フォールバック2: ユーザーのドキュメントフォルダ
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrEmpty(documentsPath))
            {
                var appFolder = Path.Combine(documentsPath, "PDF_Title_to_Filename");
                return Path.Combine(appFolder, "PDF_Title_to_Filename.json");
            }
            
            // 最終フォールバック: 一時ディレクトリ
            var tempPath = Path.GetTempPath();
            return Path.Combine(tempPath, "PDF_Title_to_Filename_settings.json");
        }

        // デフォルト設定
        public static FileNameSettings Default => new()
        {
            CustomPrefix = "",
            CustomSuffix = "",
            Separator = " - ",
            Elements = CreateDefaultElements(),
            CurrentLanguage = "en"
        };

        // デフォルト要素の作成
        private static ObservableCollection<FileNameElement> CreateDefaultElements()
        {
            return new ObservableCollection<FileNameElement>
            {
                new FileNameElement("CustomPrefix", false),
                new FileNameElement("Title", true),
                new FileNameElement("Author", false),
                new FileNameElement("Subject", false),
                new FileNameElement("Keywords", false),
                new FileNameElement("OriginalFileName", false),
                new FileNameElement("CustomSuffix", false)
            };
        }

        // 設定を保存
        public void Save()
        {
            try
            {
                // デフォルト設定を確実に初期化
                if (Elements == null || Elements.Count == 0)
                {
                    Elements = CreateDefaultElements();
                }

                // 設定ファイルパスを取得
                var settingsPath = GetSettingsFilePath();
                var settingsDirectory = Path.GetDirectoryName(settingsPath);

                // ディレクトリが存在しない場合は作成
                if (!string.IsNullOrEmpty(settingsDirectory) && !Directory.Exists(settingsDirectory))
                {
                    Directory.CreateDirectory(settingsDirectory);
                }

                // 書き込み権限の確認
                try
                {
                    if (!string.IsNullOrEmpty(settingsDirectory))
                    {
                        var testFile = Path.Combine(settingsDirectory, "test_write.tmp");
                        File.WriteAllText(testFile, "test");
                        File.Delete(testFile);
                    }
                }
                catch (Exception)
                {
                    // 書き込み権限がない場合は、一時ディレクトリを使用
                    var tempPath = Path.GetTempPath();
                    settingsPath = Path.Combine(tempPath, "PDF_Title_to_Filename_settings.json");
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
            }
            catch (Exception)
            {
                // 設定保存に失敗した場合は無視（デフォルト設定を使用）
            }
        }

        // 設定を読み込み
        public static FileNameSettings Load()
        {
            try
            {
                var settingsPath = GetSettingsFilePath();
                
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<FileNameSettings>(json);
                    if (settings != null)
                    {
                        // Elementsがnullの場合はデフォルトで初期化
                        if (settings.Elements == null || settings.Elements.Count == 0)
                        {
                            settings.Elements = CreateDefaultElements();
                        }
                        return settings;
                    }
                }
            }
            catch (Exception)
            {
                // 設定読み込みに失敗した場合はデフォルト設定を使用
            }

            return Default;
        }

        // テンプレートからファイル名を生成
        public string GenerateFileName(string originalFileName, string title, string author, string subject, string keywords)
        {
            var parts = new List<string>();

            // ログサービスは直接取得できないため、ログ出力は行わない
            // 代わりにMainWindowViewModelでログを出力する

            // 有効な要素を順序に従って追加
            foreach (var element in Elements.Where(e => e.IsEnabled))
            {
                string? value = element.ElementType switch
                {
                    "OriginalFileName" => !string.IsNullOrWhiteSpace(originalFileName) ? Path.GetFileNameWithoutExtension(originalFileName) : null,
                    "Title" => !string.IsNullOrWhiteSpace(title) ? title.Trim() : null,
                    "Author" => !string.IsNullOrWhiteSpace(author) ? author.Trim() : null,
                    "Subject" => !string.IsNullOrWhiteSpace(subject) ? subject.Trim() : null,
                    "Keywords" => !string.IsNullOrWhiteSpace(keywords) ? keywords.Trim() : null,
                    "CustomPrefix" => !string.IsNullOrWhiteSpace(CustomPrefix) ? CustomPrefix.Trim() : null,
                    "CustomSuffix" => !string.IsNullOrWhiteSpace(CustomSuffix) ? CustomSuffix.Trim() : null,
                    _ => null
                };

                if (!string.IsNullOrWhiteSpace(value))
                {
                    parts.Add(value);
                }
            }

            // パーツがない場合は空文字列を返す（メタデータなしとして処理）
            if (parts.Count == 0)
            {
                return "";
            }

            // セパレータで結合
            return string.Join(Separator, parts);
        }

        // プレビュー用のファイル名生成
        public string GeneratePreviewFileName(string originalFileName = "sample.pdf", string? title = null, 
            string? author = null, string? subject = null, string? keywords = null)
        {
            // デフォルト値を動的に取得
            var defaultTitle = title ?? GetDefaultPreviewValue("SampleTitle");
            var defaultAuthor = author ?? GetDefaultPreviewValue("SampleAuthor");
            var defaultSubject = subject ?? GetDefaultPreviewValue("SampleSubject");
            var defaultKeywords = keywords ?? GetDefaultPreviewValue("SampleKeywords");
            
            return GenerateFileName(originalFileName, defaultTitle, defaultAuthor, defaultSubject, defaultKeywords);
        }

        private string GetDefaultPreviewValue(string key)
        {
            // LanguageServiceが利用可能な場合はローカライズされた値を取得
            if (FileNameElement._languageService != null)
            {
                return FileNameElement._languageService.GetString(key) ?? GetFallbackValue(key);
            }
            return GetFallbackValue(key);
        }

        private string GetFallbackValue(string key)
        {
            return key switch
            {
                "SampleTitle" => "Sample Title",
                "SampleAuthor" => "Author Name",
                "SampleSubject" => "Subject",
                "SampleKeywords" => "Keywords",
                _ => "Unknown"
            };
        }

        // 要素の有効状態を取得
        private bool GetElementEnabled(string elementType)
        {
            return Elements?.FirstOrDefault(e => e.ElementType == elementType)?.IsEnabled ?? false;
        }

        // 要素の有効状態を設定
        private void SetElementEnabled(string elementType, bool enabled)
        {
            var element = Elements?.FirstOrDefault(e => e.ElementType == elementType);
            if (element != null)
            {
                element.IsEnabled = enabled;
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
    }
}
