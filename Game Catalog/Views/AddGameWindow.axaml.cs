using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Game_Catalog.Models;
using Game_Catalog.ViewModels;
using System;

namespace Game_Catalog.Views;

public partial class AddGameWindow : Window
{
    public AddGameWindow()
    {
        InitializeComponent();
    }
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is AddGameViewModel vm)
            vm.CloseRequested += Close;
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (DataContext is not AddGameViewModel vm) { base.OnKeyDown(e); return; }
        if (e.Key == Key.Escape) { vm.CancelCommand.Execute(null); e.Handled = true; }
        else if (e.Key == Key.Enter && !e.Handled) { vm.ConfirmCommand.Execute(null); e.Handled = true; }
        base.OnKeyDown(e);
    }
    private void OnSuggestionSelected(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not AddGameViewModel vm) return;
        if (sender is not ListBox lb) return;
        if (lb.SelectedItem is not RawgGameResult result) return;

        lb.SelectedItem = null;
        vm.SelectSuggestionCommand.Execute(result);
    }
    private async void OnSuggestStudioClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not AddGameViewModel vm) return;
        if (sender is not Button btn || btn.Tag is not string devName) return;

        var studioVm = new AddStudioViewModel();
        studioVm.Name = devName;
        var window = new AddStudioWindow { DataContext = studioVm };
        await window.ShowDialog(this);

        if (studioVm.Confirmed)
        {
            var studio = studioVm.BuildStudio();
            AppData.Instance.Studios.Add(studio);
            vm.SelectedStudio = studio;
            vm.SuggestedDeveloperNames.Remove(devName);
        }
    }
    private async void OnAddStudioClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not AddGameViewModel vm) return;

        var studioVm = new AddStudioViewModel();
        var window = new AddStudioWindow { DataContext = studioVm };
        await window.ShowDialog(this);

        if (studioVm.Confirmed)
        {
            var studio = studioVm.BuildStudio();
            AppData.Instance.Studios.Add(studio);
            vm.SelectedStudio = studio;
        }
    }
    private async void OnPickCoverClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not AddGameViewModel vm) return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Оберіть обкладинку",
            AllowMultiple = false,
            FileTypeFilter = [new("Зображення") { Patterns = ["*.jpg", "*.jpeg", "*.png", "*.webp"] }]
        });

        if (files.Count > 0)
            vm.CoverImagePath = files[0].Path.LocalPath;
    }

    private void OnClearCoverClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is AddGameViewModel vm)
            vm.CoverImagePath = string.Empty;
    }
}