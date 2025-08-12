using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.IO;
using System;
using System.Collections.Generic;

namespace PdfTitleRenamer.Models
{
    public class LanguageSettings : INotifyPropertyChanged
    {
        private string _currentLanguage = "en";

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set => SetProperty(ref _currentLanguage, value);
        }

        // 設定ファイルパスを取得
        public static string GetSettingsFilePath()
        {
            // Windows標準のアプリケーション設定保存場所を使用
            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrEmpty(localAppDataPath))
            {
                var appFolder = Path.Combine(localAppDataPath, "PDF_Title_to_Filename");
                return Path.Combine(appFolder, "language_settings.json");
            }
            
            // フォールバック: ユーザーのドキュメントフォルダ
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrEmpty(documentsPath))
            {
                var appFolder = Path.Combine(documentsPath, "PDF_Title_to_Filename");
                return Path.Combine(appFolder, "language_settings.json");
            }
            
            // 最終フォールバック: 実行ファイルと同じディレクトリ
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
            
            return Path.Combine(exeDirectory, "language_settings.json");
        }

        // デフォルト設定
        public static LanguageSettings Default => new()
        {
            CurrentLanguage = "en"
        };

        // 設定を保存
        public void Save()
        {
            try
            {
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
                    settingsPath = Path.Combine(tempPath, "PDF_Title_to_Filename_language_settings.json");
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
        public static LanguageSettings Load()
        {
            try
            {
                var settingsPath = GetSettingsFilePath();
                
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<LanguageSettings>(json);
                    if (settings != null)
                    {
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
