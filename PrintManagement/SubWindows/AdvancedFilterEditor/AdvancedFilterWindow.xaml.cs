using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using PrintManagement.Models.Entity;
using PrintManagement.Models.Filter;
using PrintManagement.SubWindows.AdvancedFilterEditor.Pages;
using System;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PrintManagement.SubWindows.AdvancedFilterEditor;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AdvancedFilterWindow : Window
{
    public string WindowTitle;
    private static readonly Type[] Stages = [typeof(Filter_Stage1), typeof(Filter_Stage2)];

    private readonly FilterDraft _draft;
    private readonly Func<GenericFilterDefinition, Task> _saveAsync;
    private int _index;

    public AdvancedFilterWindow(FilterDraft draft, Func<GenericFilterDefinition, Task> saveAsync)
    {
        _draft = draft;
        _saveAsync = saveAsync;
        WindowTitle = $"Add New {(draft.EntityType == typeof(PrinterModel) ? "Printer" : "Driver")} Filter";

        InitializeComponent();
        InitializeWindow();
        InitializeTitle();
        Backward.Click += (_, _) => GoTo(_index - 1);
        Forward.Click += Forward_Click;
        GoTo(0);
    }

    private void InitializeWindow()
    {
        var hwnd = new HWND(WinRT.Interop.WindowNative.GetWindowHandle(this));
        var scale = PInvoke.GetDpiForWindow(hwnd) / 96.0;
        AppWindow.Resize(new Windows.Graphics.SizeInt32((int)(680 * scale), (int)(660 * scale)));

        if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
        {
            presenter.IsMinimizable = false;
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }
        AppWindow.IsShownInSwitchers = false;
    }

    private void InitializeTitle()
    {
        Title = WindowTitle;
        SetTitleBar(titleBar);
        ExtendsContentIntoTitleBar = true;
    }

    private void GoTo(int index)
    {
        ErrorBar.IsOpen = false;
        _index = index;
        ContentFrame.Navigate(Stages[index], _draft, new SuppressNavigationTransitionInfo());

        Backward.IsEnabled = index > 0;
        Forward.Content = index == Stages.Length - 1 ? "Finish" : "Next";
    }

    private async void Forward_Click(object sender, RoutedEventArgs e)
    {
        bool isLast = _index == Stages.Length - 1;

        // Stage 1 only needs a name. On the last stage, validate everything.
        var error = isLast
            ? _draft.Validate()
            : string.IsNullOrWhiteSpace(_draft.Name) ? "Give the filter a name." : null;

        if (error is not null)
        {
            ErrorBar.Message = error;
            ErrorBar.IsOpen = true;
            return;
        }

        if (!isLast)
        {
            GoTo(_index + 1);
            return;
        }

        Forward.IsEnabled = Backward.IsEnabled = false;   // prevent double-clicks while saving
        try
        {
            await _saveAsync(_draft.Build());
            Close();
        }
        catch (Exception ex)
        {
            ErrorBar.Message = $"Could not save the filter: {ex.Message}";
            ErrorBar.IsOpen = true;
            Forward.IsEnabled = true;
            Backward.IsEnabled = _index > 0;
        }
    }
}
