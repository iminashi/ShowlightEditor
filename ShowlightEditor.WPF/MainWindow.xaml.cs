﻿using ReactiveUI;

using ShowlightEditor.Core;
using ShowlightEditor.Core.ViewModels;
using ShowlightEditor.WPF.Services;

using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ShowlightEditor.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IViewFor<MainWindowViewModel>
    {
        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (MainWindowViewModel)value;
        }

        public MainWindowViewModel ViewModel
        {
            get => (MainWindowViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(MainWindowViewModel), typeof(MainWindow));

        public MainWindow()
        {
            //Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            DataContext = ViewModel = new MainWindowViewModel(new WPFServices());

            InitializeComponent();

            ViewModel.ScrollIntoView
                .ObserveOnDispatcher()
                .Subscribe(slListView.ScrollIntoView);

            ViewModel.UndoManager.UndoDescription
                .BindTo(this, x => x.UndoDescription.Text);

            ViewModel.UndoManager.RedoDescription
                .BindTo(this, x => x.RedoDescription.Text);

            ViewModel.UndoManager.AffectedObject
                .Where(sl => sl is not null)
                .ObserveOnDispatcher()
                .Subscribe(SelectAndScrollIntoView);
        }

        private void SelectAndScrollIntoView(ShowLightViewModel showlight)
        {
            slListView.SelectedItem = showlight;
            slListView.ScrollIntoView(showlight);
        }

        private void ShowlightsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => ViewModel.SelectedItems = slListView.SelectedItems;

        private void Window_Closing(object sender, CancelEventArgs e)
            => e.Cancel = ViewModel.ConfirmSaveChanges() == ConfirmSaveResult.Cancel;

        private void Exit_Click(object sender, RoutedEventArgs e)
            => Close();

        private void Window_Closed(object sender, EventArgs e)
            => ViewModel.SavePreferences();

        private void EditMenuOpening(object sender, RoutedEventArgs e)
        {
            bool clipboardHasData = Clipboard.ContainsData(WPFServices.ClipboardDataFormat);
            pasteRepMenuItem.IsEnabled = pasteInsMenuItem.IsEnabled = clipboardHasData;
        }

        private void ListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            bool clipboardHasData = Clipboard.ContainsData(WPFServices.ClipboardDataFormat);
            pasteRepContextMenuItem.IsEnabled = pasteInsContextMenuItem.IsEnabled = clipboardHasData;
        }
    }
}
