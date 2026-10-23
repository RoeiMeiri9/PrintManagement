using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PrintManagement.Models.Entity;

public partial class EntityModel : INotifyPropertyChanged
{
    public const uint DEFAULT_VALUE = 0;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
