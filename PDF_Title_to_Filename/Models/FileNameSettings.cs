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



        // 設定ファイルパスを取得
        public static string GetSettingsFilePath()
        {
            string? exeDirectory = null;
            
            // 1. Process.GetCurrentProcess().MainModule.FileNameを最初に試す（最も確実）
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
            
            // 2. 実行ファイルの場所が取得できない場合はAssembly.GetExecutingAssembly().Locationを試す
            if (string.IsNullOrEmpty(exeDirectory))
            {
                var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                exeDirectory = Path.GetDirectoryName(exePath);
            }
            
            // 3. それでも取得できない場合はBaseDirectoryを使用
            if (string.IsNullOrEmpty(exeDirectory))
            {
                exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }
            
            return Path.Combine(exeDirectory, "PDF_Title_to_Filename.json");
        }

        // デフォルト設定
        public static FileNameSettings Default => new()
        {
            CustomPrefix = "",
            CustomSuffix = "",
            Separator = " - ",
            Elements = CreateDefaultElements()
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
                    // 代替パスとしてユーザーのドキュメントフォルダを使用
                    var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    if (!string.IsNullOrEmpty(documentsPath))
                    {
                        settingsPath = Path.Combine(documentsPath, "PDF_Title_to_Filename.json");
                    }
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
            // 取得対象として設定されているメタデータがすべて空の場合は空文字列を返す（リネームしない）
            var hasMetadata = false;
            
            // 有効なメタデータ要素をチェック
            foreach (var element in Elements.Where(e => e.IsEnabled))
            {
                switch (element.ElementType)
                {
                    case "Title":
                        if (!string.IsNullOrWhiteSpace(title))
                        {
                            hasMetadata = true;
                            break;
                        }
                        break;
                    case "Author":
                        if (!string.IsNullOrWhiteSpace(author))
                        {
                            hasMetadata = true;
                            break;
                        }
                        break;
                    case "Subject":
                        if (!string.IsNullOrWhiteSpace(subject))
                        {
                            hasMetadata = true;
                            break;
                        }
                        break;
                    case "Keywords":
                        if (!string.IsNullOrWhiteSpace(keywords))
                        {
                            hasMetadata = true;
                            break;
                        }
                        break;
                }
                
                // メタデータが見つかったらループを抜ける
                if (hasMetadata) break;
            }
            
            if (!hasMetadata)
            {
                return "";
            }

            var parts = new List<string>();

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

            // パーツがない場合は元のファイル名を使用
            if (parts.Count == 0)
            {
                return Path.GetFileNameWithoutExtension(originalFileName);
            }

            // セパレータで結合
            return string.Join(Separator, parts);
        }

        // プレビュー用のファイル名生成
        public string GeneratePreviewFileName(string originalFileName = "sample.pdf", string title = "サンプルタイトル", 
            string author = "作成者名", string subject = "サブタイトル", string keywords = "キーワード")
        {
            return GenerateFileName(originalFileName, title, author, subject, keywords);
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
