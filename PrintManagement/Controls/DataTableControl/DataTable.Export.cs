using PrintManagement.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PrintManagement.Controls.DataTableControl;

public partial class DataTable
{
    private readonly IExportService _exportService;

    public async void Receive(ExportCsvRequestMessage message)
    {
        try { await ExportCsvAsync(message.PageName); }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            Logger.Error(ex);
        }
    }

    public async Task ExportCsvAsync(string pageName)
    {
        await _exportService.ExportAsync(
            Columns,
            Rows,
            col => col.Header,
            (row, col) => GetCellValue(row.Item, col),
            col => col.IsVisible,
            col => col.Path,
            pageName
        );
    }

    private static object? GetCellValue(object item, TableColumn col)
    {
        if (col.ExportValue is { } export)
            return export(item);

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
