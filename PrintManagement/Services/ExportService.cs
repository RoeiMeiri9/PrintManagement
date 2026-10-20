using CsvHelper;
using PrintManagement.Resources;
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
        Func<TColumn, string?> columnPathSelector,
        string pageName);
}

public class ExportService : IExportService
{
    /// <summary>
    /// What a value selector returns for the Share Name of an unshared printer.
    /// The export blanks it out when the "show &lt;Not Shared&gt;" option is off.
    /// </summary>
    public const string NotSharedPlaceholder = "<Not Shared>";

    /// <summary>The data every format needs, already converted to strings.</summary>
    private readonly record struct Table(string[] Headers, List<string[]> Rows);

    /// <summary>The export settings, read once per export.</summary>
    private readonly record struct CellOptions(bool IsoDates, bool LowercaseBools, bool ShowNotShared);

    // CSV gets a BOM so Excel detects UTF-8; text and Markdown don't.
    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public async Task ExportAsync<TColumn, TRow>(
        IEnumerable<TColumn> columns,
        IEnumerable<TRow> rows,
        Func<TColumn, string> headerSelector,
        Func<TRow, TColumn, object?> valueSelector,
        Func<TColumn, bool> isVisibleSelector,
        Func<TColumn, string?> columnPathSelector,
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

        // Read the settings once, not once per cell.
        var options = new CellOptions(
            IsoDates: SettingsService.ReadSetting(Settings<bool>.UseISODateFormatSetting),
            LowercaseBools: SettingsService.ReadSetting(Settings<bool>.WriteTrueFalseLowercaseSetting),
            ShowNotShared: SettingsService.ReadSetting(Settings<bool>.ShowNotSharedSetting));

        // TODO: Remove this log once the export is working well. It's just for debugging.
        Logger.Log($"export options: iso={options.IsoDates}, lowercase={options.LowercaseBools}, showNotShared={options.ShowNotShared}");

        await Task.Run(() =>
        {
            // The only place that touches the selectors: one pass, one formatting rule.
            var table = new Table(
                [.. visibleColumns.Select(c => headerSelector(c) ?? string.Empty)],
                [.. rowsList.Select(r => visibleColumns.Select(c => ToCell(valueSelector(r, c), columnPathSelector(c), options)).ToArray())]);

            write(table, filePath);
        });
    }

    // ───────────── Shared helpers ─────────────

    private static string ToCell(object? value, object? path, CellOptions options) => value switch
    {
        null => string.Empty,
        DateTime dt => FormatDate(dt, options.IsoDates),
        bool b => options.LowercaseBools ? (b ? "true" : "false") : (b ? "True" : "False"),
        string when path != null && path.ToString() == "ShareName" && string.IsNullOrEmpty(value.ToString()) => options.ShowNotShared ? NotSharedPlaceholder : string.Empty,
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
    };

    /// <summary>
    /// ISO: 2006-06-21 (the same on every computer). Otherwise the date as Windows shows it
    /// for the current regional settings, such as 21/06/2006. The time is added only when it isn't midnight.
    /// </summary>
    private static string FormatDate(DateTime dt, bool iso)
    {
        bool dateOnly = dt.TimeOfDay == TimeSpan.Zero;
        return iso
            ? dt.ToString(dateOnly ? "yyyy-MM-dd" : "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            : dt.ToString(dateOnly ? "d" : "G", CultureInfo.CurrentCulture);
    }

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
        using var writer = new StreamWriter(filePath, false, Utf8WithBom);
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

        File.WriteAllText(filePath, sb.ToString(), Utf8NoBom);
    }

    /// <summary>
    /// Writes the table as a Markdown table with aligned columns. Pipes in values are escaped.
    /// </summary>
    /// <param name="table">The headers and rows to write, already converted to strings.</param>
    /// <param name="filePath">The file to create. An existing file is overwritten.</param>
    private static void WriteMarkdown(Table table, string filePath)
    {
        // A pipe would split a cell in two; a "<" would be read as an HTML tag and hidden.
        static string Escape(string s) => Flatten(s).Replace("|", "\\|").Replace("<", "\\<");

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

        File.WriteAllText(filePath, sb.ToString(), Utf8NoBom);
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