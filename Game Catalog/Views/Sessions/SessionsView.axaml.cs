using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Game_Catalog.Models;
using Game_Catalog.ViewModels;
using System;
using System.Linq;

namespace Game_Catalog.Views;

public partial class SessionsView : UserControl
{
    /// <summary> Header clicks and sort arrows of the sessions table. </summary>
    private readonly DataGridSortHeaders _sortHeaders;

    public SessionsView()
    {
        InitializeComponent();

        _sortHeaders = new DataGridSortHeaders(SessionsGrid,
            () => DataContext is SessionsViewModel vm
                ? ((object?)vm.SortField, vm.SortDescending)
                : ((object?)null, false),
            field =>
            {
                if (DataContext is SessionsViewModel vm && field is SessionSortField sortField)
                    vm.SortByColumn(sortField);
            });
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _sortHeaders.Attach(DataContext);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _sortHeaders.Detach();
        base.OnDetachedFromVisualTree(e);
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
