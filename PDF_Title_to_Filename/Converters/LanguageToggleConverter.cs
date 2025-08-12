using System;
using System.Globalization;
using System.Windows.Data;
using System.IO;

namespace PdfTitleRenamer.Converters
{
    public class LanguageToggleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string currentLanguageDisplay)
            {
                return currentLanguageDisplay == "Japanese" ? "en" : "ja";
            }
            return "en"; // デフォルト
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
