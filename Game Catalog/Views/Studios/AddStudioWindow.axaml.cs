using Avalonia.Controls;
using Avalonia.Input;
using Game_Catalog.ViewModels;
using System;
using System.Linq;
using Avalonia.Threading;
namespace Game_Catalog.Views;

public partial class AddStudioWindow : Window
{
    public AddStudioWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is AddStudioViewModel vm)
            vm.CloseRequested += Close;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (DataContext is not AddStudioViewModel vm) { base.OnKeyDown(e); return; }
        if (e.Key == Key.Escape) { vm.CancelCommand.Execute(null); e.Handled = true; }
        else if (e.Key == Key.Enter && !e.Handled) { vm.ConfirmCommand.Execute(null); e.Handled = true; }
        base.OnKeyDown(e);
    }
    private void OnCountryFlyoutOpened(object? sender, EventArgs e)
    {
        if (DataContext is AddStudioViewModel vm)
            vm.ResetCountrySearch();

        if (sender is Flyout { Content: StackPanel panel } &&
            panel.Children.OfType<TextBox>().FirstOrDefault() is { } box)
        {
            Dispatcher.UIThread.Post(() => box.Focus());
        }
    }

    private void OnCountrySelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox { SelectedItem: string country } list) return;
        if (DataContext is AddStudioViewModel vm)
            vm.PickCountry(country);

        list.SelectedItem = null;
        CountryButton.Flyout?.Hide();
    }
}