using System.Windows;

namespace PdfTitleRenamer.Views
{
    /// <summary>
    /// AboutWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        public AboutWindow(Window owner)
        {
            InitializeComponent();
            Owner = owner;
        }
    }
}
