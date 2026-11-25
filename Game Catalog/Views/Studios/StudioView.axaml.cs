using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Game_Catalog.Models;
using Game_Catalog.ViewModels;

namespace Game_Catalog.Views;

public partial class StudioView : UserControl
{
    /// <summary> Header clicks and sort arrows of the studios table. </summary>
    private readonly DataGridSortHeaders _sortHeaders;

    public StudioView()
    {
        InitializeComponent();

        _sortHeaders = new DataGridSortHeaders(StudiosGrid,
            () => DataContext is StudioViewModel vm
                ? ((object?)vm.SortField, vm.SortDescending)
                : ((object?)null, false),
            field =>
            {
                if (DataContext is StudioViewModel vm && field is StudioSortField sortField)
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
        if (DataContext is StudioViewModel vm)
            vm.CancelFiltersCommand.Execute(null);
    }
}