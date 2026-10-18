using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using PrintManagement.Resources;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security;

namespace PrintManagement.Controls.Table;

public enum TableSortDirection
{
    None,
    Ascending,
    Descending
}

public partial class TableColumn : INotifyPropertyChanged
{
    private string _importedWidth = "Auto";
    private double _width = 150;
    private double? _maxAutoWidth;
    private bool _isVisible = true;
    private TableSortDirection _sortDirection = TableSortDirection.None;
    private DataTemplate? _generatedTemplate;
    internal DataTable? Owner { get; set; }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Raise([CallerMemberName] string? n = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    /// <summary>Text shown in the header.</summary>
    public string Header { get; set; } = "";

    /// <summary>Name of the row-item property to show (e.g. "Name"). Ignored if CellTemplate is set.</summary>
    public string Path { get; set; } = "";

    public double MinWidth { get; set; } = 80;

    /// <summary>Bold, primary-colored text (like the printer name).</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Optional: what to write for this column when exporting, instead of reading the property.</summary>
    public Func<object, object?>? ExportValue { get; set; }

    /// <summary>Optional custom cell. Its DataContext is the whole row item.</summary>
    public DataTemplate? CellTemplate { get; set; }

    public String Width
    {
        get => _importedWidth;
        set
        {
            if (!string.Equals(value, "Auto", StringComparison.OrdinalIgnoreCase) &&
        !double.TryParse(value, out _))
            {
                throw new ArgumentException($"Invalid column width value: '{value}'. Must be 'Auto' or a valid number.");
            }
            _importedWidth = value; Raise(); Raise(nameof(DisplayWidth));
        }
    }

    /// The actual width of the column, which may be different from Width if Width is "Auto"
    public double ActualWidth
    {
        get => _width;
        set
        {
            if (value < MinWidth) value = MinWidth;
            if (_width == value) return;
            _width = value;
            Raise();
            Raise(nameof(DisplayWidth));
        }
    }

    public double? MaxAutoWidth
    {
        get => _maxAutoWidth;
        set { _maxAutoWidth = value; Raise(); }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set { _isVisible = value; Raise(); Raise(nameof(DisplayWidth)); Raise(nameof(CellVisibility)); }
    }

    // what the header/cells actually bind to
    public double DisplayWidth => _isVisible ? _width : 0;
    public Visibility CellVisibility => _isVisible ? Visibility.Visible : Visibility.Collapsed;

    internal void Resize(double delta) => ActualWidth = Math.Max(MinWidth, _width + delta);

    // ---------- cell template ----------

    /// <summary>
    /// The template the cell actually uses: CellTemplate if given, otherwise a text
    /// template (a real XAML DataTemplate) bound to <see cref="Path"/>.
    /// </summary>
    public DataTemplate EffectiveTemplate => CellTemplate ?? (_generatedTemplate ??= BuildTextTemplate());

    private DataTemplate BuildTextTemplate()
    {
        // A binding path can't be a variable in static XAML, so the one-line
        // text template is loaded from XAML with this column's Path filled in.
        var text = string.IsNullOrWhiteSpace(Path)
            ? ""
            : $"Text=\"{{Binding Path={SecurityElement.Escape(Path)}}}\"";

        var look = IsPrimary
            ? "FontWeight=\"SemiBold\""
            : "Foreground=\"{ThemeResource TextFillColorSecondaryBrush}\"";

        var xaml =
            "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" +
            $"<TextBlock VerticalAlignment=\"Center\" TextTrimming=\"CharacterEllipsis\" {look} {text} />" +
            "</DataTemplate>";

        return (DataTemplate)XamlReader.Load(xaml);
    }

    // ---------- sorting ----------

    /// <summary>Set to false to make the header non-clickable.</summary>
    public bool IsSortable { get; set; } = true;

    /// <summary>
    /// Row-item property to sort by (dotted paths allowed, e.g. "Driver.Name").
    /// Defaults to <see cref="Path"/>; required for columns that use a CellTemplate.
    /// </summary>
    public string? SortPath { get; set; }

    /// <summary>Optional custom comparison of the two property values.</summary>
    public IComparer? Comparer { get; set; }

    internal string EffectiveSortPath => string.IsNullOrWhiteSpace(SortPath) ? Path : SortPath!;

    public bool CanSort => IsSortable && !string.IsNullOrWhiteSpace(EffectiveSortPath);

    /// <summary>
    /// Can also be set in XAML for an initial sort. Setting it (from code, XAML or a
    /// header click) makes the owning DataTable re-sort; only one column is sorted at a time.
    /// </summary>
    public TableSortDirection SortDirection
    {
        get => _sortDirection;
        set
        {
            if (_sortDirection == value) return;
            _sortDirection = value;
            Raise();
            Raise(nameof(SortGlyph));
            Raise(nameof(SortIndicatorVisibility));
        }
    }

    // header arrow (Segoe Fluent: ChevronUp / ChevronDown)
    public string SortGlyph => _sortDirection == TableSortDirection.Descending ? Icons.ChevronUp.Glyph : Icons.ChevronDown.Glyph;

    public Visibility SortIndicatorVisibility =>
        _sortDirection == TableSortDirection.None ? Visibility.Collapsed : Visibility.Visible;

    // ---------- reordering ----------

    /// <summary>Set to false to stop this column's header from being dragged.</summary>
    public bool CanReorder { get; set; } = true;

    private bool _isBeingDragged;
    private ColumnDropPosition _dropPosition = ColumnDropPosition.None;

    internal bool IsBeingDragged
    {
        get => _isBeingDragged;
        set
        {
            if (_isBeingDragged == value) return;
            _isBeingDragged = value;
            Raise(nameof(HeaderOpacity));
        }
    }

    internal ColumnDropPosition DropPosition
    {
        get => _dropPosition;
        set
        {
            if (_dropPosition == value) return;
            _dropPosition = value;
            Raise(nameof(DropBeforeVisibility));
            Raise(nameof(DropAfterVisibility));
        }
    }

    // what the header cell binds to
    public double HeaderOpacity => _isBeingDragged ? 0.4 : 1.0;

    public Visibility DropBeforeVisibility =>
        _dropPosition == ColumnDropPosition.Before ? Visibility.Visible : Visibility.Collapsed;

    public Visibility DropAfterVisibility =>
        _dropPosition == ColumnDropPosition.After ? Visibility.Visible : Visibility.Collapsed;
}

internal enum ColumnDropPosition
{
    None,
    Before,
    After
}

public partial class TableColumnCollection : ObservableCollection<TableColumn> { }