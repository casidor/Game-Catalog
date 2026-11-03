using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Game_Catalog.ViewModels;

namespace Game_Catalog.Views;

public partial class StudioView : UserControl
{
    public StudioView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is StudioViewModel vm)
            vm.Load();
    }

    /// <summary> Closes the filter panel without applying when the dimmed backdrop is clicked. </summary>
    private void OnOverlayPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is StudioViewModel vm)
            vm.CancelFiltersCommand.Execute(null);
    }

    /// <summary> Opens the details of the studio whose row was double-clicked. </summary>
    private void OnRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is not Visual source) return;
        if (source.FindAncestorOfType<Button>(includeSelf: true) != null) return;
        if (source.FindAncestorOfType<DataGridRow>(includeSelf: true) is not { DataContext: StudioRow row }) return;

        if (DataContext is StudioViewModel vm)
            vm.OpenStudioCommand.Execute(row.Studio);
    }
}