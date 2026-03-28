using System.Reflection;

namespace PdfTitleRenamer.Helpers
{
    internal static class VersionHelper
    {
        public static string GetVersion()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version?.ToString() ?? "1.0.6";
            }
            catch
            {
                return "1.0.6";
            }
        }

        public static string GetFileVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
                return fileVersionInfo.FileVersion ?? "1.0.6";
            }
            catch
            {
                return "1.0.6";
            }
        }
    }
}
