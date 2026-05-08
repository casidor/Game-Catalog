using Avalonia;
using Avalonia.Controls;
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
}