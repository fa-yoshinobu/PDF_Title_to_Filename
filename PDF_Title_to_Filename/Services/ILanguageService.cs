namespace PdfTitleRenamer.Services
{
    internal interface ILanguageService
    {
        string GetString(string key);
        void SetLanguage(string languageCode);
        string CurrentLanguage { get; }
        event EventHandler? LanguageChanged;
    }
}
