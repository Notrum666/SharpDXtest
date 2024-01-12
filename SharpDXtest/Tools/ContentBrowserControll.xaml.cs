using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Editor.AssetsImport;
using Engine;

namespace Editor
{
    public partial class ContentBrowserControl
    {
        public ObservableCollection<FileItem> Items { get; set; }
        public FileItem Item { get; set; }
        private string currentFolderPath
        {
            get
            {
                return Directory.GetParent(Environment.CurrentDirectory).Parent.FullName + "\\TestProject\\Content";
            }
        } // Путь к стартовой папке
        private bool loaded;
        private List<LogMessage> newMessages = new ();

        public ContentBrowserControl()
        {
            InitializeComponent();
        }
        
        private void LoadDirectories(string rootPath, FileItem parent = null)
        {
            try
            {
                var directories = Directory.GetDirectories(rootPath);

                foreach (var directory in directories)
                {
                    var directoryItem = new FileItem { Name = Path.GetFileName(directory), FullPath = directory, Type = ItemType.Folder, Parent = parent };
                    parent?.Children.Add(directoryItem);
                    LoadDirectories(directory, directoryItem);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Обработка случаев, когда у пользователя нет доступа к папке
            }
        }
        private void LoadDirectoriesAndFiles(string rootPath, FileItem parent)
        {
            try
            {
                var directories = Directory.GetDirectories(rootPath);

                foreach (var directory in directories)
                {
                    var directoryItem = new FileItem { Name = Path.GetFileName(directory), FullPath = directory, Type = ItemType.Folder, Parent = parent };
                    parent?.Children.Add(directoryItem);
                }

                var files = Directory.GetFiles(rootPath);
                AssetImportContext importContext2 = new AssetImportContext(rootPath);
                importContext2.LoadAssetMeta();
                
                foreach (var file in files)
                {
                    if (file.Contains(".meta"))
                    {
                        AssetMeta assetMeta = YamlManager.LoadFromFile<AssetMeta>(file);
                        foreach (var subAsset in assetMeta.SubAssets)
                        {
                            var assetsItem = new FileItem { Name = subAsset.Key.Item2, FullPath = file, Type = ItemType.Assets, Parent = parent };
                            parent?.Children.Add(assetsItem);
                        }
                        string text = file.Replace(".meta", String.Empty);
                        var fileItem = new FileItem
                        {
                            Name = Path.GetFileName(text.Substring(0, text.Length - (text.Length - text.IndexOf('.')))), 
                            FullPath = text, 
                            Type = ItemType.File, 
                            Parent = parent
                        };
                        parent?.Children.Add(fileItem);
                        parent?.Children.Remove(parent.Children.FirstOrDefault(x => x.FullPath == text));
                    }
                    else
                    {
                        var fileItem = new FileItem { Name = Path.GetFileName(file), FullPath = file, Type = ItemType.File, Parent = parent };
                        if(parent != null && parent.Children.Any(x => x.FullPath == file)) continue;
                        parent.Children.Add(fileItem);   
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Обработка случаев, когда у пользователя нет доступа к папке
            }
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            
            var selectedFolderItem = e.NewValue as FileItem;
            if (selectedFolderItem is {Type: ItemType.Folder})
            {
                UpdateItemsControl(selectedFolderItem.FullPath, selectedFolderItem.Name);
                Item = selectedFolderItem;
            }
            
        }

        private void UpdateItemsControl(string folderPath, string nameFolder)
        {
            FileItem item = new FileItem()
            {
                FullPath = folderPath,
                Name = nameFolder,
                Type = 0
            };
            LoadDirectoriesAndFiles(folderPath, item);
            ItemsControl.ItemsSource = item.Children;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // to prevent errors during xaml designer loading in visual studio
            if (!EngineCore.IsAlive)
                return;

            if (!loaded)
            {
                Width = double.NaN;
                Height = double.NaN;

                Items = new ObservableCollection<FileItem>();
                TreeView.ItemsSource = Items;
                FileItem item = new FileItem()
                {
                    FullPath = currentFolderPath,
                    Name = "Content",
                    Type = 0
                };
                LoadDirectories(currentFolderPath, item);

                Items.Add(item);
                Application.Current.Activated += ContentBrowserControl_Activated;
            }

            Logger.OnLog += Logger_OnLog;

            loaded = true;
        }
        private void ContentBrowserControl_Activated(object sender, EventArgs e)
        {
            if (Item != null)
            {
                UpdateItemsControl(Item.FullPath, Item.Name);    
            }
        }
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // to prevent errors during xaml designer loading in visual studio
            if (!EngineCore.IsAlive)
                return;
            Application.Current.Activated -= ContentBrowserControl_Activated;
            Logger.OnLog -= Logger_OnLog;
        }
        
        private void Logger_OnLog(LogMessage message)
        {
            newMessages.Add(message);
        }

    }

    public enum ItemType
    {
        Folder,
        File,
        Assets 
    }

    public class FileItem
    {
        public string Name { get; set; }
        public ItemType Type { get; set; }
        public ObservableCollection<FileItem> Children { get; } = new ();
        public string FullPath { get; set; }

        public FileItem Parent { get; set; }
    }
}