using PdfTitleRenamer.Services;
using Xunit;

namespace PDF_Title_to_Filename.Tests
{
    public class LanguageServiceTests
    {
        [Fact]
        public void GetString_ReturnsCorrectJapaneseString()
        {
            // Arrange
            var service = new LanguageService();
            service.SetLanguage("ja");

            // Act
            var result = service.GetString("FileSelection");

            // Assert
            Assert.Equal("ファイル選択", result);
        }

        [Fact]
        public void GetString_ReturnsCorrectEnglishString()
        {
            // Arrange
            var service = new LanguageService();
            service.SetLanguage("en");

            // Act
            var result = service.GetString("FileSelection");

            // Assert
            Assert.Equal("Select Files", result);
        }

        [Fact]
        public void GetString_ReturnsKey_WhenKeyNotFound()
        {
            // Arrange
            var service = new LanguageService();
            var nonExistentKey = "NonExistentKey_12345";

            // Act
            var result = service.GetString(nonExistentKey);

            // Assert
            Assert.Equal(nonExistentKey, result);
        }
    }
}
