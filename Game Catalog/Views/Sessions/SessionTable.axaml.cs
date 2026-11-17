using Avalonia;
using Avalonia.Controls;
using Game_Catalog.Models;
using Game_Catalog.ViewModels;

namespace Game_Catalog.Views;

public partial class SessionTable : UserControl
{
    /// <summary> Shows the game column. The sessions page needs it; the sessions tab of one game does not. </summary>
    public static readonly StyledProperty<bool> ShowGameColumnProperty =
        AvaloniaProperty.Register<SessionTable, bool>(nameof(ShowGameColumn), defaultValue: true);

    public bool ShowGameColumn
    {
        get => GetValue(ShowGameColumnProperty);
        set => SetValue(ShowGameColumnProperty, value);
    }

    /// <summary> Shows the context menu. Off for sessions that may only be viewed. </summary>
    public static readonly StyledProperty<bool> AllowChangesProperty =
        AvaloniaProperty.Register<SessionTable, bool>(nameof(AllowChanges), defaultValue: true);

    public bool AllowChanges
    {
        get => GetValue(AllowChangesProperty);
        set => SetValue(AllowChangesProperty, value);
    }

    /// <summary> Context menu of the grid, kept to put it back when changes are allowed again. </summary>
    private readonly ContextMenu? _menu;

    /// <summary> Header clicks and sort arrows of the sessions table. </summary>
    private readonly DataGridSortHeaders _sortHeaders;

    public SessionTable()
    {
        InitializeComponent();
        _menu = SessionsGrid.ContextMenu;

        _sortHeaders = new DataGridSortHeaders(SessionsGrid,
            () => DataContext is SessionTableViewModel vm
                ? ((object?)vm.SortField, vm.SortDescending)
                : ((object?)null, false),
            field =>
            {
                if (DataContext is SessionTableViewModel vm && field is SessionSortField sortField)
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
            SessionsGrid.Columns[0].IsVisible = ShowGameColumn;
        else if (change.Property == AllowChangesProperty)
            SessionsGrid.ContextMenu = AllowChanges ? _menu : null;
    }
}
