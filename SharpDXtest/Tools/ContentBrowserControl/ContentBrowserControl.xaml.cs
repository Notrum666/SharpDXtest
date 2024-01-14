using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Editor.AssetsImport;

using Engine;

namespace Editor
{
    public partial class ContentBrowserControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<ContentBrowserFolderViewModel> RootFolderViewModels { get; } = new ObservableCollection<ContentBrowserFolderViewModel>();
        private double itemsWidth = 100;
        public double ItemsWidth
        {
            get => itemsWidth;
            set
            {
                itemsWidth = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ItemsHeight));
            }
        }
        public double ItemsHeight => ItemsWidth * 1.5;

        private bool loaded;
        
        private bool isDragging = false;

        public ContentBrowserControl()
        {
            InitializeComponent();

            DataContext = this;
            //ItemsControl.PreviewMouseLeftButtonDown += ItemsControl_PreviewMouseLeftButtonDown;
            //ItemsControl.MouseMove += ItemsControl_MouseMove;
            //ItemsControl.MouseLeftButtonUp += ItemsControl_MouseLeftButtonUp;
        }
        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        //private void ItemsControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    DependencyObject dep = (DependencyObject)e.OriginalSource;
        //
        //    while (dep != null && !(dep is ContentPresenter || dep is TextBlock))
        //    {
        //        dep = VisualTreeHelper.GetParent(dep);
        //    }
        //
        //    if (dep == null)
        //    {
        //        return;
        //    }
        //
        //    Item = (FileItem)((FrameworkElement)dep).DataContext;
        //    if (Item.Type == ItemType.File)
        //    {
        //        isDragging = true;    
        //    }
        //    
        //}
        //
        //private void ItemsControl_MouseMove(object sender, MouseEventArgs e)
        //{
        //    if (isDragging && e.LeftButton == MouseButtonState.Pressed)
        //    {
        //        DataObject data = new DataObject();
        //        data.SetData(DataFormats.FileDrop, Item);
        //
        //        DragDrop.DoDragDrop((DependencyObject)e.OriginalSource, data, DragDropEffects.Copy);
        //        
        //        isDragging = false;
        //    }
        //}
        //
        //private void ItemsControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        //{
        //    isDragging = false;
        //}

        //private void TreeView_DragEnter(object sender, DragEventArgs e)
        //{
        //    if (e.Data.GetDataPresent(DataFormats.FileDrop))
        //    {
        //        e.Effects = DragDropEffects.Copy;
        //    }
        //    else
        //    {
        //        e.Effects = DragDropEffects.None;
        //    }
        //}
        //
        //private void TreeView_DragOver(object sender, DragEventArgs e)
        //{
        //    if (e.Data.GetDataPresent(DataFormats.FileDrop))
        //    {
        //        e.Effects = DragDropEffects.Copy;
        //    }
        //    else
        //    {
        //        e.Effects = DragDropEffects.None;
        //    }
        //    Point currentPosition = e.GetPosition(TreeView);
        //
        //    TreeViewItem item = GetTreeViewItemFromPoint(TreeView, currentPosition);
        //
        //    if (item != null && item != lastItem)
        //    {
        //        if (lastItem != null)
        //        {
        //            lastItem.Background = Brushes.Transparent;
        //        }
        //
        //        item.Background = Brushes.LightBlue;
        //        lastItem = item;
        //    }
        //
        //    if (item != null && item.HasItems)
        //    {
        //        item.IsExpanded = true;
        //    }
        //
        //    e.Handled = true;
        //}
        
        //private TreeViewItem GetTreeViewItemFromPoint(TreeView treeView, Point position)
        //{
        //    HitTestResult hitTestResult = VisualTreeHelper.HitTest(treeView, position);
        //    DependencyObject target = hitTestResult?.VisualHit;
        //
        //    while (target != null && !(target is TreeViewItem))
        //    {
        //        target = VisualTreeHelper.GetParent(target);
        //    }
        //
        //    return target as TreeViewItem;
        //}
        //
        //private void TreeView_Drop(object sender, DragEventArgs e)
        //{
        //    var fileDrop = e.Data.GetData(DataFormats.FileDrop);
        //    DependencyObject dep = (DependencyObject)e.OriginalSource;
        //
        //    while (dep != null && !(dep is ContentPresenter || dep is TextBlock))
        //    {
        //        dep = VisualTreeHelper.GetParent(dep);
        //    }
        //
        //    if (dep == null)
        //    {
        //        return;
        //    }
        //
        //    FileItem itemTreeViewTarget = (FileItem)((FrameworkElement)dep).DataContext;
        //    
        //    if (fileDrop.GetType() == typeof(string[]))
        //    {
        //        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
        //        foreach (string fileOther in files)
        //        {
        //            CopyFile(fileOther, itemTreeViewTarget.FullPath); 
        //        }
        //
        //        TreeView_ClearSelectColor();
        //        return;
        //    }
        //
        //    if (fileDrop.GetType() == typeof(string[]))
        //    {
        //        FileItem file = (FileItem)fileDrop;
        //    
        //
        //        string getResultPath = itemTreeViewTarget.FullPath + "\\" + file.FullName;
        //    
        //        bool resultMove = AssetsRegistry.MoveAsset(file.FullPath, getResultPath);
        //        if (resultMove)
        //        {
        //            Guid? guid = AssetsRegistry.ImportAsset(getResultPath);
        //            if (guid != null)
        //            {
        //                UpdateItemsControl(getResultPath, Item.Name);
        //            }    
        //        }
        //
        //        TreeView_ClearSelectColor();
        //        return;
        //    }
        //    e.Handled = true;
        //    // тут лог сделать что файл говно   
        //}

        //private void UserControl_DragEnter(object sender, DragEventArgs e)
        //{
        //    if (e.Data.GetDataPresent(DataFormats.FileDrop))
        //    {
        //        e.Effects = DragDropEffects.Copy;
        //    }
        //    else
        //    {
        //        e.Effects = DragDropEffects.None;
        //    }
        //}
        //
        //private void TreeView_ClearSelectColor()
        //{
        //    if (lastItem != null)
        //    {
        //        lastItem.Background = Brushes.Transparent;
        //        lastItem = null;
        //    }
        //}
        

        //private void UserControl_DragOver(object sender, DragEventArgs e)
        //{
        //    if (e.Data.GetDataPresent(DataFormats.FileDrop))
        //    {
        //        e.Effects = DragDropEffects.Copy;
        //    }
        //    else
        //    {
        //        e.Effects = DragDropEffects.None;
        //    }
        //
        //    e.Handled = true;
        //}
        
        //private void LoadDirectories(string rootPath, FileItem parent = null)
        //{
        //    try
        //    {
        //        var directories = Directory.GetDirectories(rootPath);
        //
        //        foreach (var directory in directories)
        //        {
        //            var directoryItem = new FileItem { Name = Path.GetFileName(directory), FullPath = directory, Type = ItemType.Folder, Parent = parent };
        //            parent?.Children.Add(directoryItem);
        //            LoadDirectories(directory, directoryItem);
        //        }
        //    }
        //    catch (UnauthorizedAccessException)
        //    {
        //        // Обработка случаев, когда у пользователя нет доступа к папке
        //    }
        //}
        //private void LoadDirectoriesAndFiles(string rootPath, FileItem parent)
        //{
        //    try
        //    {
        //        var directories = Directory.GetDirectories(rootPath);
        //
        //        foreach (var directory in directories)
        //        {
        //            var directoryItem = new FileItem { Name = Path.GetFileName(directory), FullPath = directory, Type = ItemType.Folder, Parent = parent };
        //            parent?.Children.Add(directoryItem);
        //        }
        //
        //        var files = Directory.GetFiles(rootPath);
        //
        //        foreach (var file in files)
        //        {
        //            if (file.Contains(".meta"))
        //            {
        //                AssetMeta assetMeta = YamlManager.LoadFromFile<AssetMeta>(file);
        //                foreach (var subAsset in assetMeta.SubAssets)
        //                {
        //                    var assetsItem = new FileItem { Name = subAsset.Key.Item2, FullPath = file, Type = ItemType.Assets, Parent = parent };
        //                    parent?.Children.Add(assetsItem);
        //                }
        //                string text = file.Replace(".meta", String.Empty);
        //                var fileItem = new FileItem
        //                {
        //                    Name = Path.GetFileName(text.Substring(0, text.Length - (text.Length - text.IndexOf('.')))), 
        //                    FullPath = text, 
        //                    FullName = Path.GetFileName(text), 
        //                    Type = ItemType.File, 
        //                    Parent = parent
        //                };
        //                parent?.Children.Add(fileItem);
        //            }
        //        }
        //    }
        //    catch (UnauthorizedAccessException)
        //    {
        //        // Обработка случаев, когда у пользователя нет доступа к папке
        //    }
        //}

        //private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        //{
        //    var selectedFolderItem = e.NewValue as FileItem;
        //    if (selectedFolderItem is {Type: ItemType.Folder})
        //    {
        //        UpdateItemsControl(selectedFolderItem.FullPath, selectedFolderItem.Name);
        //        Item = selectedFolderItem;
        //    }
        //    
        //}

        //private void UpdateItemsControl(string folderPath, string nameFolder)
        //{
        //    FileItem item = new FileItem()
        //    {
        //        FullPath = folderPath,
        //        Name = nameFolder,
        //        Type = 0
        //    };
        //    LoadDirectoriesAndFiles(folderPath, item);
        //    ItemsControl.ItemsSource = item.Children;
        //}
        public void Refresh()
        {
            RootFolderViewModels.Clear();
            RootFolderViewModels.Add(new ContentBrowserFolderViewModel(AssetsRegistry.ContentFolderPath));
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

                Refresh();
                SelectFolder(RootFolderViewModels.First());
            }

            loaded = true;
        }
        //private void ContentBrowserControl_Activated(object sender, EventArgs e)
        //{
        //    if (Item != null && (Item.Type != ItemType.File || Item.Type != ItemType.Assets))
        //    {
        //        UpdateItemsControl(Item.FullPath, Item.Name);    
        //    }
        //}
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // to prevent errors during xaml designer loading in visual studio
            if (!EngineCore.IsAlive)
                return;
        }

        private void CopyFile(string sourceFilePath, ContentBrowserFolderViewModel destinationFolder)
        {
            try
            {
                string fileName = Path.GetFileName(sourceFilePath);
                string destinationFilePath = Path.Combine(destinationFolder.FullPath, fileName);
                File.Copy(sourceFilePath, destinationFilePath);
                
                Guid? guid = AssetsRegistry.ImportAsset(destinationFilePath);
                if (guid != null)
                    destinationFolder.Refresh();
                else
                    File.Delete(destinationFilePath);
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, "Error occured during copying of file from \"" + sourceFilePath + "\" to \"" +
                    destinationFolder.FullPath + "\": " + e.Message);
            }
        }
        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            ContentBrowserFolderViewModel targetFolder = (ContentBrowserFolderViewModel)((FrameworkElement)sender).DataContext;
            foreach (string file in files)
                CopyFile(file, targetFolder);

            e.Handled = true;
        }

        private void SelectFolder(ContentBrowserFolderViewModel folder)
        {
            Stack<ContentBrowserFolderViewModel> queue = new Stack<ContentBrowserFolderViewModel>();
            queue.Push(folder);
            ContentBrowserFolderViewModel parent = folder.Parent;
            while (parent != null)
            {
                queue.Push(parent);
                parent = parent.Parent;
            }

            ItemContainerGenerator generator = FoldersTreeView.ItemContainerGenerator;
            while (queue.Count > 0)
            {
                ContentBrowserFolderViewModel dequeue = queue.Pop();
                FoldersTreeView.UpdateLayout();
                TreeViewItem treeViewItem = (TreeViewItem)generator.ContainerFromItem(dequeue);
                if (queue.Count > 0)
                    treeViewItem.IsExpanded = true;
                else
                    treeViewItem.IsSelected = true;
                generator = treeViewItem.ItemContainerGenerator;
            }
        }

        private void ListBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            ((ListBox)sender).UnselectAll();
        }

        private void ListBox_KeyDown(object sender, KeyEventArgs e)
        {
            ListBox listBox = (ListBox)sender;
            if (listBox.SelectedItem is null)
                return;

            if (e.Key == Key.Enter && listBox.SelectedItem is ContentBrowserFolderViewModel folderToEnter)
                SelectFolder(folderToEnter);
            if (e.Key == Key.Delete)
            {
                if (listBox.SelectedItem is ContentBrowserFolderViewModel folderToDelete)
                    AssetsRegistry.DeleteFolder(folderToDelete.FullPath);
                if (listBox.SelectedItem is ContentBrowserAssetViewModel assetToDelete)
                    AssetsRegistry.DeleteAsset(assetToDelete.AssetPath);
                ((ContentBrowserFolderViewModel)listBox.DataContext).Refresh();
            }
        }

        private void FolderItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && ((FrameworkElement)sender).DataContext is ContentBrowserFolderViewModel folder)
                SelectFolder(folder);
        }
    }
}