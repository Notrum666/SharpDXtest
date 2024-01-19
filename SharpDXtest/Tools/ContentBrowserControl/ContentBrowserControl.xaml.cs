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
using Engine.AssetsData;

namespace Editor
{
    public partial class ContentBrowserControl : UserControl, INotifyPropertyChanged
    {
        public static ContentBrowserControl Current { get; private set; }
        private RelayCommand startFolderCreationCommand;
        public RelayCommand StartFolderCreationCommand => startFolderCreationCommand ??= new RelayCommand(obj => FolderCreationViewModels.Add(new FolderCreationViewModel()));
        private RelayCommand startAssetCreationCommand;
        public RelayCommand StartAssetCreationCommand => startAssetCreationCommand ??= new RelayCommand(obj => AssetCreationViewModels.Add(new AssetCreationViewModel((Type)obj)));
        private RelayCommand refreshCommand;
        public RelayCommand RefreshCommand => refreshCommand ??= new RelayCommand(_ => ((ContentBrowserFolderViewModel)SelectedFolderListBox.DataContext).Refresh());
        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<ContentBrowserFolderViewModel> RootFolderViewModels { get; } = new ObservableCollection<ContentBrowserFolderViewModel>();
        public ObservableCollection<AssetCreationViewModel> AssetCreationViewModels { get; } = new ObservableCollection<AssetCreationViewModel>();
        public ObservableCollection<FolderCreationViewModel> FolderCreationViewModels { get; } = new ObservableCollection<FolderCreationViewModel>();
        private double itemsWidth = 60;
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

            Current = this;

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

            Current = null;
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
            if (sourceFile.AssetPath is null)
            {
                Logger.Log(LogType.Warning, "Tried to move subasset, which does not have a file, move the base asset instead.");
                return;
            }

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

        private void SelectedFolderListBox_Drop(object sender, DragEventArgs e)
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

        private void ListBox_PreviewMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = e.LeftButton == MouseButtonState.Pressed;

            ListBoxItem item = ((DependencyObject)e.OriginalSource).FindParentWithPath<ListBox>().FirstOrDefault(d => d is ListBoxItem, null) as ListBoxItem;
            if (item is not null)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    object dataContext = item.DataContext;

                    if (dataContext is not ContentBrowserFolderViewModel && dataContext is not ContentBrowserAssetViewModel)
                        return;

                    if (e.ClickCount == 2)
                    {
                        if (dataContext is ContentBrowserFolderViewModel folder)
                            SelectFolder(folder);
                        if (dataContext is ContentBrowserAssetViewModel asset)
                            asset.Open();
                        return;
                    }

                    isGrabbed = true;
                    localGrabPos = e.GetPosition(this);
                }

                return;
            }

            ((ListBox)sender).UnselectAll();
            ((ListBox)sender).Focus();
        }

        private void ListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            isGrabbed = false;

            ((ListBox)sender).Focus();

            ListBoxItem item = ((DependencyObject)e.OriginalSource).FindParentWithPath<ListBox>().FirstOrDefault(d => d is ListBoxItem, null) as ListBoxItem;
            if (item is not null)
                SelectedFolderListBox.SelectedItem = item.DataContext;

            SendSelectedAssetToInspector();
        }

        private void SelectedFolderListBox_KeyDown(object sender, KeyEventArgs e)
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
                    AssetsRegistry.DeleteAsset(Path.ChangeExtension(assetToDelete.AssetPath, null));
                ((ContentBrowserFolderViewModel)listBox.DataContext).Refresh();
            }

            e.Handled = true;
        }

        private void FolderItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
                isGrabbed = false;

            e.Handled = true;

            if (isGrabbed && (e.GetPosition(this) - localGrabPos).LengthSquared >= DragThreshold * DragThreshold)
            {
                object dataContext = ((FrameworkElement)sender).DataContext;
                DragDropEffects effects = DragDropEffects.Move;
                if (dataContext is ContentBrowserAssetViewModel)
                    effects |= DragDropEffects.Link;
                DragDrop.DoDragDrop(this, new DataObject(DataFormats.Serializable, dataContext), effects);
            }
        }

        private void TreeViewFolder_Drop(object sender, DragEventArgs e)
        {
            ContentBrowserFolderViewModel targetFolder = (ContentBrowserFolderViewModel)((FrameworkElement)sender).DataContext;

            e.Handled = true;

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
            ContentBrowserFolderViewModel targetFolder = ((FrameworkElement)sender).DataContext as ContentBrowserFolderViewModel;
            if (targetFolder is null)
                return;

            e.Handled = true;

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

            if (obj is ContentBrowserFolderViewModel folder && targetFolder != folder)
            {
                MoveFolder(folder, targetFolder);
                targetFolder.Refresh();
                ((ContentBrowserFolderViewModel)SelectedFolderListBox.DataContext).Refresh();
                return;
            }
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

        private void ItemCreationTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            AssetCreationViewModels.Clear();
            FolderCreationViewModels.Clear();
        }

        private void ItemCreationTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                AssetCreationViewModels.Clear();
                FolderCreationViewModels.Clear();

                e.Handled = true;
                return;
            }
            if (e.Key == Key.Enter)
            {
                ContentBrowserFolderViewModel folder = (ContentBrowserFolderViewModel)SelectedFolderListBox.DataContext;
                if (((FrameworkElement)sender).DataContext is FolderCreationViewModel folderCreationViewModel)
                {
                    try
                    {
                        AssetsRegistry.CreateFolder(folder.FullPath, folderCreationViewModel.Name);
                    }
                    catch (Exception exception)
                    {
                        Logger.Log(LogType.Error, "Error during folder creation: " + exception.Message);
                    }
                }
                if (((FrameworkElement)sender).DataContext is AssetCreationViewModel assetCreationViewModel)
                {
                    try
                    {
                        if (assetCreationViewModel.Type == typeof(SceneData))
                            AssetsRegistry.CreateAsset<SceneData>(assetCreationViewModel.Name, folder.FullPath);
                        else if (assetCreationViewModel.Type == typeof(MaterialData))
                            AssetsRegistry.CreateAsset<MaterialData>(assetCreationViewModel.Name, folder.FullPath);
                        //else if (assetCreationViewModel.Type == typeof(ScriptData))
                        //    AssetsRegistry.CreateAsset<ScriptData>(assetCreationViewModel.Name, folder.FullPath);
                    }
                    catch (Exception exception)
                    {
                        Logger.Log(LogType.Error, "Error during asset creation: " + exception.Message);
                    }
                }

                AssetCreationViewModels.Clear();
                FolderCreationViewModels.Clear();

                folder.Refresh();

                e.Handled = true;

                return;
            }
        }

        private void FocusSelfOnLoad(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).Focus();
        }

        private void FoldersTreeView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            bool res = Focus();
        }

        private void SendSelectedAssetToInspector()
        {
            ContentBrowserAssetViewModel asset = SelectedFolderListBox.SelectedItem as ContentBrowserAssetViewModel;
            if (asset is null || asset.IsSubAsset)
            {
                EditorLayer.Current.InspectedObject = null;
                return;
            }

            EditorLayer.Current.InspectedObject = new InspectorAssetViewModel(asset);
        }

        private void TreeViewFolder_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;

            if ((string[])e.Data.GetData(DataFormats.FileDrop) is not null ||
                e.Data.GetData(DataFormats.Serializable) is ContentBrowserAssetViewModel or ContentBrowserFolderViewModel)
                e.Effects = DragDropEffects.Move;
            else
                e.Effects = DragDropEffects.None;
        }

        private void SelectedFolderListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;

            if ((string[])e.Data.GetData(DataFormats.FileDrop) is not null ||
                e.Data.GetData(DataFormats.Serializable) is ContentBrowserAssetViewModel or ContentBrowserFolderViewModel)
                e.Effects = DragDropEffects.Move;
            else
                e.Effects = DragDropEffects.None;
        }

        private void FolderItem_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;

            if ((string[])e.Data.GetData(DataFormats.FileDrop) is not null ||
                e.Data.GetData(DataFormats.Serializable) is ContentBrowserAssetViewModel or ContentBrowserFolderViewModel)
                e.Effects = DragDropEffects.Move;
            else
                e.Effects = DragDropEffects.None;
        }
    }
}