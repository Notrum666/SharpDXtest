using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace Editor
{
    /// <summary>
    /// Interaction logic for DockingManager.xaml
    /// </summary>
    [ContentProperty("Items")]
    public partial class DockingManager : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = ItemsControl.ItemsSourceProperty.AddOwner(typeof(DockingManager));
        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }
        public ItemCollection Items => itemsControl.Items;

        private List<FlyingControl> flyingControls = new List<FlyingControl>();
        public ReadOnlyCollection<FlyingControl> FlyingControls => flyingControls.AsReadOnly();

        public DockingManager()
        {
            InitializeComponent();

            DataContext = this;
        }

        public void AddFlyingControl(FlyingControl control)
        {
            if (flyingControls.Contains(control))
                flyingControls.Remove(control);
            else
            {
                control.Loaded += FlyingControl_Loaded;
                control.PreviewMouseDown += FlyingControl_PreviewMouseDown;
                control.OnBecameEmpty += FlyingControl_OnBecameEmpty;
                Items.Add(control);
            }

            flyingControls.Add(control);
            updateFlyingControlsOrder();
        }

        private void FlyingControl_Loaded(object sender, RoutedEventArgs e)
        {
            FlyingControl control = sender as FlyingControl;
            control.Loaded -= FlyingControl_Loaded;
            control.Width = control.ActualWidth;
            control.Height = control.ActualHeight;
            if (control.Margin.Left == 0 && control.Margin.Top == 0)
                control.Margin = new Thickness((ActualWidth - control.ActualWidth) / 2.0, (ActualHeight - control.ActualHeight) / 2.0, 0.0, 0.0);
        }

        private void FlyingControl_OnBecameEmpty(FlyingControl sender)
        {
            sender.OnBecameEmpty -= FlyingControl_OnBecameEmpty;
            flyingControls.Remove(sender);
            Items.Remove(sender);
            updateFlyingControlsOrder();
        }

        private void FlyingControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            AddFlyingControl(sender as FlyingControl);
        }

        private void updateFlyingControlsOrder()
        {
            for (int i = 0; i < flyingControls.Count; i++)
                Panel.SetZIndex(flyingControls[i], i + 10000);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (UIElement child in Items)
            {
                if (child is FlyingControl flyingControl)
                {
                    flyingControl.Loaded += FlyingControl_Loaded;
                    flyingControl.OnBecameEmpty += FlyingControl_OnBecameEmpty;
                    flyingControl.PreviewMouseDown += FlyingControl_PreviewMouseDown;
                    flyingControls.Add(flyingControl);
                }
            }

            updateFlyingControlsOrder();
        }
    }
}