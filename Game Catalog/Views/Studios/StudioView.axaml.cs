using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
}