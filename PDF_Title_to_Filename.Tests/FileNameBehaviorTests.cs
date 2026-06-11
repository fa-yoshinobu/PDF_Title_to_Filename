using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using PdfTitleRenamer.Models;
using PdfTitleRenamer.Services;
using PdfTitleRenamer.ViewModels;
using Xunit;

namespace PDF_Title_to_Filename.Tests
{
    public class FileNameBehaviorTests
    {
        [Fact]
        public void GenerateFileName_UsesEnabledElementsInOrderAndSkipsEmptyValues()
        {
            var settings = new FileNameSettings
            {
                CustomPrefix = "  PRE  ",
                CustomSuffix = "  SUF  ",
                Separator = " | ",
                Elements = new ObservableCollection<FileNameElement>
                {
                    new("CustomPrefix", true),
                    new("Author", true),
                    new("Title", true),
                    new("OriginalFileName", true),
                    new("CustomSuffix", true),
                    new("Subject", false)
                }
            };

            var result = settings.GenerateFileName(
                "Original.pdf",
                "  Title  ",
                "",
                "Subject",
                "Keywords");

            Assert.Equal("PRE | Title | Original | SUF", result);
        }

        [Fact]
        public void GenerateFileName_ReturnsEmptyStringWhenEnabledValuesAreEmpty()
        {
            var settings = new FileNameSettings
            {
                Elements = new ObservableCollection<FileNameElement>
                {
                    new("Title", true),
                    new("Author", true)
                }
            };

            var result = settings.GenerateFileName("Original.pdf", "", "   ", "Subject", "Keywords");

            Assert.Equal("", result);
        }

        [Fact]
        public void SanitizeFileName_PreservesCurrentSanitizingRules()
        {
            var service = CreatePdfProcessingService();

            Assert.Equal("ABC 123", service.SanitizeFileName("ＡＢＣ　１２３"));
            Assert.Equal("ＡＢＣ １２３", service.SanitizeFileName("ＡＢＣ　１２３", applyNFKC: false));
            Assert.Equal("A_B", service.SanitizeFileName("A<B"));
            Assert.Equal("A B", service.SanitizeFileName("  A   B  "));
            Assert.Equal("Untitled", service.SanitizeFileName("   "));
            Assert.Equal("_CON.txt", service.SanitizeFileName("CON.txt"));
            Assert.Equal(240, service.SanitizeFileName(new string('a', 241)).Length);
        }

        [Fact]
        public void GetUniqueFilePath_AppendsIncrementingSuffixWhenTargetExists()
        {
            var directory = CreateTempDirectory();
            try
            {
                var service = CreatePdfProcessingService();
                var first = Path.Combine(directory, "report.pdf");
                var second = Path.Combine(directory, "report(1).pdf");
                File.WriteAllText(first, "first");
                File.WriteAllText(second, "second");

                var result = service.GetUniqueFilePath(first);

                Assert.Equal(Path.Combine(directory, "report(2).pdf"), result);
                Assert.Equal(Path.Combine(directory, "new.pdf"), service.GetUniqueFilePath(Path.Combine(directory, "new.pdf")));
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        [Fact]
        public void FileNameSettings_SaveAndLoad_RoundTripsJsonSchema()
        {
            var directory = CreateTempDirectory();
            try
            {
                var path = Path.Combine(directory, "PDF_Title_to_Filename.json");
                var settings = new FileNameSettings
                {
                    CustomPrefix = "Prefix",
                    CustomSuffix = "Suffix",
                    Separator = "__",
                    CurrentLanguage = "ja",
                    Elements = new ObservableCollection<FileNameElement>
                    {
                        new("CustomPrefix", true),
                        new("Title", false),
                        new("OriginalFileName", true)
                    }
                };

                settings.Save(path);
                var loaded = FileNameSettings.Load(path);

                Assert.True(File.Exists(path));
                Assert.Contains("\"CustomPrefix\": \"Prefix\"", File.ReadAllText(path), StringComparison.Ordinal);
                Assert.Equal("Prefix", loaded.CustomPrefix);
                Assert.Equal("Suffix", loaded.CustomSuffix);
                Assert.Equal("__", loaded.Separator);
                Assert.Equal("ja", loaded.CurrentLanguage);
                Assert.Collection(
                    loaded.Elements,
                    element =>
                    {
                        Assert.Equal("CustomPrefix", element.ElementType);
                        Assert.True(element.IsEnabled);
                    },
                    element =>
                    {
                        Assert.Equal("Title", element.ElementType);
                        Assert.False(element.IsEnabled);
                    },
                    element =>
                    {
                        Assert.Equal("OriginalFileName", element.ElementType);
                        Assert.True(element.IsEnabled);
                    });
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        [Fact]
        public void FileNameSettings_LoadFallsBackToDefaultWhenJsonIsInvalid()
        {
            var directory = CreateTempDirectory();
            try
            {
                var path = Path.Combine(directory, "PDF_Title_to_Filename.json");
                File.WriteAllText(path, "{ invalid json", Encoding.UTF8);

                var loaded = FileNameSettings.Load(path);

                Assert.Equal(" - ", loaded.Separator);
                Assert.Equal("en", loaded.CurrentLanguage);
                Assert.Contains(loaded.Elements, element => element.ElementType == "Title" && element.IsEnabled);
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        [Fact]
        public void SanitizeFileNameWithNfkcControl_PreservesCurrentSkippedElementIndexDrift()
        {
            var settings = new FileNameSettings
            {
                Separator = " - ",
                Elements = new ObservableCollection<FileNameElement>
                {
                    new("Title", true),
                    new("OriginalFileName", true)
                }
            };
            var viewModel = CreateViewModel(settings);
            var generated = settings.GenerateFileName("ＯＲＩＧ.pdf", "", "", "", "");

            // Known issue fixed as current behavior: the empty Title is skipped by GenerateFileName,
            // but the sanitizer still maps the first generated part to the first enabled element.
            var result = InvokeSanitizeFileNameWithNfkcControl(viewModel, generated);

            Assert.Equal("ORIG", result);
        }

        [Fact]
        public void SanitizeFileNameWithNfkcControl_PreservesCurrentSeparatorSplitDrift()
        {
            var settings = new FileNameSettings
            {
                Separator = " - ",
                Elements = new ObservableCollection<FileNameElement>
                {
                    new("Title", true),
                    new("OriginalFileName", true)
                }
            };
            var viewModel = CreateViewModel(settings);
            var generated = settings.GenerateFileName("ＯＲＩＧ.pdf", "Ａ - Ｂ", "", "", "");

            // Known issue fixed as current behavior: metadata containing the separator is split
            // as if it were multiple filename elements, and extra parts are dropped.
            var result = InvokeSanitizeFileNameWithNfkcControl(viewModel, generated);

            Assert.Equal("A - Ｂ", result);
        }

        private static PdfProcessingService CreatePdfProcessingService()
        {
            return new PdfProcessingService(
                new TestLogger<PdfProcessingService>(),
                new TestLogService(),
                new TestLanguageService());
        }

        private static MainWindowViewModel CreateViewModel(FileNameSettings settings)
        {
            var pdfProcessingService = CreatePdfProcessingService();
            var logService = new TestLogService();
            var languageService = new TestLanguageService();

            var viewModel = new MainWindowViewModel(pdfProcessingService, logService, languageService);
            var settingsField = typeof(MainWindowViewModel).GetField("_settings", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(settingsField);
            settingsField.SetValue(viewModel, settings);
            return viewModel;
        }

        private static string InvokeSanitizeFileNameWithNfkcControl(MainWindowViewModel viewModel, string fileName)
        {
            var method = typeof(MainWindowViewModel).GetMethod(
                "SanitizeFileNameWithNFKCControl",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            return Assert.IsType<string>(method.Invoke(viewModel, new object[] { fileName }));
        }

        private static string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), $"PDF_Title_to_Filename.Tests.{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            return path;
        }

        private static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }

        private sealed class TestLanguageService : ILanguageService
        {
            public string CurrentLanguage => "en";

            public event EventHandler? LanguageChanged;

            public string GetString(string key)
            {
                return key;
            }

            public void SetLanguage(string languageCode)
            {
                LanguageChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private sealed class TestLogService : ILogService
        {
            public event EventHandler<string>? LogAdded;

            public void LogMessage(string message)
            {
                LogInfo(message);
            }

            public void LogError(string message, Exception? exception = null)
            {
                LogAdded?.Invoke(this, message);
            }

            public void LogInfo(string message)
            {
                LogAdded?.Invoke(this, message);
            }

            public void LogWarning(string message)
            {
                LogAdded?.Invoke(this, message);
            }

            public void LogDebug(string message)
            {
            }

            public void LogVerbose(string category, string message, object? data = null)
            {
            }

            public string GetLogText()
            {
                return string.Empty;
            }

            public void ClearLog()
            {
                LogAdded?.Invoke(this, string.Empty);
            }
        }

        private sealed class TestLogger<T> : ILogger<T>
        {
            public IDisposable BeginScope<TState>(TState state)
                where TState : notnull
            {
                return NullScope.Instance;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return false;
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
            }

            private sealed class NullScope : IDisposable
            {
                internal static readonly NullScope Instance = new();

                public void Dispose()
                {
                }
            }
        }
    }
}
