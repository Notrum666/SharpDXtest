using System;
using System.IO;

using Editor.AssetsImport;
using Engine;

namespace Editor
{
    public class FolderCreationViewModel : ViewModelBase
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
        public FolderCreationViewModel()
        {

        }
    }
}
