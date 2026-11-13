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
        public void GenerateFileNameParts_KeepsElementTypesForSanitizing()
        {
            var settings = new FileNameSettings
            {
                CustomPrefix = "  PRE  ",
                Separator = " | ",
                Elements = new ObservableCollection<FileNameElement>
                {
                    new("CustomPrefix", true),
                    new("Title", true),
                    new("OriginalFileName", true)
                }
            };

            var result = settings.GenerateFileNameParts("ＯＲＩＧ.pdf", "  Ａ | Ｂ  ", "", "", "");

            Assert.Collection(
                result,
                part =>
                {
                    Assert.Equal("CustomPrefix", part.ElementType);
                    Assert.Equal("PRE", part.Value);
                },
                part =>
                {
                    Assert.Equal("Title", part.ElementType);
                    Assert.Equal("Ａ | Ｂ", part.Value);
                },
                part =>
                {
                    Assert.Equal("OriginalFileName", part.ElementType);
                    Assert.Equal("ＯＲＩＧ", part.Value);
                });
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
        public void FileNameSettings_SaveFallsBackToTempWhenTargetCannotBeWritten()
        {
            var directory = CreateTempDirectory();
            var fallbackPath = Path.Combine(Path.GetTempPath(), "PDF_Title_to_Filename_settings.json");

            try
            {
                if (File.Exists(fallbackPath))
                {
                    File.Delete(fallbackPath);
                }

                var settings = new FileNameSettings
                {
                    CustomPrefix = "Fallback",
                    Elements = new ObservableCollection<FileNameElement>
                    {
                        new("Title", true)
                    }
                };

                settings.Save(directory);

                Assert.True(File.Exists(fallbackPath));
                Assert.Contains("\"CustomPrefix\": \"Fallback\"", File.ReadAllText(fallbackPath), StringComparison.Ordinal);
                Assert.False(File.Exists(Path.Combine(directory, "test_write.tmp")));
            }
            finally
            {
                if (File.Exists(fallbackPath))
                {
                    File.Delete(fallbackPath);
                }

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
        public void SanitizeFileNameWithNfkcControl_UsesOriginalFileNameRuleAfterSkippedMetadata()
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
            var generatedParts = settings.GenerateFileNameParts("ＯＲＩＧ.pdf", "", "", "", "");

            var result = InvokeSanitizeFileNameWithNfkcControl(viewModel, generatedParts);

            Assert.Equal("ＯＲＩＧ", result);
        }

        [Fact]
        public void SanitizeFileNameWithNfkcControl_KeepsMetadataContainingSeparatorAsOnePart()
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
            var generatedParts = settings.GenerateFileNameParts("ＯＲＩＧ.pdf", "Ａ - Ｂ", "", "", "");

            var result = InvokeSanitizeFileNameWithNfkcControl(viewModel, generatedParts);

            Assert.Equal("A - B - ＯＲＩＧ", result);
        }

        [Fact]
        public void SanitizeFileNameWithNfkcControl_TruncatesJoinedNameTo240Characters()
        {
            var settings = new FileNameSettings
            {
                Separator = " - ",
                Elements = new ObservableCollection<FileNameElement>
                {
                    new("Title", true),
                    new("Author", true)
                }
            };
            var viewModel = CreateViewModel(settings);
            var generatedParts = settings.GenerateFileNameParts("orig.pdf", new string('a', 200), new string('b', 200), "", "");

            var result = InvokeSanitizeFileNameWithNfkcControl(viewModel, generatedParts);

            Assert.Equal(240, result.Length);
            Assert.StartsWith(new string('a', 200) + " - ", result, StringComparison.Ordinal);
        }

        [Fact]
        public void RemoveNoMetadataFilesCommand_RemovesOnlyFilesWithoutMetadata()
        {
            var settings = new FileNameSettings
            {
                Elements = new ObservableCollection<FileNameElement>
                {
                    new("Title", true)
                }
            };
            var viewModel = CreateViewModel(settings);
            var withMetadata = new FileItem(@"C:\docs\with.pdf");
            var withoutMetadata1 = new FileItem(@"C:\docs\without1.pdf") { HasMetadata = false };
            var withoutMetadata2 = new FileItem(@"C:\docs\without2.pdf") { HasMetadata = false };
            viewModel.SelectedFiles.Add(withoutMetadata1);
            viewModel.SelectedFiles.Add(withMetadata);
            viewModel.SelectedFiles.Add(withoutMetadata2);

            Assert.True(viewModel.RemoveNoMetadataFilesCommand.CanExecute(null));
            viewModel.RemoveNoMetadataFilesCommand.Execute(null);

            Assert.Same(withMetadata, Assert.Single(viewModel.SelectedFiles));
            Assert.False(viewModel.RemoveNoMetadataFilesCommand.CanExecute(null));
        }

        [Fact]
        public void ApplyManualName_SanitizesNameAndSchedulesRename()
        {
            var viewModel = CreateViewModel(CreateTitleOnlySettings());
            var item = new FileItem(@"C:\docs\old.pdf");
            viewModel.SelectedFiles.Add(item);

            viewModel.ApplyManualName(item, "  New:Name.PDF ");

            Assert.True(item.IsManuallyEdited);
            Assert.Equal("New_Name.pdf", item.NewFileName);
            Assert.Equal("RenameScheduled", item.Status);
        }

        [Fact]
        public void ApplyManualName_SameAsCurrentNameIsNoChange()
        {
            var viewModel = CreateViewModel(CreateTitleOnlySettings());
            var item = new FileItem(Path.Combine(Path.GetTempPath(), "same.pdf"));
            viewModel.SelectedFiles.Add(item);

            viewModel.ApplyManualName(item, "same");

            Assert.Equal("NoChangeNeeded", item.Status);
        }

        [Fact]
        public void ApplyManualName_IsIgnoredForRenamedFiles()
        {
            var viewModel = CreateViewModel(CreateTitleOnlySettings());
            var item = new FileItem(@"C:\docs\done.pdf") { Status = "RenameComplete", NewFileName = "Done.pdf" };
            viewModel.SelectedFiles.Add(item);

            viewModel.ApplyManualName(item, "Other");

            Assert.False(item.IsManuallyEdited);
            Assert.Equal("Done.pdf", item.NewFileName);
        }

        [Fact]
        public void FilterLabels_ShowCountsPerStatus()
        {
            var viewModel = CreateViewModel(CreateTitleOnlySettings());
            viewModel.SelectedFiles.Add(new FileItem(@"C:\docs\a.pdf") { Status = "RenameScheduled" });
            viewModel.SelectedFiles.Add(new FileItem(@"C:\docs\b.pdf") { Status = "NoMetadata" });
            viewModel.SelectedFiles.Add(new FileItem(@"C:\docs\c.pdf") { Status = "Error" });
            var changing = new FileItem(@"C:\docs\d.pdf") { Status = "RenameScheduled" };
            viewModel.SelectedFiles.Add(changing);

            changing.Status = "RenameComplete";

            Assert.Equal("FilterAll (4)", viewModel.FilterAllText);
            Assert.Equal("FilterScheduled (1)", viewModel.FilterScheduledText);
            Assert.Equal("FilterNoMetadata (1)", viewModel.FilterNoMetadataText);
            Assert.Equal("FilterError (1)", viewModel.FilterErrorText);
            Assert.Equal("FilterDone (1)", viewModel.FilterDoneText);
        }

        [Fact]
        public void RemoveSelectedCommand_RemovesGivenItems()
        {
            var viewModel = CreateViewModel(CreateTitleOnlySettings());
            var keep = new FileItem(@"C:\docs\keep.pdf");
            var remove1 = new FileItem(@"C:\docs\remove1.pdf");
            var remove2 = new FileItem(@"C:\docs\remove2.pdf");
            viewModel.SelectedFiles.Add(remove1);
            viewModel.SelectedFiles.Add(keep);
            viewModel.SelectedFiles.Add(remove2);

            viewModel.RemoveSelectedCommand.Execute(new List<object> { remove1, remove2 });

            Assert.Same(keep, Assert.Single(viewModel.SelectedFiles));
        }

        [Fact]
        public async Task ProcessAndUndo_RenamesAndRestoresFiles()
        {
            var directory = CreateTempDirectory();
            try
            {
                var originalPath = Path.Combine(directory, "scan001.pdf");
                File.WriteAllText(originalPath, "dummy");

                var viewModel = CreateViewModel(CreateTitleOnlySettings());
                var confirmations = 0;
                viewModel.ConfirmAction = (_, _) => { confirmations++; return true; };
                var item = new FileItem(originalPath);
                viewModel.SelectedFiles.Add(item);
                viewModel.ApplyManualName(item, "Manual Name");

                await InvokeProcessFilesAsync(viewModel);

                var renamedPath = Path.Combine(directory, "Manual Name.pdf");
                Assert.Equal("RenameComplete", item.Status);
                Assert.True(File.Exists(renamedPath));
                Assert.False(File.Exists(originalPath));
                Assert.Equal(renamedPath, item.CurrentFilePath);
                Assert.True(viewModel.CanUndo);

                viewModel.UndoCommand.Execute(null);

                Assert.True(File.Exists(originalPath));
                Assert.False(File.Exists(renamedPath));
                Assert.Equal(originalPath, item.CurrentFilePath);
                Assert.Equal("RenameScheduled", item.Status);
                Assert.False(viewModel.CanUndo);
                Assert.Equal(2, confirmations);
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        [Fact]
        public async Task Process_DoesNothingWhenConfirmationIsDeclined()
        {
            var directory = CreateTempDirectory();
            try
            {
                var originalPath = Path.Combine(directory, "scan002.pdf");
                File.WriteAllText(originalPath, "dummy");

                var viewModel = CreateViewModel(CreateTitleOnlySettings());
                viewModel.ConfirmAction = (_, _) => false;
                var item = new FileItem(originalPath);
                viewModel.SelectedFiles.Add(item);
                viewModel.ApplyManualName(item, "Other Name");

                await InvokeProcessFilesAsync(viewModel);

                Assert.True(File.Exists(originalPath));
                Assert.Equal("RenameScheduled", item.Status);
                Assert.False(viewModel.CanUndo);
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static FileNameSettings CreateTitleOnlySettings()
        {
            return new FileNameSettings
            {
                Elements = new ObservableCollection<FileNameElement>
                {
                    new("Title", true)
                }
            };
        }

        private static Task InvokeProcessFilesAsync(MainWindowViewModel viewModel)
        {
            var method = typeof(MainWindowViewModel).GetMethod("ProcessFilesAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            return Assert.IsAssignableFrom<Task>(method.Invoke(viewModel, null));
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

        private static string InvokeSanitizeFileNameWithNfkcControl(MainWindowViewModel viewModel, IReadOnlyList<FileNamePart> parts)
        {
            var method = typeof(MainWindowViewModel).GetMethod(
                "SanitizeFileNameWithNFKCControl",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            return Assert.IsType<string>(method.Invoke(viewModel, new object[] { parts }));
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
