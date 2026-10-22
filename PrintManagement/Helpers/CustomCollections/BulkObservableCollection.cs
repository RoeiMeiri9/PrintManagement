using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace PrintManagement.Helpers.CustomCollections;

public sealed class BulkObservableCollection<T> : ObservableCollection<T>
{
    public event EventHandler? ReplaceAllCompleted;
    public void ReplaceAll(IEnumerable<T> items)
    {
        Items.Clear();
        foreach (var i in items)
        {
            Items.Add(i);
        }

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

        ReplaceAllCompleted?.Invoke(this, EventArgs.Empty);
    }
}