using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System.Collections;
using System.Windows.Input;

namespace Game_Catalog.Views;

/// <summary>
/// Shared table behavior, set up from XAML through attached properties:
/// mirrors the selected rows into the ViewModel, opens a row on double click, deletes the selection with Delete,
/// clears the selection with Escape and selects rows on right click the way a file manager does.
/// </summary>
public static class DataGridBehavior
{
    /// <summary> List of the ViewModel that always holds the rows selected in the grid (grid to ViewModel only). </summary>
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

    static DataGridBehavior()
    {
        SelectedItemsProperty.Changed.AddClassHandler<DataGrid>((grid, _) => { Hook(grid); SyncSelection(grid); });
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

    /// <summary> Copies the current selection of the grid into the ViewModel list. </summary>
    private static void SyncSelection(DataGrid grid)
    {
        if (GetSelectedItems(grid) is not { } target)
            return;

        target.Clear();
        foreach (var item in grid.SelectedItems)
            target.Add(item);
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
