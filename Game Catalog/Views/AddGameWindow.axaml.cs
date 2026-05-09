using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
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
    /// <summary>Forwards the selected RAWG suggestion to the ViewModel command.</summary>
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
}