using System;
using System.Windows;
using System.Windows.Input;

using Microsoft.Xaml.Behaviors;

namespace Editor
{
    public sealed class EventToCommandAction : TriggerAction<DependencyObject>
    {
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register("Command", typeof(ICommand), typeof(EventToCommandAction), null);

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty SenderProperty = DependencyProperty.Register("Sender", typeof(object), typeof(EventToCommandAction), null);
        public object Sender
        {
            get => GetValue(SenderProperty);
            set => SetValue(SenderProperty, value);
        }

        protected override void Invoke(object parameter)
        {
            if (AssociatedObject == null)
                return;

            ICommand command = Command;
            if (command != null)
            {
                Tuple<object, EventArgs> args = new Tuple<object, EventArgs>(Sender, (EventArgs)parameter);
                if (command.CanExecute(args))
                    command.Execute(args);
            }
        }
    }
}