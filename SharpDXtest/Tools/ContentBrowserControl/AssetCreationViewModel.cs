using System;
using System.IO;

using Editor.AssetsImport;
using Engine;

namespace Editor
{
    public class AssetCreationViewModel : ViewModelBase
    {
        private string name = "";
        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }
        private Type type;
        public Type Type
        {
            get => type;
            set
            {
                type = value;
                OnPropertyChanged();
            }
        }
        public AssetCreationViewModel(Type t)
        {
            Type = t;
        }
    }
}
