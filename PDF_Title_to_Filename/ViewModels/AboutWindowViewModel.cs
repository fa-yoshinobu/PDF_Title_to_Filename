using System.ComponentModel;
using System.Runtime.CompilerServices;
using PdfTitleRenamer.Services;
using System.Collections.Generic;

namespace PdfTitleRenamer.ViewModels
{
    public class AboutWindowViewModel : INotifyPropertyChanged
    {
        private readonly ILanguageService _languageService;

        public AboutWindowViewModel(ILanguageService languageService)
        {
            _languageService = languageService;

            // 言語変更イベントを購読
            _languageService.LanguageChanged += (s, e) => {
                OnPropertyChanged(string.Empty);
            };

            // 初期化
            _licenseLabel = _languageService.GetString("LicenseLabel");
        }

                            // UI文字列プロパティ
                    public string AboutWindowTitle => _languageService.GetString("AboutWindowTitle");
                    public string AppDescription => _languageService.GetString("AppDescription");
                    public string AboutTab => _languageService.GetString("AboutTab");
                    public string LicenseTab => _languageService.GetString("LicenseTab");
                    public string AppInfoHeader => _languageService.GetString("AppInfoHeader");
                    public string VersionLabel => _languageService.GetString("VersionLabel");
                    public string AuthorLabel => _languageService.GetString("AuthorLabel");
                    public string LinkLabel => _languageService.GetString("LinkLabel");
                    public string DevelopmentLanguageLabel => _languageService.GetString("DevelopmentLanguageLabel");
                    public string UIFrameworkLabel => _languageService.GetString("UIFrameworkLabel");
                    public string DesignLabel => _languageService.GetString("DesignLabel");
                    public string SupportedOSLabel => _languageService.GetString("SupportedOSLabel");
                    public string MainFeaturesHeader => _languageService.GetString("MainFeaturesHeader");
                    public string Feature1 => _languageService.GetString("Feature1");
                    public string Feature2 => _languageService.GetString("Feature2");
                    public string Feature3 => _languageService.GetString("Feature3");
                    public string Feature4 => _languageService.GetString("Feature4");
                    public string Feature5 => _languageService.GetString("Feature5");
                    public string Feature6 => _languageService.GetString("Feature6");
                    public string UsageHeader => _languageService.GetString("UsageHeader");
                    public string Usage1 => _languageService.GetString("Usage1");
                    public string Usage2 => _languageService.GetString("Usage2");
                    public string Usage3 => _languageService.GetString("Usage3");
                                         public string Usage4 => _languageService.GetString("Usage4");
                     
                     // Value properties for AboutWindow
                     public string VersionValue => "1.0.3";
                     public string AuthorValue => "fa-yoshinobu";
                     public string LinkValue => "https://github.com/fa-yoshinobu/PDF_Title_to_Filename";
                     public string DevelopmentLanguageValue => _languageService.GetString("DevelopmentLanguageValue");
                     public string UIFrameworkValue => _languageService.GetString("UIFrameworkValue");
                     public string DesignValue => _languageService.GetString("DesignValue");
                     public string SupportedOSValue => _languageService.GetString("SupportedOSValue");
                     
                     private string _licenseLabel = "";
                     public string LicenseLabel
                     {
                         get => _licenseLabel;
                         set => SetProperty(ref _licenseLabel, value);
                     }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
