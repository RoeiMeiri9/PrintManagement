using Microsoft.UI.Dispatching;
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

        private static readonly Thickness lowPadding = new(8, 6, 8, 6);
        private static readonly Thickness highPadding = new(8);

        private void ItemsControl_Loaded(object sender, RoutedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                ApplyCompactViewStyle(CompactView);
            });
        }

        private void ApplyCompactViewStyle(bool compact)
        {

            Thickness targetPadding = compact ? lowPadding : highPadding;

            if (RowsList != null)
            {
                for (int i = 0; i < RowsList.Items.Count; i++)
                {
                    if (RowsList.ContainerFromIndex(i) is ListViewItem container)
                    {
                        container.Padding = targetPadding;
                    }
                }
            }
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
