using Microsoft.UI.Xaml;
using PrintManagement.Models.Entity;
using PrintManagement.Models.Filter;
using PrintManagement.SubWindows.AdvancedFilterEditor;
using System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT.Interop;

namespace PrintManagement.Services;

public interface ISubWindowService
{
    event EventHandler<GenericFilterDefinition>? FilterSaved;
    bool IsFilterEditorOpen { get; }
    void ShowFilterEditor<TEntity>(GenericFilterDefinition? existing = null) where TEntity : EntityModel;
    void CloseFilterEditor();
}

public sealed class SubWindowService(Window owner, FilterStorageService filterStorage) : ISubWindowService
{
    private AdvancedFilterWindow? _filterEditor;

    public bool IsFilterEditorOpen => _filterEditor is not null;

    public event EventHandler<GenericFilterDefinition>? FilterSaved;

    public void ShowFilterEditor<TEntity>(GenericFilterDefinition? existing = null)
        where TEntity : EntityModel
    {
        // Already open: bring it forward instead of opening a second one
        if (_filterEditor is not null)
        {
            _filterEditor.Activate();
            return;
        }

        var draft = new FilterDraft(typeof(TEntity), existing);

        var window = new AdvancedFilterWindow(draft, async def =>
        {
            await filterStorage.SaveAsync<TEntity>(def);
            FilterSaved?.Invoke(this, def);
        });

        SetOwner(window, owner);
        window.Closed += (_, _) => _filterEditor = null;
        _filterEditor = window;
        window.Activate();
    }

    public void CloseFilterEditor() => _filterEditor?.Close();

    private static void SetOwner(Window window, Window ownerWindow)
    {
        var hwnd = new HWND(WindowNative.GetWindowHandle(window));
        var ownerHwnd = WindowNative.GetWindowHandle(ownerWindow);

        // GWLP_HWNDPARENT sets the OWNER, not a child parent
        PInvoke.SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWLP_HWNDPARENT, ownerHwnd);
    }
}