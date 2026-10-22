using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PrintManagement.Models.Statics;
using PrintManagement.Resources;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PrintManagement.Controls.IconGridViewControl;

public sealed partial class ItemModel(string name, Icons icon) : INotifyPropertyChanged
{
    public string Name { get; } = name;
    public Icons Icon { get; } = icon;

    private double _iconSize = StaticValues.SmallIcons;
    public double IconSize
    {
        get => _iconSize;
        set { if (_iconSize != value) { _iconSize = value; OnPropertyChanged(); } }
    }

    private Thickness _padding = new(10, 8, 10, 8);
    public Thickness Padding
    {
        get => _padding;
        set { if (_padding != value) { _padding = value; OnPropertyChanged(); } }
    }

    private Orientation _orientation = Orientation.Vertical;
    public Orientation Orientation
    {
        get => _orientation;
        set { if (_orientation != value) { _orientation = value; OnPropertyChanged(); } }
    }

    private HorizontalAlignment _alignment = HorizontalAlignment.Center;
    public HorizontalAlignment Alignment
    {
        get => _alignment;
        set { if (_alignment != value) { _alignment = value; OnPropertyChanged(); } }
    }

    private TextAlignment _textAlignment = TextAlignment.Center;
    public TextAlignment TextAlignment
    {
        get => _textAlignment;
        set { if (_textAlignment != value) { _textAlignment = value; OnPropertyChanged(); } }
    }

    private double _tileWidth = double.NaN;
    public double TileWidth
    {
        get => _tileWidth;
        set { if (_tileWidth != value) { _tileWidth = value; OnPropertyChanged(); } }
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BackgroundBrush));
            OnPropertyChanged(nameof(BorderBrush));
        }
    }

    private bool _isActive;   // true = control "has the user's attention": show the fill too
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value) return;
            _isActive = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BackgroundBrush));
        }
    }

    private bool _isHovered;   // true = control "has the user's attention": show the fill too
    public bool IsHovered
    {
        get => _isHovered;
        set
        {
            if (_isHovered == value) return;
            _isHovered = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BackgroundBrush));
        }
    }
    private static readonly bool isDarkMode = App.Current.RequestedTheme == ApplicationTheme.Dark;

    private static readonly Brush TransparentBrush = new SolidColorBrush(Colors.Transparent);
    private static readonly Brush Rest = (Brush)Application.Current.Resources["SubtleFillColorTertiaryBrush"];
    private static readonly Brush Hover = (Brush)Application.Current.Resources["SubtleFillColorTertiaryBrush"];
    private static readonly Brush Pressed = (Brush)Application.Current.Resources[isDarkMode ? "ControlFillColorDefaultBrush" : "SubtleFillColorSecondaryBrush"];
    private static readonly Brush SelectedBorderBrush = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];

    public Brush BackgroundBrush
    {
        get
        {
            if (_isHovered)
                return _isActive ? Hover : Pressed;

            if (_isActive)
                return Pressed;

            return TransparentBrush;
        }
    }
    public Brush BorderBrush => _isSelected ? SelectedBorderBrush : TransparentBrush;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public partial class ItemModelCollection : System.Collections.ObjectModel.ObservableCollection<ItemModel> { }