﻿using System.Windows;

namespace Editor.GameProject
{
    /// <summary>
    /// Interaction logic for ProjectBrowserDialog.xaml
    /// </summary>
    public partial class ProjectBrowserDialogWindow : Window
    {
        public ProjectBrowserDialogWindow()
        {
            InitializeComponent();
        }

        private void CreateProjectButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenProjectButton.IsChecked = false;
            CreateProjectButton.IsChecked = true;

            BrowserContent.Margin = new Thickness(-800, 0, 0, 0);
        }

        private void OpenProjectButton_OnClick(object sender, RoutedEventArgs e)
        {
            CreateProjectButton.IsChecked = false;
            OpenProjectButton.IsChecked = true;

            BrowserContent.Margin = new Thickness(0, 0, 0, 0);
        }
    }
}