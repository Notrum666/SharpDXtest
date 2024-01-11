using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Engine;

namespace Editor
{
    public partial class ContentBrowserControl : UserControl
    {
        public ObservableCollection<FileItem> Items { get; set; }
        public FileItem Item { get; set; }
        private string currentFolderPath = "C:\\GamePS2"; // Путь к стартовой папке
        private bool loaded = false;
        private List<LogMessage> newMessages = new ();

        public ContentBrowserControl()
        {
            InitializeComponent();
            Items = new ObservableCollection<FileItem>();
            TreeView.ItemsSource = Items;
            FileItem item = new FileItem()
            {
                FullPath = currentFolderPath,
                Name = "GamePS2",
                Type = 0
            };
            LoadDirectories(currentFolderPath, item);
            
            Items.Add(item);
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
        private void LoadDirectoriesAndFiles(string rootPath, FileItem parent = null)
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

                foreach (var file in files)
                {
                    var fileItem = new FileItem { Name = Path.GetFileName(file), FullPath = file, Type = ItemType.File, Parent = parent };
                    parent?.Children.Add(fileItem);
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
            }

            Logger.OnLog += Logger_OnLog;

            loaded = true;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // to prevent errors during xaml designer loading in visual studio
            if (!EngineCore.IsAlive)
                return;

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
        File
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