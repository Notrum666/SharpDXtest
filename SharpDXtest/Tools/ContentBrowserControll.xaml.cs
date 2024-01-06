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
        private string currentFolderPath = "Content"; // Путь к стартовой папке
        private bool loaded = false;
        private List<LogMessage> newMessages = new ();

        public ContentBrowserControl()
        {
            InitializeComponent();
            Items = new ObservableCollection<FileItem>();
            TreeView.ItemsSource = Items;

            LoadDirectories(currentFolderPath, null);
        }

        private void LoadDirectories(string path, FileItem parent)
        {
            try
            {
                foreach (var directory in Directory.GetDirectories(path))
                {
                    var directoryItem = new FileItem { Name = Path.GetFileName(directory), Type = ItemType.Folder, FullPath = directory};
                    parent?.Children.Add(directoryItem);
                    Items.Add(directoryItem);
                }

                foreach (var file in Directory.GetFiles(path))
                {
                    var fileItem = new FileItem { Name = Path.GetFileName(file), Type = ItemType.File };
                    Items.Add(fileItem);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            
            var selectedFolderItem = e.NewValue as FileItem;

            if (selectedFolderItem != null)
            {
                UpdateItemsControl(selectedFolderItem.FullPath);
                Item = selectedFolderItem;
            }
        }

        private void UpdateItemsControl(string folderPath)
        {
            Items.Clear();
            LoadDirectories(folderPath, null);
            ItemsControl.ItemsSource = Items;
        }
        
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Item != null)
            {
                var parentDirectory = Directory.GetParent(Item.FullPath);

                if (parentDirectory != null)
                {
                    currentFolderPath = parentDirectory.FullName;
                    UpdateItemsControl(currentFolderPath);
                }   
            }
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