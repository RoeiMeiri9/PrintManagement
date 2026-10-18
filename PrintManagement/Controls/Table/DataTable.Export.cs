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
        await _exportService.ExportToCsvAsync(
            Columns,
            Rows,
            col => col.Header,
            (row, col) => GetCellValue(row.Item, col),
            pageName
        );
    }

    private static object? GetCellValue(object item, TableColumn col)
    {
        // 1. קביעת ה-Property Path (מעדיפים Path, ואם ריק משתמשים ב-SortPath)
        var propertyPath = !string.IsNullOrWhiteSpace(col.Path) ? col.Path : col.SortPath;

        if (string.IsNullOrWhiteSpace(propertyPath))
            return null;

        // 2. קריאת הערך האמיתי מהמודל באמצעות ה-ValueReader
        var value = ValueReader.Read(item, propertyPath);

        // 3. טיפול מיוחד באוספים/רשימות (כמו QueueStatus) כדי שלא ירנדרו כ-System.Collections.Generic...
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
