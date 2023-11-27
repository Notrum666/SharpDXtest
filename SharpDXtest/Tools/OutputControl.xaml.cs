using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Engine;

namespace Editor
{
    /// <summary>
    /// Interaction logic for OutputControl.xaml
    /// </summary>
    public partial class OutputControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private RelayCommand clearOutputCommand;
        public RelayCommand ClearOutputCommand => clearOutputCommand ?? (clearOutputCommand = new RelayCommand(obj => LogMessages.Clear()));
        public ObservableCollection<LogMessage> LogMessages { get; private set; } = new ObservableCollection<LogMessage>();
        private List<LogMessage> newMessages = new List<LogMessage>();
        public int InfoCount => LogMessages.Count(msg => msg.Type == LogType.Info);
        public int WarningCount => LogMessages.Count(msg => msg.Type == LogType.Warning);
        public int ErrorCount => LogMessages.Count(msg => msg.Type == LogType.Error);
        private bool showInfoMessages = true;
        public bool ShowInfoMessages
        {
            get => showInfoMessages;
            set
            {
                showInfoMessages = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowLogTypeSelector));
            }
        }
        private bool showWarningMessages = true;
        public bool ShowWarningMessages
        {
            get => showWarningMessages;
            set
            {
                showWarningMessages = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowLogTypeSelector));
            }
        }
        private bool showErrorMessages = true;
        public bool ShowErrorMessages
        {
            get => showErrorMessages;
            set
            {
                showErrorMessages = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowLogTypeSelector));
            }
        }
        public Dictionary<LogType, Func<bool>> ShowLogTypeSelector { get; private set; }
        private bool loaded = false;
        private readonly Dictionary<LogType, Action> CountPropertyChangedLambdas;
        private DispatcherTimer updateTimer;

        public OutputControl()
        {
            InitializeComponent();

            DataContext = this;
            CountPropertyChangedLambdas = new Dictionary<LogType, Action>()
            {
                [LogType.Info] = () => OnPropertyChanged(nameof(InfoCount)),
                [LogType.Warning] = () => OnPropertyChanged(nameof(WarningCount)),
                [LogType.Error] = () => OnPropertyChanged(nameof(ErrorCount))
            };
            ShowLogTypeSelector = new Dictionary<LogType, Func<bool>>()
            {
                [LogType.Info] = () => ShowInfoMessages,
                [LogType.Warning] = () => ShowWarningMessages,
                [LogType.Error] = () => ShowErrorMessages
            };

            LogMessages.CollectionChanged += LogMessages_CollectionChanged;

            updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromMilliseconds(50);
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            List<LogMessage> copy = new List<LogMessage>();
            copy = Interlocked.Exchange(ref newMessages, copy);
            foreach (LogMessage message in copy)
                LogMessages.Insert(0, message);
        }

        private void LogMessages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (LogMessage msg in e.NewItems)
                    CountPropertyChangedLambdas[msg.Type]();
                return;
            }
            foreach (LogType type in CountPropertyChangedLambdas.Keys)
                CountPropertyChangedLambdas[type]();
        }

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
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
}