using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Game_Catalog.Models;
using Game_Catalog.Services;
using Game_Catalog.ViewModels;
using System;
using System.IO;
using System.Threading.Tasks;

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
    private async Task AddStudioAsync(AddGameViewModel vm, string? name = null)
    {
        var studioVm = new AddStudioViewModel();
        if (name != null) studioVm.Name = name;
        await new AddStudioWindow { DataContext = studioVm }.ShowDialog(this);
        if (!studioVm.Confirmed) return;

        try
        {
            var studio = studioVm.BuildStudio();
            DatabaseService.InsertStudio(studio);
            AppData.Instance.Studios.Add(studio);
            vm.SelectedStudio = studio;
        }
        catch (DatabaseException ex)
        {
            await ConfirmationWindow.ShowErrorAsync(this,
                "Помилка збереження студії", ex.Message, ex);
        }
    }

    private async void OnAddStudioClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is AddGameViewModel vm) await AddStudioAsync(vm);
    }

    private async void OnSuggestStudioClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is AddGameViewModel vm && sender is Button { Tag: string name })
            await AddStudioAsync(vm, name);
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
        {
            var source = files[0].Path.LocalPath;
            var ext = Path.GetExtension(source);
            Directory.CreateDirectory(RawgService.CoversFolder);
            var dest = Path.Combine(RawgService.CoversFolder, $"{Guid.NewGuid()}{ext}");
            File.Copy(source, dest, overwrite: true);
            vm.CoverImagePath = dest;
        }
    }

    private void OnClearCoverClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is AddGameViewModel vm)
            vm.CoverImagePath = string.Empty;
    }
}