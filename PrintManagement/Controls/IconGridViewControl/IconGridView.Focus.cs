using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace PrintManagement.Controls.IconGridViewControl;

public sealed partial class IconGridView
{
    private ItemModel? _selectedItem;
    private UIElement? _root;
    private bool _keyboardNavigating;

    private void IconGridView_Loaded_Focus(object sender, RoutedEventArgs e)
    {
        _root = XamlRoot?.Content;
        // handledEventsToo = true, so ScrollViewer / buttons can't swallow the press
        _root?.AddHandler(PointerPressedEvent,
            new PointerEventHandler(Window_PointerPressed), true);
        _root?.AddHandler(UIElement.PreviewKeyDownEvent,
            new KeyEventHandler(Root_PreviewKeyDown), true);
    }

    private void IconGridView_Unloaded_Focus(object sender, RoutedEventArgs e)
    {
        _root?.RemoveHandler(PointerPressedEvent,
            new PointerEventHandler(Window_PointerPressed));
        _root?.RemoveHandler(UIElement.PreviewKeyDownEvent,
            new KeyEventHandler(Root_PreviewKeyDown));

        _root = null;
    }

    // ---- state changes ----

    private void SelectItem(ItemModel model)
    {
        if (_selectedItem != null && _selectedItem != model)
        {
            _selectedItem.IsSelected = false;
            _selectedItem.IsActive = false;
        }
        _selectedItem = model;
        model.IsSelected = true;   // border
        model.IsActive = true;     // background
    }

    public void ClearSelection()
    {
        if (_selectedItem == null) return;
        _selectedItem.IsSelected = false;
        _selectedItem.IsActive = false;
        _selectedItem = null;
    }

    /// Press inside the grid, not on an item: remove the background; if that's all that's left, clear.
    private void PressedInside()
    {
        if (_selectedItem is { IsSelected: true, IsActive: true })
            _selectedItem.IsActive = false;
        else
            ClearSelection();

        FocusSink.Focus(FocusState.Programmatic);   // drop the focus rectangle
    }

    /// Press outside the grid: remove the border, keep the background; if that's all that's left, clear.
    private void PressedOutside()
    {
        if (_selectedItem is { IsSelected: true, IsActive: true })
            _selectedItem.IsSelected = false;
        else
            ClearSelection();
    }

    // ---- input ----

    private void Window_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _keyboardNavigating = false;
        var node = e.OriginalSource as DependencyObject;
        ItemModel? hit = null;

        while (node != null && node != this)
        {
            if (node is ScrollBar) return;   // dragging the scrollbar is not a "click"
            if (hit == null && node is FrameworkElement { Tag: ItemModel m }) hit = m;
            node = VisualTreeHelper.GetParent(node);
        }

        if (node == null) PressedOutside();      // never reached `this` -> outside
        else if (hit != null) SelectItem(hit);
        else PressedInside();
    }

    // Pointer: just give the item keyboard focus so arrow keys work from here.
    private void Item_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        (sender as Control)?.Focus(FocusState.Pointer);
    }

    // Keyboard navigation (Tab / arrows) selects the item too.
    private void Item_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: ItemModel model } element
            && element.FocusState == FocusState.Keyboard)
        {
            SelectItem(model);
        }
    }
    private bool IsInsideGrid(DependencyObject? node)
    {
        while (node != null)
        {
            if (node == this) return true;
            node = VisualTreeHelper.GetParent(node);
        }
        return false;
    }

    private void Root_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Runs before focus moves, so LostFocus below knows a key caused it
        if (e.Key is Windows.System.VirtualKey.Tab
            or Windows.System.VirtualKey.Up or Windows.System.VirtualKey.Down
            or Windows.System.VirtualKey.Left or Windows.System.VirtualKey.Right)
        {
            _keyboardNavigating = true;
        }
    }

    private void Item_LostFocus(object sender, RoutedEventArgs e)
    {
        if (!_keyboardNavigating) return;                  // pointer is handled elsewhere
        if (sender is not FrameworkElement { Tag: ItemModel model } || model != _selectedItem) return;

        // Wait until focus has actually landed somewhere
        DispatcherQueue.TryEnqueue(() =>
        {
            var focused = FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
            if (IsInsideGrid(focused)) return;             // moved to another item

            if (model is { IsSelected: true, IsActive: true })
                model.IsSelected = false;                  // border off, background stays
        });
    }
}