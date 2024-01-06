using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Editor
{
    public class ViewModelBase : INotifyPropertyChanged //TODO: Inherit other ViewModels (in InspectorControl) from this one
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
