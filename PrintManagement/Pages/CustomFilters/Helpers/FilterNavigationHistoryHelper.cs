using PrintManagement.Pages.CustomFilters.Components.TreeViewLeaf;
using System;
using System.Collections.Generic;

namespace PrintManagement.Pages.CustomFilters.Helpers;

/// <summary>
/// Back / forward history for the filter tree (like Explorer's navigation buttons).
/// It only tracks leaves; the page still owns the TreeView and passes in a callback that performs the selection.
/// </summary>
public sealed class FilterNavigationHistory
{
    private readonly Stack<FilterLeaf> _back = new();
    private readonly Stack<FilterLeaf> _forward = new();

    /// <summary>Raised after anything changes, so the page can refresh its buttons and tooltips.</summary>
    public event EventHandler? Changed;

    /// <summary>
    /// True while a history move is selecting a leaf. The selection-changed handler must not
    /// record that selection as a new step; <see cref="OnSelectionChanged"/> already checks this.
    /// </summary>
    public bool IsNavigating { get; private set; }

    public bool CanGoBack => _back.Count > 0;
    public bool CanGoForward => _forward.Count > 0;

    public string BackTargetName => _back.Count > 0 ? _back.Peek().Name : string.Empty;
    public string ForwardTargetName => _forward.Count > 0 ? _forward.Peek().Name : string.Empty;

    /// <summary>
    /// Call from the TreeView's SelectionChanged. A selection the user made by hand becomes a new back step
    /// and clears forward history; selections made by <see cref="GoBack"/>, <see cref="GoForward"/> and
    /// <see cref="GoTo"/> are ignored here.
    /// </summary>
    public void OnSelectionChanged(FilterLeaf? previous, FilterLeaf current)
    {
        if (!IsNavigating && previous is not null && previous != current)
        {
            _back.Push(previous);
            _forward.Clear();
        }
        RaiseChanged();
    }

    /// <returns>False if there is nothing to go back to.</returns>
    public bool GoBack(FilterLeaf? current, Action<FilterLeaf> select)
    {
        if (_back.Count == 0) return false;

        if (current is not null)
            _forward.Push(current);

        Navigate(_back.Pop(), select);
        return true;
    }

    /// <returns>False if there is nothing to go forward to.</returns>
    public bool GoForward(FilterLeaf? current, Action<FilterLeaf> select)
    {
        if (_forward.Count == 0) return false;

        if (current is not null)
            _back.Push(current);

        Navigate(_forward.Pop(), select);
        return true;
    }

    /// <summary>
    /// A programmatic jump (for example "Up" to the root). Counts as a normal step:
    /// the current leaf goes on the back stack and forward history is cleared.
    /// </summary>
    public void GoTo(FilterLeaf? current, FilterLeaf target, Action<FilterLeaf> select)
    {
        if (ReferenceEquals(current, target)) return;

        if (current is not null)
        {
            _back.Push(current);
            _forward.Clear();
        }
        Navigate(target, select);
    }

    /// <summary>Forget everything, e.g. after the tree is rebuilt and the old leaf objects no longer exist.</summary>
    public void Clear()
    {
        _back.Clear();
        _forward.Clear();
        RaiseChanged();
    }

    private void Navigate(FilterLeaf target, Action<FilterLeaf> select)
    {
        IsNavigating = true;
        try { select(target); }
        finally { IsNavigating = false; }

        RaiseChanged();
    }

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}