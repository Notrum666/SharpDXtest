using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;

namespace Editor
{
    /// <summary>
    /// Interaction logic for FlyingControl.xaml
    /// </summary>
    [ContentProperty("Items")]
    public partial class FlyingControl : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register("Items",
                                                                                              typeof(ObservableCollection<FrameworkElement>), typeof(FlyingControl));
        public ObservableCollection<FrameworkElement> Items
        {
            get => (ObservableCollection<FrameworkElement>)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        private RelayCommand closeFlyingControlCommand;
        public RelayCommand CloseFlyingControlCommand => closeFlyingControlCommand ?? (closeFlyingControlCommand = new RelayCommand(obj => Close()));

        public event PropertyChangedEventHandler PropertyChanged;

        public event Action<FlyingControl> OnBecameEmpty;

        private Point holdDelta;

        public FlyingControl()
        {
            InitializeComponent();

            Items = new ObservableCollection<FrameworkElement>();

            DataContext = this;

            FlyingControlContentHost.OnConfigurationUpdated += FlyingControlContentHost_OnConfigurationUpdated;
        }

        private void FlyingControlContentHost_OnConfigurationUpdated(DockableHost sender)
        {
            if (sender.Items.Count == 0)
                OnBecameEmpty?.Invoke(this);
        }

        public void Close()
        {
            Items.Clear();
        }

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private void shift(double horizontal, double vertical)
        {
            Margin = new Thickness(Margin.Left + horizontal, Margin.Top + vertical, 0, 0);
        }

        private void Thumb_DragDelta_TopLeft(object sender, DragDeltaEventArgs e)
        {
            double newHeight = Math.Max(MinHeight, ActualHeight - e.VerticalChange);
            double newWidth = Math.Max(MinWidth, ActualWidth - e.HorizontalChange);
            shift(ActualWidth - newWidth, ActualHeight - newHeight);
            Height = newHeight;
            Width = newWidth;
        }

        private void Thumb_DragDelta_Top(object sender, DragDeltaEventArgs e)
        {
            double newHeight = Math.Max(MinHeight, ActualHeight - e.VerticalChange);
            shift(0, ActualHeight - newHeight);
            Height = newHeight;
        }

        private void Thumb_DragDelta_TopRight(object sender, DragDeltaEventArgs e)
        {
            double newHeight = Math.Max(MinHeight, ActualHeight - e.VerticalChange);
            Width = Math.Max(MinWidth, ActualWidth + e.HorizontalChange);
            shift(0, ActualHeight - newHeight);
            Height = newHeight;
        }

        private void Thumb_DragDelta_Right(object sender, DragDeltaEventArgs e)
        {
            Width = Math.Max(MinWidth, ActualWidth + e.HorizontalChange);
        }

        private void Thumb_DragDelta_BottomRight(object sender, DragDeltaEventArgs e)
        {
            Height = Math.Max(MinHeight, ActualHeight + e.VerticalChange);
            Width = Math.Max(MinWidth, ActualWidth + e.HorizontalChange);
        }

        private void Thumb_DragDelta_Bottom(object sender, DragDeltaEventArgs e)
        {
            Height = Math.Max(MinHeight, ActualHeight + e.VerticalChange);
        }

        private void Thumb_DragDelta_BottomLeft(object sender, DragDeltaEventArgs e)
        {
            Height = Math.Max(MinHeight, ActualHeight + e.VerticalChange);
            double newWidth = Math.Max(MinWidth, ActualWidth - e.HorizontalChange);
            shift(ActualWidth - newWidth, 0);
            Width = newWidth;
        }

        private void Thumb_DragDelta_Left(object sender, DragDeltaEventArgs e)
        {
            double newWidth = Math.Max(MinWidth, ActualWidth - e.HorizontalChange);
            shift(ActualWidth - newWidth, 0);
            Width = newWidth;
        }

        private void ThisFlyingControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                e.Handled = true;

                InitiateDrag((Point)(e.GetPosition(Parent as UIElement) - new Point(Margin.Left, Margin.Top)));
            }
        }

        public void InitiateDrag(Point holdDelta)
        {
            this.holdDelta = holdDelta;

            UIElement parent = Parent as UIElement;
            parent.PreviewDragOver += Parent_PreviewDragOver;
            IsHitTestVisible = false;
            Opacity = 0.4;

            DragDrop.DoDragDrop(this, new DataObject(DataFormats.Serializable, this), DragDropEffects.All);

            parent.DragOver -= Parent_PreviewDragOver;
            IsHitTestVisible = true;
            Opacity = 1.0;
        }

        private void Parent_PreviewDragOver(object sender, DragEventArgs e)
        {
            object data = e.Data.GetData(DataFormats.Serializable);

            if (data is FlyingControl control)
            {
                Point pos = (Point)(e.GetPosition(sender as UIElement) - holdDelta);
                control.Margin = new Thickness(pos.X, pos.Y, 0, 0);
            }
        }

        protected override void OnGiveFeedback(GiveFeedbackEventArgs e)
        {
            e.Handled = true;
        }
    }
}