using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Data;

using Engine;

namespace Editor
{
    public class ContentBrowserFolderViewModel : ViewModelBase
    {
        private string name;
        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }
        private string fullPath;
        public string FullPath
        {
            get => fullPath;
            set
            {
                fullPath = value;
                OnPropertyChanged();
            }
        }
        private ContentBrowserFolderViewModel parent = null;
        public ContentBrowserFolderViewModel Parent
        {
            get => parent;
            set
            {
                parent = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<ContentBrowserFolderViewModel> Subfolders { get; } = new ObservableCollection<ContentBrowserFolderViewModel>();
        public ObservableCollection<ContentBrowserAssetViewModel> Assets { get; } = new ObservableCollection<ContentBrowserAssetViewModel>();
        public CompositeCollection FolderItems { get; }
        public ContentBrowserFolderViewModel(string path, ContentBrowserFolderViewModel parent = null)
        {
            FolderItems = new CompositeCollection
            {
                new CollectionContainer() { Collection = Subfolders },
                new CollectionContainer() { Collection = Assets }
            };

            fullPath = path;
            Name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar));
            Parent = parent;

            Refresh();
        }
        public void Refresh()
        {
            Subfolders.Clear();
            Assets.Clear();

            if (!Directory.Exists(fullPath))
            {
                if (parent is null)
                {
                    Logger.Log(LogType.Error, "Content folder is lost, unable to refresh folder.");
                    return;
                }
                parent.Refresh();
                return;
            }

            string[] directories;
            try
            {
                directories = Directory.GetDirectories(fullPath);
            }
            catch (UnauthorizedAccessException)
            {
                Logger.Log(LogType.Warning, "No access to folder " + AssetsRegistry.ContentFolderName + "\\" +
                    Path.GetRelativePath(AssetsRegistry.ContentFolderPath, fullPath));
                return;
            }

            foreach (string dir in directories)
                Subfolders.Add(new ContentBrowserFolderViewModel(dir, this));

            string[] files = Directory.GetFiles(fullPath);

            foreach (string file in files)
            {
                if (Path.GetExtension(file) == ".meta")
                {
                    ContentBrowserAssetViewModel assetViewModel = new ContentBrowserAssetViewModel(file);
                    if (assetViewModel.AssetMeta is null)
                        continue;

                    Assets.Add(assetViewModel);

                    foreach ((Type type, string name) subAsset in assetViewModel.AssetMeta.SubAssets.Keys)
                        Assets.Add(new ContentBrowserAssetViewModel() { Name = subAsset.name });
                }
            }
        }
    }
}
