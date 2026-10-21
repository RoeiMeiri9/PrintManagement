using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PrintManagement.Models.enums;
using System.Collections.Specialized;

namespace PrintManagement.Controls.IconGridViewControl;

public sealed partial class IconGridView : UserControl
{
    #region Dependency Propreties
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(object),
            typeof(IconGridView),
            new PropertyMetadata(null, (d, e) => (d as IconGridView)?.OnItemsSourceChanged()));

    public object ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty ViewTypeProperty =
        DependencyProperty.Register(
            nameof(ViewType),
            typeof(ViewMode),
            typeof(IconGridView),
            new PropertyMetadata(ViewMode.Tiles, (d, e) => (d as IconGridView)?.OnViewTypeChanged((ViewMode)e.NewValue)));

    public ViewMode ViewType
    {
        get => (ViewMode)GetValue(ViewTypeProperty);
        set => SetValue(ViewTypeProperty, value);
    }

    public static readonly DependencyProperty CompactViewProperty =
       DependencyProperty.Register(
           nameof(CompactView),
           typeof(bool),
           typeof(IconGridView),
           new PropertyMetadata(false, (d, e) => (d as IconGridView)?.OnCompactViewChanged((bool)e.NewValue)));

    public bool CompactView
    {
        get => (bool)GetValue(CompactViewProperty);
        set => SetValue(CompactViewProperty, value);
    }


    private INotifyCollectionChanged? _observed;

    private static readonly Thickness DefaultPadding = new(10, 8, 10, 8);
    private static readonly Thickness ShrinkedPadding = new(10, 6, 10, 6);

    #endregion

    public IconGridView()
    {
        InitializeComponent();
        Loaded += IconGridView_Loaded_Layout;
        Unloaded += IconGridView_Unloaded_Layout;
        Loaded += IconGridView_Loaded_Focus;
        Unloaded += IconGridView_Unloaded_Focus;
    }
}