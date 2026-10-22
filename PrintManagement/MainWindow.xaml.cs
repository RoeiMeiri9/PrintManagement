using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using PrintManagement.Helpers.WindowHelpers;
using PrintManagement.Pages.CommonPageWrapper;
using PrintManagement.Pages.SettingsPage;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PrintManagement;

public sealed partial class MainWindow : Window, INotifyPropertyChanged
{
    public string AppTitle => Windows.ApplicationModel.Package.Current.DisplayName;
    private const int MinWindowWidth = 800;
    private const int MinWindowHeight = 600;

    private const double CompactWidth = 641.2;

    private bool? _isCompact;          // null = not applied yet
    private bool _restoreSearchFocus;

    private readonly static Thickness NoMarginOnTitle = new(0);
    private readonly static Thickness SmallMarginOnTitle = new(2, 0, 0, 0);

    private Thickness _titleMargin = new(0, 0, 0, 0);
    public Thickness TitleMargin
    {
        get => _titleMargin;
        set
        {
            _titleMargin = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TitleMargin)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow()
    {
        InitializeComponent();
        InitializeWindow();
    }

    private void InitializeWindow()
    {
        SetBackground();
        Title = AppTitle;
        SetTitleBar(titleBar);
        SetIcon();
        this.SetMinimumSize(MinWindowWidth, MinWindowHeight);
        SizeChanged += MainWindow_SizeChanged;
    }

    private void SetBackground()
    {
        ExtendsContentIntoTitleBar = true;
        if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
        {
            SystemBackdrop = new MicaBackdrop();
        }
        else
        {
            SystemBackdrop = new DesktopAcrylicBackdrop();
            MainGrid.Background = new SolidColorBrush(Color.FromArgb(60, 56, 56, 56)); // Dark gray background for acrylic
            ContentGrid.Background = new SolidColorBrush(Color.FromArgb(10, 250, 250, 250)); // Dark gray background for acrylic
        }
    }
    private void SetIcon()
    {
        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "IconGroup51.ico");
        if (File.Exists(iconPath))
            AppWindow.SetIcon(iconPath);
        else
            System.Diagnostics.Debug.WriteLine($"Icon not found: {iconPath}");
    }


    // ---------------------------------------- //
    //              Event Handlers              //
    // ---------------------------------------- //


    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        navView.IsPaneOpen = !navView.IsPaneOpen;
    }


    private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
    {
        bool compact = args.Size.Width <= CompactWidth;

        if (compact == _isCompact)
            return; // still on the same side of the threshold

        _isCompact = compact;
        ApplyLayout(compact);
    }

    private void ApplyLayout(bool compact)
    {
        navView.IsPaneToggleButtonVisible = !compact;
        titleBar.IsPaneToggleButtonVisible = compact;
        CustomAppTitle.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;

        if (compact)
        {
            // Remember focus BEFORE hiding anything
            _restoreSearchFocus = IsSearchFocused();
            ToggleSearchBox(false);
        }
        else
        {
            ToggleSearchBox(true, focus: _restoreSearchFocus);
            _restoreSearchFocus = false;
        }

    }

    private bool IsSearchFocused()
    {
        if (TitleBarSearchBox.Visibility != Visibility.Visible)
            return false;

        DependencyObject? current =
            FocusManager.GetFocusedElement(Content.XamlRoot) as DependencyObject;

        while (current != null)
        {
            if (current == TitleBarSearchBox) return true;
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    private void SearchIconButton_Click(object s, RoutedEventArgs e) => ToggleSearchBox(true, focus: true);

    private void TitleBarSearchBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isCompact == true && string.IsNullOrWhiteSpace(TitleBarSearchBox.Text))
        {
            ToggleSearchBox(false);
        }
    }

    private void ToggleSearchBox(bool expanded, bool focus = false)
    {
        SearchIconButton.Visibility = expanded ? Visibility.Collapsed : Visibility.Visible;
        TitleBarSearchBox.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
        SearchColumn.Width = expanded ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

        TitleBarSearchBoxContainer.UpdateLayout();

        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            UpdatePassthroughRegions();
            if (expanded && focus)
                TitleBarSearchBox.Focus(FocusState.Programmatic);
        });
    }

    private void UpdatePassthroughRegions()
    {
        var scale = Content.XamlRoot.RasterizationScale;
        var rects = new List<RectInt32>();

        void Add(FrameworkElement? el)
        {
            if (el is null || el.Visibility != Visibility.Visible || el.ActualWidth == 0) return;
            var b = el.TransformToVisual(null)
                      .TransformBounds(new Rect(0, 0, el.ActualWidth, el.ActualHeight));
            rects.Add(new RectInt32(
                (int)Math.Round(b.X * scale),
                (int)Math.Round(b.Y * scale),
                (int)Math.Round(b.Width * scale),
                (int)Math.Round(b.Height * scale)));
        }

        Add(SearchIconButton);
        Add(TitleBarSearchBox);

        var source = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
        source.SetRegionRects(NonClientRegionKind.Passthrough, rects.ToArray());
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            navFrame.Navigate(typeof(SettingsPage));
            TitleMargin = SmallMarginOnTitle;
        }
        else if (args.SelectedItem is NavigationViewItem selectedItem)
        {
            bool navigated = selectedItem.Name switch
            {
                "CustomFilters" => navFrame.Navigate(typeof(CommonPageWrapper)),
                _ => false
            };

            if (navigated)
            {
                TitleMargin = NoMarginOnTitle;
                navFrame.BackStack.Clear();
            }
        }
    }
    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        if (navFrame.CanGoBack)
        {
            var previousPageType = navFrame.BackStack.LastOrDefault()?.SourcePageType;
            TitleMargin = NoMarginOnTitle;
            navFrame.GoBack();

            navView.SelectedItem = previousPageType?.Name switch
            {
                nameof(SettingsPage) => navView.SettingsItem,
                nameof(CommonPageWrapper) => CustomFilters,
                _ => navView.SelectedItem
            };
        }
    }
}