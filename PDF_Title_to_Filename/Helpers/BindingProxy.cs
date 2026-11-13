using System.Windows;

namespace PdfTitleRenamer.Helpers
{
    /// <summary>
    /// DataGridColumn のようにビジュアルツリーに属さない要素から DataContext を参照するための中継オブジェクト。
    /// (DataGridColumn.Header に RelativeSource でバインドしても値が入らないため)
    /// </summary>
    internal sealed class BindingProxy : Freezable
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));

        public object? Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        protected override Freezable CreateInstanceCore() => new BindingProxy();
    }
}
