using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace Editor
{
    /// <summary>
    /// Interaction logic for ToolsHost.xaml
    /// </summary>
    [ContentProperty("Items")]
    public partial class ToolsHost : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(ToolsHost));
        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }
        public ItemCollection Items => ToolsTabControl.Items;

        private RelayCommand closeButton_ClickCommand;
        public RelayCommand CloseButton_ClickCommand => closeButton_ClickCommand ?? (closeButton_ClickCommand = new RelayCommand(obj => ToolsTabControl.Items.Remove(obj)));
        private RelayCommand contentBackground_MouseDownCommand;
        public RelayCommand ContentBackground_MouseDownCommand => contentBackground_MouseDownCommand ?? (contentBackground_MouseDownCommand = new RelayCommand(obj => (obj as RoutedEventArgs).Handled = true));
        private RelayCommand headerBackground_MouseDownCommand;

        public RelayCommand HeaderBackground_MouseDownCommand => headerBackground_MouseDownCommand ?? (headerBackground_MouseDownCommand =
            new RelayCommand(obj =>
            {
                Tuple<object, EventArgs> data = obj as Tuple<object, EventArgs>;
                HeaderBackground_MouseDown(data.Item1, data.Item2 as MouseButtonEventArgs);
            }));

        private RelayCommand headerBackground_MouseMoveCommand;

        public RelayCommand HeaderBackground_MouseMoveCommand => headerBackground_MouseMoveCommand ?? (headerBackground_MouseMoveCommand =
            new RelayCommand(obj =>
            {
                Tuple<object, EventArgs> data = obj as Tuple<object, EventArgs>;
                HeaderBackground_MouseMove(data.Item1, data.Item2 as MouseEventArgs);
            }));

        private RelayCommand tabPanel_PreviewMouseWheelCommand;

        public RelayCommand TabPanel_PreviewMouseWheelCommand => tabPanel_PreviewMouseWheelCommand ?? (tabPanel_PreviewMouseWheelCommand =
            new RelayCommand(obj =>
            {
                Tuple<object, EventArgs> data = obj as Tuple<object, EventArgs>;
                TabPanel_PreviewMouseWheel(data.Item1, data.Item2 as MouseWheelEventArgs);
            }));

        public event Action<ToolsHost> OnBecameEmpty;

        private bool isGrabbed = false;
        private Point localGrabPos;
        private readonly double DragThreshold = 5;

        public ToolsHost()
        {
            InitializeComponent();

            DataContext = this;

            ((INotifyCollectionChanged)Items).CollectionChanged += ToolsHost_CollectionChanged;
        }

        public void MoveTabsTo(ToolsHost target)
        {
            while (!Items.IsEmpty)
            {
                object item = Items[0];
                Items.Remove(item);
                target.Items.Add(item);
            }
        }

        private void TabPanel_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                (sender as ScrollViewer).LineLeft();
            else
                (sender as ScrollViewer).LineRight();
        }

        private void HeaderBackground_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Items.Count < 2)
            {
                DockableHost host = this.FindParent<DockableHost>();
                IList<DependencyObject> ancestors = host.FindParentWithPath<DockableHost>();
                int ancestorsCountToDockableHost = ancestors != null ? ancestors.Count : int.MaxValue;
                ancestors = host.FindParentWithPath<FlyingControl>();
                int ancestorsCountToFlyingControl = ancestors != null ? ancestors.Count : int.MaxValue;
                if (ancestorsCountToFlyingControl < ancestorsCountToDockableHost)
                    return;
            }
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                isGrabbed = true;
                localGrabPos = e.GetPosition(this);

                if (ToolsTabControl.SelectedItem == sender)
                    e.Handled = true;
            }
        }

        private void HeaderBackground_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
                isGrabbed = false;

            if (!isGrabbed)
                return;

            e.Handled = true;

            if ((e.GetPosition(this) - localGrabPos).LengthSquared >= DragThreshold * DragThreshold)
            {
                DockingManager dockingManager = this.FindParent<DockingManager>();
                if (dockingManager == null)
                    throw new Exception("Invalid UI configuration, no DockingManager found, ToolsHost must be a child of DockingManager.");

                FlyingControl flyingControl = new FlyingControl();
                Point spawnLocation = (Point)(e.GetPosition(dockingManager) - localGrabPos);
                flyingControl.Margin = new Thickness(spawnLocation.X, spawnLocation.Y, 0, 0);
                flyingControl.Width = ActualWidth;
                flyingControl.Height = ActualHeight;
                Items.Remove(sender);
                flyingControl.Items.Add(sender as FrameworkElement);
                dockingManager.AddFlyingControl(flyingControl);
                flyingControl.InitiateDrag(localGrabPos);
            }
        }

        private void ToolsHost_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Items.Count == 0)
                OnBecameEmpty?.Invoke(this);
        }
    }
}