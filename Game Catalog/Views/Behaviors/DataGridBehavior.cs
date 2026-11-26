using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;

namespace Game_Catalog.Views;

/// <summary>
/// Shared table behavior, set up from XAML through attached properties:
/// keeps the selected rows in step with a list of the ViewModel both ways, opens a row on double click,
/// deletes the selection with Delete, clears the selection with Escape and selects rows on right click
/// the way a file manager does.
/// </summary>
public static class DataGridBehavior
{
    /// <summary>
    /// List of the ViewModel that holds the rows selected in the grid. Works both ways: a selection made in the grid
    /// is copied into the list, and rows the ViewModel puts into the list get selected in the grid.
    /// </summary>
    public static readonly AttachedProperty<IList?> SelectedItemsProperty =
        AvaloniaProperty.RegisterAttached<DataGrid, IList?>("SelectedItems", typeof(DataGridBehavior));

    /// <summary> Runs on double click on a row, with the item of that row as the parameter. </summary>
    public static readonly AttachedProperty<ICommand?> RowDoubleTapCommandProperty =
        AvaloniaProperty.RegisterAttached<DataGrid, ICommand?>("RowDoubleTapCommand", typeof(DataGridBehavior));

    /// <summary> Runs on the Delete key, without a parameter: the ViewModel works with its selected rows. </summary>
    public static readonly AttachedProperty<ICommand?> DeleteCommandProperty =
        AvaloniaProperty.RegisterAttached<DataGrid, ICommand?>("DeleteCommand", typeof(DataGridBehavior));

    /// <summary> Marks a grid whose events are already subscribed, so that several properties subscribe once. </summary>
    private static readonly AttachedProperty<bool> IsHookedProperty =
        AvaloniaProperty.RegisterAttached<DataGrid, bool>("IsHooked", typeof(DataGridBehavior));

    /// <summary> Link of a grid with the selection list of its ViewModel. </summary>
    private static readonly AttachedProperty<SelectionLink?> LinkProperty =
        AvaloniaProperty.RegisterAttached<DataGrid, SelectionLink?>("Link", typeof(DataGridBehavior));

    /// <summary>
    /// Keeps a grid and the selection list of its ViewModel in step. While one side is being copied into the other,
    /// IsSyncing is set, so the change it causes on the other side is not copied back.
    /// </summary>
    private sealed class SelectionLink
    {
        /// <summary> The list being watched for changes made by the ViewModel. </summary>
        public INotifyCollectionChanged? Source;

        /// <summary> Reacts to changes of the list; made once per grid. </summary>
        public NotifyCollectionChangedEventHandler? Handler;

        public bool IsSyncing;
    }

    static DataGridBehavior()
    {
        SelectedItemsProperty.Changed.AddClassHandler<DataGrid>((grid, e) =>
        {
            Hook(grid);
            Link(grid, e.NewValue as IList);
        });
        RowDoubleTapCommandProperty.Changed.AddClassHandler<DataGrid>((grid, _) => Hook(grid));
        DeleteCommandProperty.Changed.AddClassHandler<DataGrid>((grid, _) => Hook(grid));
    }

    public static IList? GetSelectedItems(DataGrid grid) => grid.GetValue(SelectedItemsProperty);
    public static void SetSelectedItems(DataGrid grid, IList? value) => grid.SetValue(SelectedItemsProperty, value);

    public static ICommand? GetRowDoubleTapCommand(DataGrid grid) => grid.GetValue(RowDoubleTapCommandProperty);
    public static void SetRowDoubleTapCommand(DataGrid grid, ICommand? value) => grid.SetValue(RowDoubleTapCommandProperty, value);

    public static ICommand? GetDeleteCommand(DataGrid grid) => grid.GetValue(DeleteCommandProperty);
    public static void SetDeleteCommand(DataGrid grid, ICommand? value) => grid.SetValue(DeleteCommandProperty, value);

    /// <summary> Subscribes to the grid events once. Keys and pointer presses are caught while tunneling, before the grid handles them itself. </summary>
    private static void Hook(DataGrid grid)
    {
        if (grid.GetValue(IsHookedProperty))
            return;
        grid.SetValue(IsHookedProperty, true);

        grid.SelectionChanged += (_, _) => SyncSelection(grid);
        grid.DoubleTapped += OnDoubleTapped;
        grid.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        grid.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        grid.AddHandler(Control.ContextRequestedEvent, OnContextRequested, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Starts watching the new ViewModel list instead of the previous one and selects in the grid
    /// whatever the list already holds.
    /// </summary>
    private static void Link(DataGrid grid, IList? list)
    {
        var link = grid.GetValue(LinkProperty);
        if (link == null)
        {
            link = new SelectionLink();
            link.Handler = (_, _) => ApplyToGrid(grid);
            grid.SetValue(LinkProperty, link);
        }

        if (link.Source != null)
            link.Source.CollectionChanged -= link.Handler;
        link.Source = list as INotifyCollectionChanged;
        if (link.Source != null)
            link.Source.CollectionChanged += link.Handler;

        ApplyToGrid(grid);
    }

    /// <summary> Copies the current selection of the grid into the ViewModel list. </summary>
    private static void SyncSelection(DataGrid grid)
    {
        var link = grid.GetValue(LinkProperty);
        if (GetSelectedItems(grid) is not { } target || link == null || link.IsSyncing)
            return;

        link.IsSyncing = true;
        try
        {
            target.Clear();
            foreach (var item in grid.SelectedItems)
                target.Add(item);
        }
        finally
        {
            link.IsSyncing = false;
        }
    }

    /// <summary>
    /// Selects in the grid the rows the ViewModel list holds. Items that are not rows of the grid are skipped;
    /// a single-selection grid takes only the first one.
    /// </summary>
    private static void ApplyToGrid(DataGrid grid)
    {
        var link = grid.GetValue(LinkProperty);
        if (GetSelectedItems(grid) is not { } source || link == null || link.IsSyncing)
            return;

        var rows = grid.ItemsSource?.Cast<object>().ToHashSet() ?? new();
        var items = source.Cast<object>().Where(rows.Contains).ToList();

        link.IsSyncing = true;
        try
        {
            if (grid.SelectionMode == DataGridSelectionMode.Extended)
            {
                grid.SelectedItems.Clear();
                foreach (var item in items)
                    grid.SelectedItems.Add(item);
            }
            else
            {
                grid.SelectedItem = items.FirstOrDefault();
            }
        }
        finally
        {
            link.IsSyncing = false;
        }
    }

    /// <summary> Returns the row under the event source, or null for the header, the empty area or a button inside a row. </summary>
    private static DataGridRow? RowUnder(object? source, bool skipButtons)
    {
        if (source is not Visual visual)
            return null;
        if (skipButtons && visual.FindAncestorOfType<Button>(includeSelf: true) != null)
            return null;
        return visual.FindAncestorOfType<DataGridRow>(includeSelf: true);
    }

    /// <summary> Clears the selection in any selection mode: the SelectedItems collection may be changed only in the extended mode. </summary>
    private static void ClearSelection(DataGrid grid)
    {
        if (grid.SelectionMode == DataGridSelectionMode.Extended)
            grid.SelectedItems.Clear();
        else
            grid.SelectedItem = null;
    }

    private static void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not DataGrid grid || RowUnder(e.Source, skipButtons: true) is not { DataContext: { } item })
            return;

        var command = GetRowDoubleTapCommand(grid);
        if (command?.CanExecute(item) == true)
            command.Execute(item);
    }

    private static void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not DataGrid grid || e.KeyModifiers != KeyModifiers.None)
            return;

        switch (e.Key)
        {
            case Key.Delete when GetDeleteCommand(grid) is { } command:
                if (command.CanExecute(null))
                    command.Execute(null);
                e.Handled = true;
                break;

            case Key.Escape when grid.SelectedItems.Count > 0:
                ClearSelection(grid);
                e.Handled = true;
                break;
        }
    }

    /// <summary>
    /// Right click on a row outside the selection selects only that row; on a selected row the selection stays,
    /// so the context menu works with all of it. Right click outside the rows clears the selection.
    /// </summary>
    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not DataGrid grid || !e.GetCurrentPoint(grid).Properties.IsRightButtonPressed)
            return;

        var row = RowUnder(e.Source, skipButtons: false);
        if (row?.DataContext is not { } item)
        {
            ClearSelection(grid);
            return;
        }

        if (!grid.SelectedItems.Contains(item))
        {
            ClearSelection(grid);
            grid.SelectedItem = item;
        }
    }

    /// <summary> The context menu opens only over a row, never over the header or the empty area. </summary>
    private static void OnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (RowUnder(e.Source, skipButtons: false) == null)
            e.Handled = true;
    }
}
