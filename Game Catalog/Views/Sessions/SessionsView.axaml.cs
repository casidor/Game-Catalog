using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Game_Catalog.ViewModels;
using System;
using System.Linq;

namespace Game_Catalog.Views;

public partial class SessionsView : UserControl
{
    public SessionsView()
    {
        InitializeComponent();
    }

    /// <summary> Closes the filter panel without applying when the dimmed backdrop is clicked. </summary>
    private void OnOverlayPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is SessionsViewModel vm)
            vm.CancelFiltersCommand.Execute(null);
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
}
