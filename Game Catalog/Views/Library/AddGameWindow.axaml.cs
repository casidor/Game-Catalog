using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Game_Catalog.Models;
using Game_Catalog.ViewModels;
using System;
using System.Linq;

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

    /// <summary> Clears the search box of an opened dropdown and puts the focus into it. </summary>
    private void OnFlyoutOpened(object? sender, EventArgs e)
    {
        if (sender is not Flyout { Content: StackPanel panel } ||
            panel.Children.OfType<TextBox>().FirstOrDefault() is not { } box)
            return;

        box.Text = string.Empty;
        Dispatcher.UIThread.Post(() => box.Focus());
    }

    /// <summary> Applies the studio picked in the list and closes the dropdown. </summary>
    private void OnStudioSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox { SelectedItem: Studio studio } list) return;
        if (DataContext is AddGameViewModel vm)
            vm.PickStudio(studio);

        list.SelectedItem = null;
        StudioButton.Flyout?.Hide();
    }
}