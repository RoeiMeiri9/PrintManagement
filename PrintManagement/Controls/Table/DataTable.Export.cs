using PrintManagement.Services;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrintManagement.Controls.Table;

public partial class DataTable
{
    private readonly IExportService _exportService;

    public async void Receive(ExportCsvRequestMessage message)
    {
        await ExportCsvAsync(message.PageName);
    }

    public async Task ExportCsvAsync(string pageName)
    {
        await _exportService.ExportAsync(
            Columns,
            Rows,
            col => col.Header,
            (row, col) => GetCellValue(row.Item, col),
            col => col.IsVisible,
            pageName
        );
    }

    private static object? GetCellValue(object item, TableColumn col)
    {
        var propertyPath = !string.IsNullOrWhiteSpace(col.Path) ? col.Path : col.SortPath;

        if (string.IsNullOrWhiteSpace(propertyPath))
            return null;

        var value = ValueReader.Read(item, propertyPath);

        if (value is IEnumerable list && value is not string)
        {
            var items = new List<string>();
            foreach (var element in list)
            {
                var statusText = ValueReader.Read(element, "Status")?.ToString() ?? element?.ToString();
                if (!string.IsNullOrEmpty(statusText)) items.Add(statusText);
            }
            return string.Join(", ", items);
        }

        return value;
    }
}
