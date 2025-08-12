namespace PdfTitleRenamer.Services
{
    public interface ILanguageService
    {
        string GetString(string key);
        void SetLanguage(string languageCode);
        string CurrentLanguage { get; }
        event EventHandler? LanguageChanged;
    }
}
