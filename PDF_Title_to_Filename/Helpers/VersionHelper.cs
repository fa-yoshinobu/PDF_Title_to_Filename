using System.Reflection;

namespace PdfTitleRenamer.Helpers
{
    public static class VersionHelper
    {
        public static string GetVersion()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version?.ToString() ?? "1.0.2";
            }
            catch
            {
                return "1.0.2";
            }
        }

        public static string GetFileVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
                return fileVersionInfo.FileVersion ?? "1.0.2";
            }
            catch
            {
                return "1.0.2";
            }
        }
    }
}
