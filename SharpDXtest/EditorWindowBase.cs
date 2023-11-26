﻿using System;
using System.Windows;
using System.Windows.Input;
using System.Threading;
using System.Globalization;
using System.Windows.Interop;

using Engine;

namespace Editor
{
    public class EditorWindowBase : Window
    {
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register("Header", typeof(object), typeof(EditorWindowBase));
        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        public static readonly DependencyProperty FooterProperty = DependencyProperty.Register("Footer", typeof(object), typeof(EditorWindowBase));
        public object Footer
        {
            get { return GetValue(FooterProperty); }
            set { SetValue(FooterProperty, value); }
        }

        static EditorWindowBase()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EditorWindowBase), new FrameworkPropertyMetadata(typeof(EditorWindowBase)));
        }
    }
}