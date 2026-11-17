using Avalonia;
using Avalonia.Controls;
using Game_Catalog.Models;
using Game_Catalog.ViewModels;

namespace Game_Catalog.Views;

public partial class GoalTable : UserControl
{
    /// <summary> Shows the game column. The goals page needs it; the goals tab of one game does not. </summary>
    public static readonly StyledProperty<bool> ShowGameColumnProperty =
        AvaloniaProperty.Register<GoalTable, bool>(nameof(ShowGameColumn), defaultValue: true);

    public bool ShowGameColumn
    {
        get => GetValue(ShowGameColumnProperty);
        set => SetValue(ShowGameColumnProperty, value);
    }

    /// <summary> Shows the status buttons column and the context menu. Off for goals that may only be viewed. </summary>
    public static readonly StyledProperty<bool> AllowChangesProperty =
        AvaloniaProperty.Register<GoalTable, bool>(nameof(AllowChanges), defaultValue: true);

    public bool AllowChanges
    {
        get => GetValue(AllowChangesProperty);
        set => SetValue(AllowChangesProperty, value);
    }

    /// <summary> Context menu of the grid, kept to put it back when changes are allowed again. </summary>
    private readonly ContextMenu? _menu;

    /// <summary> Header clicks and sort arrows of the goals table. </summary>
    private readonly DataGridSortHeaders _sortHeaders;

    public GoalTable()
    {
        InitializeComponent();
        _menu = GoalsGrid.ContextMenu;

        _sortHeaders = new DataGridSortHeaders(GoalsGrid,
            () => DataContext is GoalTableViewModel vm
                ? ((object?)vm.SortField, vm.SortDescending)
                : ((object?)null, false),
            field =>
            {
                if (DataContext is GoalTableViewModel vm && field is GoalSortField sortField)
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

    /// <summary> Columns are not in the visual tree and do not take bindings, so they are toggled here. </summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ShowGameColumnProperty)
        {
            GoalsGrid.Columns[0].IsVisible = ShowGameColumn;
        }
        else if (change.Property == AllowChangesProperty)
        {
            GoalsGrid.Columns[^1].IsVisible = AllowChanges;
            GoalsGrid.ContextMenu = AllowChanges ? _menu : null;
        }
    }
}
