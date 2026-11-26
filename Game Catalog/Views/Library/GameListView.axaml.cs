using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Game_Catalog.Models;
using Game_Catalog.ViewModels;

namespace Game_Catalog.Views;

public partial class GamesListView : UserControl
{
    /// <summary> Header clicks and sort arrows of the games table. </summary>
    private readonly DataGridSortHeaders _sortHeaders;

    public GamesListView()
    {
        InitializeComponent();

        _sortHeaders = new DataGridSortHeaders(GamesGrid,
            () => DataContext is GamesListViewModel vm
                ? ((object?)vm.SortField, vm.SortDescending)
                : ((object?)null, false),
            field =>
            {
                if (DataContext is GamesListViewModel vm && field is GameSortField sortField)
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
        if (DataContext is GamesListViewModel vm)
            vm.CancelFiltersCommand.Execute(null);
    }

    /// <summary> Opens the game whose card was double-clicked; a double click on its checkbox only selects. </summary>
    private void OnCardDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is not Visual source) return;
        if (source.FindAncestorOfType<CheckBox>(includeSelf: true) != null) return;
        if (source.FindAncestorOfType<ListBoxItem>(includeSelf: true) is not { DataContext: GameRow row }) return;

        if (DataContext is GamesListViewModel vm)
            vm.OpenGameCommand.Execute(row);
    }
}