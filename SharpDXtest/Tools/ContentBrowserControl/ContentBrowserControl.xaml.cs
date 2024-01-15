using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

        private bool isGrabbed = false;
        private Point localGrabPos;
        private readonly double DragThreshold = 5;

        public ContentBrowserControl()
        {
            InitializeComponent();

            DataContext = this;
        }
        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
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

        private void MoveAsset(ContentBrowserAssetViewModel sourceFile, ContentBrowserFolderViewModel destinationFolder)
        {
            string filePath = Path.ChangeExtension(sourceFile.AssetPath, null);
            string fileName = Path.GetFileName(filePath);
            string destinationFilePath = Path.Combine(destinationFolder.FullPath, fileName);

            if (AssetsRegistry.MoveAsset(filePath, destinationFilePath))
            {
                destinationFolder.Refresh();
                ((ContentBrowserFolderViewModel)SelectedFolderListBox.DataContext).Refresh();
            }
        }

        private void MoveFolder(ContentBrowserFolderViewModel sourceFolder, ContentBrowserFolderViewModel destinationFolder)
        {
            string folderName = Path.GetFileName(sourceFolder.FullPath);
            string destinationFolderPath = Path.Combine(destinationFolder.FullPath, folderName);

            AssetsRegistry.MoveFolder(sourceFolder.FullPath, destinationFolderPath);
        }

        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null)
            {
                ContentBrowserFolderViewModel targetFolder = (ContentBrowserFolderViewModel)((FrameworkElement)sender).DataContext;
                foreach (string file in files)
                    CopyFile(file, targetFolder);
                return;
            }

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
            ((ListBox)sender).Focus();
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
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (e.ClickCount == 2 && ((FrameworkElement)sender).DataContext is ContentBrowserFolderViewModel folder)
                {
                    SelectFolder(folder);
                    return;
                }

                isGrabbed = true;
                localGrabPos = e.GetPosition(this);
            }
        }

        private void FolderItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
                isGrabbed = false;

            e.Handled = true;

            if (isGrabbed && (e.GetPosition(this) - localGrabPos).LengthSquared >= DragThreshold * DragThreshold)
            {
                DragDrop.DoDragDrop(this, new DataObject(DataFormats.Serializable, ((FrameworkElement)sender).DataContext), DragDropEffects.Move);
            }
        }

        private void TreeViewFolder_Drop(object sender, DragEventArgs e)
        {
            ContentBrowserFolderViewModel targetFolder = (ContentBrowserFolderViewModel)((FrameworkElement)sender).DataContext;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null)
            {
                foreach (string file in files)
                    CopyFile(file, targetFolder);
                return;
            }

            object obj = e.Data.GetData(DataFormats.Serializable);
            if (obj is ContentBrowserAssetViewModel asset)
            {
                MoveAsset(asset, targetFolder);
                return;
            }

            if (obj is ContentBrowserFolderViewModel folder)
            {
                MoveFolder(folder, targetFolder);
                RootFolderViewModels[0].Refresh();
                SelectFolder(RootFolderViewModels[0]);
                return;
            }

            e.Handled = true;
        }

        private void PasteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            StringCollection files = Clipboard.GetFileDropList();
            foreach (string file in files)
                CopyFile(file, (ContentBrowserFolderViewModel)SelectedFolderListBox.DataContext);
        }

        private void CopyCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (((ListBox)sender).SelectedItem is ContentBrowserAssetViewModel asset && !asset.IsSubAsset)
                Clipboard.SetFileDropList(new StringCollection() { Path.ChangeExtension(asset.AssetPath, null) });
        }

        private void FolderItem_Drop(object sender, DragEventArgs e)
        {
            ContentBrowserFolderViewModel targetFolder = (ContentBrowserFolderViewModel)((FrameworkElement)sender).DataContext;
            if (targetFolder is null)
                return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null)
            {
                foreach (string file in files)
                    CopyFile(file, targetFolder);
                return;
            }

            object obj = e.Data.GetData(DataFormats.Serializable);
            if (obj is ContentBrowserAssetViewModel asset)
            {
                MoveAsset(asset, targetFolder);
                return;
            }

            if (obj is ContentBrowserFolderViewModel folder)
            {
                MoveFolder(folder, targetFolder);
                targetFolder.Refresh();
                ((ContentBrowserFolderViewModel)SelectedFolderListBox.DataContext).Refresh();
                return;
            }

            e.Handled = true;
        }

        private void TreeViewItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                isGrabbed = true;
                localGrabPos = e.GetPosition(this);
            }
        }

        private void TreeViewItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
                isGrabbed = false;

            e.Handled = true;

            if (isGrabbed && (e.GetPosition(this) - localGrabPos).LengthSquared >= DragThreshold * DragThreshold)
            {
                DragDrop.DoDragDrop(this, new DataObject(DataFormats.Serializable, ((FrameworkElement)sender).DataContext), DragDropEffects.Move);
            }
        }
    }
}