using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace PrintManagement.Services;

public interface IExportService
{
    Task ExportToCsvAsync<TColumn, TRow>(
        IEnumerable<TColumn> columns,
        IEnumerable<TRow> rows,
        Func<TColumn, string> headerSelector,
        Func<TRow, TColumn, object?> valueSelector,
        string pageName);
}

public class ExportService : IExportService
{
    public async Task ExportToCsvAsync<TColumn, TRow>(
        IEnumerable<TColumn> columns,
        IEnumerable<TRow> rows,
        Func<TColumn, string> headerSelector,
        Func<TRow, TColumn, object?> valueSelector,
        string pageName)
    {
        var filePath = await PickSaveFilePathAsync(pageName);
        if (string.IsNullOrEmpty(filePath)) return;

        await Task.Run(() =>
        {
            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            var colsList = columns.ToList();

            foreach (var col in colsList)
                csv.WriteField(headerSelector(col));
            csv.NextRecord();

            foreach (var row in rows)
            {
                foreach (var col in colsList)
                    csv.WriteField(valueSelector(row, col));
                csv.NextRecord();
            }
        });
    }

    private async Task<string?> PickSaveFilePathAsync(string pageName)
    {
        var savePicker = new FileSavePicker();

        var window = App.MainWindow;
        if (window == null)
        {
            Logger.Error(new InvalidOperationException("Main window is not available."));
            throw new InvalidOperationException("Main window is not available.");
        }

        var hWnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(savePicker, hWnd);

        savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        savePicker.FileTypeChoices.Add("CSV File", new List<string>() { ".csv" });
        savePicker.SuggestedFileName = $"{pageName}_Export_{DateTime.Now:yyyyMMdd_HHmmss}";

        var file = await savePicker.PickSaveFileAsync();
        return file?.Path;
    }
}

public class ExportCsvRequestMessage
{
    public string PageName { get; set; } = string.Empty;
}