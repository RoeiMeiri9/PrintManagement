using System.ComponentModel;

namespace PrintManagement.Interfaces;

internal interface INavigationCommands : INotifyPropertyChanged
{
    bool CanRefresh { get; }
    bool CanGoUp { get; }
    bool CanGoBack { get; }
    bool CanGoForward { get; }

    string BackTargetName { get; }
    string ForwardTargetName { get; }
    string UpTargetName { get; }
    string RefereshTargetName { get; }

    void Refresh();
    void GoUp();
    void GoBack();
    void GoForward();
}
