using SharpDX;
using SharpDX.Direct3D9;
using SharpDX.DXGI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Xps.Packaging;

namespace Editor
{
    /// <summary>
    /// Interaction logic for DockableHost.xaml
    /// </summary>
    [ContentProperty("Items")]
    public partial class DockableHost : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register("Items", 
            typeof(ObservableCollection<FrameworkElement>), typeof(DockableHost),
            new FrameworkPropertyMetadata(OnItemsPropertyChanged));
        public ObservableCollection<FrameworkElement> Items
        {
            get 
            { 
                return (ObservableCollection<FrameworkElement>)GetValue(ItemsProperty); 
            }
            set { SetValue(ItemsProperty, value); }
        }
        private static void OnItemsPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DockableHost control = sender as DockableHost;

            ObservableCollection<FrameworkElement> collection = e.OldValue as ObservableCollection<FrameworkElement>;

            if (collection != null)
                collection.CollectionChanged -= control.DockableHost_CollectionChanged;

            collection = e.NewValue as ObservableCollection<FrameworkElement>;

            if (collection != null)
            {
                collection.CollectionChanged += control.DockableHost_CollectionChanged;
                control.DockableHost_CollectionChanged(control, null);
            }
        }

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation", typeof(Orientation), typeof(DockableHost),
            new FrameworkPropertyMetadata(OnOrientationPropertyChanged));
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }
        private static void OnOrientationPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as DockableHost).UpdateConfiguration();
        }

        public static readonly DependencyProperty SplitLocationProperty = DependencyProperty.Register("SplitLocation", typeof(double), typeof(DockableHost),
            new FrameworkPropertyMetadata(0.5, OnSplitLocationPropertyChanged),
            new ValidateValueCallback(IsValidSplitLocation));
        public double SplitLocation
        {
            get { return (double)GetValue(SplitLocationProperty); }
            set { SetValue(SplitLocationProperty, value); }
        }
        private static bool IsValidSplitLocation(object obj)
        {
            double value = (double)obj;
            return value >= 0.0 && value <= 1.0;
        }
        private static void OnSplitLocationPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DockableHost obj = (DockableHost)sender;
            double clampedValue = Math.Clamp((double)e.NewValue, 0.1, 0.9);
            if (obj.Orientation == Orientation.Horizontal)
            {
                double width1 = obj.ContentGrid.ColumnDefinitions[0].ActualWidth;
                if (width1 == 0.0)
                    width1 = obj.ContentGrid.ColumnDefinitions[0].Width.Value;
                double width2 = obj.ContentGrid.ColumnDefinitions[2].ActualWidth;
                if (width2 == 0.0)
                    width2 = obj.ContentGrid.ColumnDefinitions[2].Width.Value;
                double totalWidth = width1 + width2;
                obj.ContentGrid.ColumnDefinitions[0].Width = new GridLength(totalWidth * clampedValue, GridUnitType.Star);
                obj.ContentGrid.ColumnDefinitions[2].Width = new GridLength(totalWidth * (1.0 - clampedValue), GridUnitType.Star);
            }
            else
            {
                double height1 = obj.ContentGrid.RowDefinitions[0].ActualHeight;
                if (height1 == 0.0)
                    height1 = obj.ContentGrid.RowDefinitions[0].Height.Value;
                double height2 = obj.ContentGrid.RowDefinitions[2].ActualHeight;
                if (height2 == 0.0)
                    height2 = obj.ContentGrid.RowDefinitions[2].Height.Value;
                double totalHeight = height1 + height2;
                obj.ContentGrid.RowDefinitions[0].Height = new GridLength(totalHeight * clampedValue, GridUnitType.Star);
                obj.ContentGrid.RowDefinitions[2].Height = new GridLength(totalHeight * (1.0 - clampedValue), GridUnitType.Star);
            }
        }

        public event Action<DockableHost> OnConfigurationUpdated;

        private bool showDockingOverlay = false;
        public bool ShowDockingOverlay
        {
            get => showDockingOverlay;
            set
            {
                showDockingOverlay = value;
                OnPropertyChanged();
            }
        }
        private bool showPlaceholder = false;
        public bool ShowPlaceholder
        {
            get => showPlaceholder;
            set
            {
                showPlaceholder = value;
                OnPropertyChanged();
            }
        }
        public bool IsLocked { get; set; } = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public DockableHost()
        {
            InitializeComponent();

            DataContext = this;

            Items = new ObservableCollection<FrameworkElement>();
        }
        private void MoveDataTo(DockableHost target, bool targetIsAnAncestor)
        {
            if (targetIsAnAncestor)
                target.IsLocked = true;
            target.Orientation = Orientation;
            target.SplitLocation = SplitLocation;
            ObservableCollection<FrameworkElement> items = new ObservableCollection<FrameworkElement>(Items);
            Items.Clear();
            target.IsLocked = false;
            target.Items = items;
        }
        private void MoveDataTo(ToolsHost target)
        {
            if (Items.Count == 1)
            {
                (Items[0] as ToolsHost).MoveTabsTo(target);
                return;
            }
            if (Items.Count == 2)
            {
                IsLocked = true;
                ObservableCollection<FrameworkElement> copy = new ObservableCollection<FrameworkElement>(Items);
                foreach (FrameworkElement item in copy)
                    (item as DockableHost).MoveDataTo(target);
                IsLocked = false;
                UpdateConfiguration();
            }
        }
        private DockableHost CreateCopyAndStealItems()
        {
            DockableHost copy = new DockableHost();
            MoveDataTo(copy, false);
            return copy;
        }
        private void UpdateConfiguration()
        {
            if (IsLocked)
                return;

            for (int i = 0; i < ContentGrid.Children.Count; i++)
                if (ContentGrid.Children[i] != ColumnsSplitter && ContentGrid.Children[i] != RowsSplitter)
                {
                    if (ContentGrid.Children[i] is DockableHost dockableHost)
                        dockableHost.OnConfigurationUpdated -= DockableHostItem_OnConfigurationUpdated;
                    if (ContentGrid.Children[i] is ToolsHost toolsHost)
                        toolsHost.OnBecameEmpty -= ToolsHostItem_OnBecameEmpty;
                    ContentGrid.Children.RemoveAt(i);
                    i--;
                }

            if (Items.Count > 2)
                throw new Exception("Dockable host can't have more than 2 children.");

            switch (Items.Count)
            {
                case 1:
                    if (Items[0] is not ToolsHost)
                    {
                        if (Items[0] is DockableHost dockableHost)
                        {
                            dockableHost.MoveDataTo(this, true);
                            return;
                        }
                        ToolsHost wrapper = new ToolsHost();
                        wrapper.Items.Add(Items[0]);
                        Items[0] = wrapper;
                        return;
                    }

                    ContentGrid.Children.Add(Items[0]);
                    Grid.SetColumnSpan(Items[0], 3);
                    Grid.SetRowSpan(Items[0], 3);
                    Grid.SetColumn(Items[0], 0);
                    Grid.SetRow(Items[0], 0);

                    ColumnsSplitter.Visibility = Visibility.Collapsed;
                    RowsSplitter.Visibility = Visibility.Collapsed;

                    (Items[0] as ToolsHost).OnBecameEmpty += ToolsHostItem_OnBecameEmpty;
                    break;
                case 2:
                    for (int i = 0; i < 2; i++)
                        if (Items[i] is not DockableHost)
                        {
                            DockableHost wrapper = new DockableHost();
                            wrapper.Items.Add(Items[i]);
                            Items[i] = wrapper;
                            return;
                        }

                    Orientation orientation = Orientation;

                    for (int i = 0; i < 2; i++)
                    {
                        ContentGrid.Children.Add(Items[i]);
                        Grid.SetColumnSpan(Items[i], orientation == Orientation.Horizontal ? 1 : 3);
                        Grid.SetRowSpan(Items[i], orientation == Orientation.Vertical ? 1 : 3);
                    }
                    Grid.SetColumn(Items[0], 0);
                    Grid.SetRow(Items[0], 0);
                    Grid.SetColumn(Items[1], orientation == Orientation.Horizontal ? 2 : 0);
                    Grid.SetRow(Items[1], orientation == Orientation.Vertical ? 2 : 0);

                    ColumnsSplitter.Visibility = (orientation == Orientation.Horizontal ? Visibility.Visible : Visibility.Collapsed);
                    RowsSplitter.Visibility = (orientation == Orientation.Vertical ? Visibility.Visible : Visibility.Collapsed);

                    (Items[0] as DockableHost).OnConfigurationUpdated += DockableHostItem_OnConfigurationUpdated;
                    (Items[1] as DockableHost).OnConfigurationUpdated += DockableHostItem_OnConfigurationUpdated;
                    break;
            }

            OnConfigurationUpdated?.Invoke(this);
        }

        private void ToolsHostItem_OnBecameEmpty(ToolsHost sender)
        {
            sender.OnBecameEmpty -= ToolsHostItem_OnBecameEmpty;
            Items.Remove(sender);
        }

        private void DockableHost_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateConfiguration();
        }

        private void DockableHostItem_OnConfigurationUpdated(DockableHost sender)
        {
            if (sender.Items.Count == 0)
                Items.Remove(sender);
        }

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        private void UserControl_DragEnter(object sender, DragEventArgs e)
        {
            object data = e.Data.GetData(DataFormats.Serializable);
            if (data is not FlyingControl || Items.Count > 1)
                return;

            ShowDockingOverlay = true;
        }

        private void UserControl_DragLeave(object sender, DragEventArgs e)
        {
            object data = e.Data.GetData(DataFormats.Serializable);
            if (data is not FlyingControl || Items.Count > 1)
                return;
            
            ShowDockingOverlay = false;
        }

        private void displayPlaceholder(int row, int rowSpan, int column, int columnSpan)
        {
            ShowPlaceholder = true;
            Grid.SetRow(Placeholder, row);
            Grid.SetRowSpan(Placeholder, rowSpan);
            Grid.SetColumn(Placeholder, column);
            Grid.SetColumnSpan(Placeholder, columnSpan);
        }

        private void DockingCenter_DragEnter(object sender, DragEventArgs e)
        {
            displayPlaceholder(0, 2, 0, 2);
        }

        private void DockingLeft_DragEnter(object sender, DragEventArgs e)
        {
            displayPlaceholder(0, 2, 0, 1);
        }

        private void DockingTop_DragEnter(object sender, DragEventArgs e)
        {
            displayPlaceholder(0, 1, 0, 2);
        }

        private void DockingRight_DragEnter(object sender, DragEventArgs e)
        {
            displayPlaceholder(0, 2, 1, 1);
        }

        private void DockingBottom_DragEnter(object sender, DragEventArgs e)
        {
            displayPlaceholder(1, 1, 0, 2);
        }

        private void DockingSpot_DragLeave(object sender, DragEventArgs e)
        {
            ShowPlaceholder = false;
        }

        private void DockingCenter_Drop(object sender, DragEventArgs e)
        {
            ShowPlaceholder = false;
            object obj = e.Data.GetData(DataFormats.Serializable);
            if (obj is FlyingControl flyingControl)
                obj = flyingControl.FlyingControlContentHost;

            switch (Items.Count)
            {
                case 0:
                    (obj as DockableHost).MoveDataTo(this, false);
                    break;
                case 1:
                    (obj as DockableHost).MoveDataTo(Items[0] as ToolsHost);
                    break;
                default:
                    throw new Exception("Something went wrong, DockingCenter_Drop was called when it should not be visible.");
            }
        }

        private void DockingLeft_Drop(object sender, DragEventArgs e)
        {
            ShowPlaceholder = false;
            Orientation = Orientation.Horizontal;
            Items.Insert(0, (e.Data.GetData(DataFormats.Serializable) as FlyingControl).FlyingControlContentHost.CreateCopyAndStealItems());
        }

        private void DockingTop_Drop(object sender, DragEventArgs e)
        {
            ShowPlaceholder = false;
            Orientation = Orientation.Vertical;
            Items.Insert(0, (e.Data.GetData(DataFormats.Serializable) as FlyingControl).FlyingControlContentHost.CreateCopyAndStealItems());
        }

        private void DockingRight_Drop(object sender, DragEventArgs e)
        {
            ShowPlaceholder = false;
            Orientation = Orientation.Horizontal;
            Items.Insert(1, (e.Data.GetData(DataFormats.Serializable) as FlyingControl).FlyingControlContentHost.CreateCopyAndStealItems());
        }

        private void DockingBottom_Drop(object sender, DragEventArgs e)
        {
            ShowPlaceholder = false;
            Orientation = Orientation.Vertical;
            Items.Insert(1, (e.Data.GetData(DataFormats.Serializable) as FlyingControl).FlyingControlContentHost.CreateCopyAndStealItems());
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            ShowDockingOverlay = false;
        }

        private void ColumnsSplitter_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            SplitLocation = ContentGrid.ColumnDefinitions[0].ActualWidth /
                (ContentGrid.ColumnDefinitions[0].ActualWidth + ContentGrid.ColumnDefinitions[2].ActualWidth);
        }

        private void RowsSplitter_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            SplitLocation = ContentGrid.RowDefinitions[0].ActualHeight /
                (ContentGrid.RowDefinitions[0].ActualHeight + ContentGrid.RowDefinitions[2].ActualHeight);
        }
    }
}
