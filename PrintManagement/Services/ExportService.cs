using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace PrintManagement.Services;

public interface IExportService
{
    Task ExportAsync<TColumn, TRow>(
        IEnumerable<TColumn> columns,
        IEnumerable<TRow> rows,
        Func<TColumn, string> headerSelector,
        Func<TRow, TColumn, object?> valueSelector,
        Func<TColumn, bool> isVisibleSelector,
        string pageName);
}

public class ExportService : IExportService
{
    /// <summary>The data every format needs, already converted to strings.</summary>
    private readonly record struct Table(string[] Headers, List<string[]> Rows);

    public async Task ExportAsync<TColumn, TRow>(
        IEnumerable<TColumn> columns,
        IEnumerable<TRow> rows,
        Func<TColumn, string> headerSelector,
        Func<TRow, TColumn, object?> valueSelector,
        Func<TColumn, bool> isVisibleSelector,
        string pageName)
    {
        var filePath = await PickSaveFilePathAsync(pageName);
        if (string.IsNullOrEmpty(filePath)) return;

        // Only the columns the user can see.
        var visibleColumns = columns.Where(isVisibleSelector).ToList();
        if (visibleColumns.Count == 0) return;

        var rowsList = rows.ToList();

        Action<Table, string> write = Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".csv" => WriteCsv,
            ".md" => WriteMarkdown,
            ".txt" => WriteText,
            var ext => throw Unsupported(ext)
        };

        await Task.Run(() =>
        {
            // The only place that touches the selectors: one pass, one formatting rule.
            var table = new Table(
                visibleColumns.Select(c => headerSelector(c) ?? string.Empty).ToArray(),
                rowsList.Select(r => visibleColumns.Select(c => ToCell(valueSelector(r, c))).ToArray()).ToList());

            write(table, filePath);
        });
    }

    // ───────────── Shared helpers ─────────────

    private static string ToCell(object? value) =>
        Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>Keeps one record on one line for the line-based formats (md, txt).</summary>
    private static string Flatten(string s) =>
        s.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ');

    private static NotSupportedException Unsupported(string extension)
    {
        var ex = new NotSupportedException($"File extension '{extension}' is not supported for export.");
        Logger.Error(ex);
        return ex;
    }

    // ───────────── Writers: one per format, strings only ─────────────
    /// <summary>
    /// Writes the table as CSV with a header row. Quoting is handled by CsvHelper.
    /// </summary>
    /// <param name="table">The headers and rows to write, already converted to strings.</param>
    /// <param name="filePath">The file to create. An existing file is overwritten.</param>
    private static void WriteCsv(Table table, string filePath)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        foreach (var header in table.Headers)
            csv.WriteField(header);
        csv.NextRecord();

        foreach (var row in table.Rows)
        {
            foreach (var cell in row)
                csv.WriteField(cell);
            csv.NextRecord();
        }
    }

    /// <summary>
    /// Writes the table as tab-separated text, one record per line, with a header line first.
    /// Working the same as the .txt export from the original Print Management.
    /// </summary>
    /// <param name="table">The headers and rows to write, already converted to strings.</param>
    /// <param name="filePath">The file to create. An existing file is overwritten.</param>
    private static void WriteText(Table table, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join("\t", table.Headers.Select(Flatten)));
        foreach (var row in table.Rows)
            sb.AppendLine(string.Join("\t", row.Select(Flatten)));

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>
    /// Writes the table as a Markdown table with aligned columns. Pipes in values are escaped.
    /// </summary>
    /// <param name="table">The headers and rows to write, already converted to strings.</param>
    /// <param name="filePath">The file to create. An existing file is overwritten.</param>
    private static void WriteMarkdown(Table table, string filePath)
    {
        // A pipe inside a cell would split it into two columns.
        static string Escape(string s) => Flatten(s).Replace("|", "\\|");

        var headers = table.Headers.Select(Escape).ToArray();
        var rows = table.Rows.Select(r => r.Select(Escape).ToArray()).ToList();

        // Column widths, so the raw file lines up in an editor.
        int[] widths = [.. headers.Select((h, i) => Math.Max(3, rows.Select(r => r[i].Length).Prepend(h.Length).Max()))];

        string Line(IEnumerable<string> cells) =>
            "| " + string.Join(" | ", cells.Select((c, i) => c.PadRight(widths[i]))) + " |";

        var sb = new StringBuilder();
        sb.AppendLine(Line(headers));
        sb.AppendLine(Line(widths.Select(w => new string('-', w))));
        foreach (var row in rows)
            sb.AppendLine(Line(row));

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    // ───────────── File picker ─────────────

    private static async Task<string?> PickSaveFilePathAsync(string pageName)
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
        savePicker.FileTypeChoices.Add("CSV File (*.csv)", [".csv"]);
        savePicker.FileTypeChoices.Add("Markdown Document (*.md)", [".md"]);
        savePicker.FileTypeChoices.Add("Text File (*.txt)", [".txt"]);

        savePicker.SuggestedFileName = $"{pageName}_Export_{DateTime.Now:yyyyMMdd_HHmmss}";

        StorageFile file = await savePicker.PickSaveFileAsync();
        return file?.Path;
    }
}

public class ExportCsvRequestMessage
{
    public string PageName { get; set; } = string.Empty;
}