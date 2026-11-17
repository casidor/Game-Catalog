using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Game_Catalog.Views;

/// <summary>
/// Lets a DataGrid be sorted by its ViewModel (and so by the database) instead of sorting its own copy of the rows.
/// A sortable column carries its sort field in Tag: a click on its header reports that field,
/// and a thin arrow in the header shows the current field and direction.
/// The grid needs the "dbSort" class, which hides the built-in sort glyph (see App.axaml).
/// </summary>
public sealed class DataGridSortHeaders
{
    private readonly DataGrid _grid;
    private readonly Func<(object? Field, bool Descending)> _getSort;
    private readonly Action<object> _sortBy;

    /// <summary> Arrow labels of the sortable columns, shown on the right side of the header. </summary>
    private readonly Dictionary<DataGridColumn, TextBlock> _arrows = new();

    private INotifyPropertyChanged? _source;

    /// <param name="grid"> The table whose header clicks are taken over. </param>
    /// <param name="getSort"> Returns the current sort field and direction of the ViewModel. </param>
    /// <param name="sortBy"> Called with the Tag of the clicked column. </param>
    public DataGridSortHeaders(DataGrid grid, Func<(object? Field, bool Descending)> getSort, Action<object> sortBy)
    {
        _grid = grid;
        _getSort = getSort;
        _sortBy = sortBy;
        _grid.Sorting += OnSorting;
    }

    /// <summary> Starts following the ViewModel: arrows are refreshed when one of its Sort* properties changes. </summary>
    public void Attach(object? viewModel)
    {
        Detach();
        _source = viewModel as INotifyPropertyChanged;
        if (_source != null)
            _source.PropertyChanged += OnSourcePropertyChanged;
        Update();
    }

    /// <summary> Stops following the ViewModel. </summary>
    public void Detach()
    {
        if (_source != null)
            _source.PropertyChanged -= OnSourcePropertyChanged;
        _source = null;
    }

    private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName?.StartsWith("Sort", StringComparison.Ordinal) == true)
            Update();
    }

    /// <summary> Sends the clicked column to the ViewModel instead of letting the grid sort its own copy. </summary>
    private void OnSorting(object? sender, DataGridColumnEventArgs e)
    {
        e.Handled = true;
        if (e.Column.Tag is { } field)
            _sortBy(field);
    }

    /// <summary> Replaces the text headers of the sortable columns with a title plus a right-aligned arrow. Runs once. </summary>
    private void BuildHeaders()
    {
        if (_arrows.Count > 0) return;

        foreach (var column in _grid.Columns)
        {
            if (column.Tag == null) continue;

            var title = new TextBlock
            {
                Text = column.Header?.ToString() ?? string.Empty,
                VerticalAlignment = VerticalAlignment.Center
            };
            var arrow = new TextBlock
            {
                FontSize = 16,
                FontWeight = FontWeight.Light,
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
    private void Update()
    {
        BuildHeaders();
        var (field, descending) = _getSort();

        foreach (var (column, arrow) in _arrows)
            arrow.Text = Equals(column.Tag, field) ? (descending ? "↓" : "↑") : string.Empty;
    }
}
