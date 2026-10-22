using System;
using System.Collections.Generic;

namespace PrintManagement.Controls.DataTableControl;

public partial class DataTable
{
    public event EventHandler? TableReadyForAnimation;
    bool _isReadyFired = false;

    private enum TableInitialTasks
    {
        DataLoaded = 1,
        AutoWidthCalculated = 2,
        PaddingSet = 3,
    }
    private readonly HashSet<TableInitialTasks> _pendingTasks = [
        TableInitialTasks.DataLoaded,
        TableInitialTasks.AutoWidthCalculated,
        TableInitialTasks.PaddingSet,
    ];

    private void TaskCompleted(TableInitialTasks taskName)
    {
        if (_isReadyFired) return;

        _pendingTasks.Remove(taskName);

        if (_pendingTasks.Count == 0)
        {
            _isReadyFired = true;
            TableReadyForAnimation?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ResetReadinessGate()
    {
        _isReadyFired = false;
        _pendingTasks.Clear();
        _pendingTasks.Add(TableInitialTasks.DataLoaded);
        _pendingTasks.Add(TableInitialTasks.AutoWidthCalculated);
        _pendingTasks.Add(TableInitialTasks.PaddingSet);
    }
}
