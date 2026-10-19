using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;

namespace PrintManagement.Controls.Table
{
    public partial class DataTable
    {
        private bool _suppressSelection;

        public object? SelectedItem
        {
            get => (RowsList.SelectedItem as TableRow)?.Item;
            set => RowsList.SelectedItem = value == null ? null : FindRow(value);
        }

        public event SelectionChangedEventHandler? SelectionChanged;

        private void RowsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelection) return;
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(Unwrap(e.RemovedItems), Unwrap(e.AddedItems)));
        }

        private static List<object> Unwrap(IList<object> rows) =>
            [.. rows.OfType<TableRow>().Select(r => r.Item)];

        private TableRow? FindRow(object item) => _originalUnsortedRows.FirstOrDefault(r => ReferenceEquals(r.Item, item));
    }
}
