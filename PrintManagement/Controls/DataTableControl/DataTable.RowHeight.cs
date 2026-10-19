using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PrintManagement.Controls.DataTableControl
{
    public partial class DataTable
    {
        public static readonly DependencyProperty CompactViewProperty =
        DependencyProperty.Register(
            nameof(CompactView),
            typeof(bool),
            typeof(DataTable),
            new PropertyMetadata(false, (d, e) => ((DataTable)d).OnCompactViewChanged(e.NewValue)));

        public bool CompactView
        {
            get => (bool)GetValue(CompactViewProperty);
            set => SetValue(CompactViewProperty, value);
        }

        private Style? _originalItemStyle;

        private RoutedEventHandler OnTableLoaded_ApplyCompactViewStyle()
        {
            return (sender, e) =>
            {
                ApplyCompactViewStyle(CompactView);
            };
        }

        private void ApplyCompactViewStyle(bool compact)
        {
            if (RowsList == null) return;

            _originalItemStyle ??= RowsList.ItemContainerStyle;

            var updatedStyle = new Style(typeof(ListViewItem))
            {
                BasedOn = _originalItemStyle
            };

            updatedStyle.Setters.Add(new Setter(Control.PaddingProperty, compact ? new Thickness(4) : new Thickness(4, 6, 4, 6)));

            RowsList.ItemContainerStyle = updatedStyle;
        }

        private void OnCompactViewChanged(object? newValue)
        {
            if (newValue is bool compact)
            {
                ApplyCompactViewStyle(compact);
            }
        }
    }
}
