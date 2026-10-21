using Microsoft.UI.Xaml.Controls;

namespace PrintManagement.Controls.IconGridViewControl;

public sealed partial class IconGridView
{
    private void Item_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is ContentControl { Tag: ItemModel itemModel })
        {
            itemModel.IsHovered = true;
        }
    }

    private void Item_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is ContentControl { Tag: ItemModel itemModel })
        {
            itemModel.IsHovered = false;
        }
    }
}
