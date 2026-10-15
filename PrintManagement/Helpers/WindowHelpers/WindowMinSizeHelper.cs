using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace PrintManagement.Helpers.WindowHelpers
{
    public static class WindowExtensions
    {
        public static void SetMinimumSize(this Window window, int minWidth, int minHeight)
        {
            if (window.AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.PreferredMinimumWidth = minWidth;   // device pixels
                presenter.PreferredMinimumHeight = minHeight;
            }
        }
    }
}