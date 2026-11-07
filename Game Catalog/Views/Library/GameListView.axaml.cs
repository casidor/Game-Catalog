using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Game_Catalog.Models;
using Game_Catalog.ViewModels;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Layout;

namespace Game_Catalog.Views;

public partial class GamesListView : UserControl
{
    public GamesListView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is GamesListViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            vm.Load();
            UpdateSortArrows();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is GamesListViewModel vm)
            vm.PropertyChanged -= OnViewModelPropertyChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(GamesListViewModel.SortField) or nameof(GamesListViewModel.SortDescending))
            UpdateSortArrows();
    }

    /// <summary> Arrow labels of the sortable columns, shown on the right side of the header. </summary>
    private readonly Dictionary<DataGridColumn, TextBlock> _arrows = new();

    /// <summary> Sends the clicked column to the ViewModel instead of letting the grid sort its own copy. </summary>
    private void OnColumnSorting(object? sender, DataGridColumnEventArgs e)
    {
        e.Handled = true;
        if (DataContext is GamesListViewModel vm && e.Column.Tag is GameSortField field)
            vm.SortByColumn(field);
    }

    /// <summary> Replaces the text headers of the sortable columns with a title plus a right-aligned arrow. Runs once. </summary>
    private void BuildSortHeaders()
    {
        if (_arrows.Count > 0) return;

        foreach (var column in GamesGrid.Columns)
        {
            if (column.Tag is not GameSortField) continue;

            var title = new TextBlock
            {
                Text = column.Header?.ToString() ?? string.Empty,
                VerticalAlignment = VerticalAlignment.Center
            };
            var arrow = new TextBlock
            {
                FontSize = 16,
                FontWeight = Avalonia.Media.FontWeight.Light,
                Margin = new Thickness(8, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(arrow, 1);

            var header = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            header.Children.Add(title);
            header.Children.Add(arrow);

            column.Header = header;
            _arrows[column] = arrow;
        }
    }

    /// <summary> Shows a thin ↑/↓ on the column that is currently sorted and nothing on the others. </summary>
    private void UpdateSortArrows()
    {
        if (DataContext is not GamesListViewModel vm) return;
        BuildSortHeaders();

        foreach (var (column, arrow) in _arrows)
        {
            arrow.Text = column.Tag is GameSortField field && field == vm.SortField
                ? (vm.SortDescending ? "↓" : "↑")
                : string.Empty;
        }
    }

    /// <summary> Closes the filter panel without applying when the dimmed backdrop is clicked. </summary>
    private void OnOverlayPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is GamesListViewModel vm)
            vm.CancelFiltersCommand.Execute(null);
    }

    /// <summary> Opens the game whose table row was double-clicked. </summary>
    private void OnRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is not Visual source) return;
        if (source.FindAncestorOfType<DataGridRow>(includeSelf: true) is not { DataContext: GameRow row }) return;

        if (DataContext is GamesListViewModel vm)
            vm.OpenGameCommand.Execute(row.Game);
    }
}