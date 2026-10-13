
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using PrinterManager.Helpers.WindowHelpers;
using PrinterManager.Pages;
using PrinterManager.Pages.SettingsPage;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Foundation;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PrinterManager
{
    public sealed partial class MainWindow : Window
    {
        public string AppTitle => Windows.ApplicationModel.Package.Current.DisplayName;
        private const int MinWindowWidth = 800;
        private const int MinWindowHeight = 600;

        private const double CompactWidth = 641.2;

        private bool? _isCompact;          // null = not applied yet
        private bool _restoreSearchFocus;

        public MainWindow()
        {
            InitializeComponent();
            InitializeWindow();
            InitializeFrame();
        }


        private void InitializeWindow()
        {
            ExtendsContentIntoTitleBar = true;
            Title = AppTitle;
            SetTitleBar(titleBar);
            SetIcon();
            this.SetMinimumSize(MinWindowWidth, MinWindowHeight);
            SizeChanged += MainWindow_SizeChanged;
        }

        private void SetIcon()
        {
            string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "IconGroup51.ico");
            if (File.Exists(iconPath))
                AppWindow.SetIcon(iconPath);
            else
                System.Diagnostics.Debug.WriteLine($"Icon not found: {iconPath}");
        }

        private void InitializeFrame()
        {
            navFrame.Navigate(typeof(CustomFiltersPage));
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

            DispatcherQueue.TryEnqueue(() =>
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
            }
            else
            {
                var selectedItem = args.SelectedItem as NavigationViewItem;
                if (selectedItem != null)
                {
                    switch (selectedItem.Name.ToString())
                    {
                        case "CustomFilters":
                            navFrame.Navigate(typeof(CustomFiltersPage));
                            break;
                    }
                }
            }
        }
    }
}