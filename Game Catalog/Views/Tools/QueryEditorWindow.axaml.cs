using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Game_Catalog.ViewModels;
using System;
using System.Collections.Generic;

namespace Game_Catalog.Views;

public partial class QueryEditorWindow : Window
{
    private QueryEditorViewModel? _vm;

    public QueryEditorWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_vm != null) _vm.ResultChanged -= RebuildGrid;
        _vm = DataContext as QueryEditorViewModel;
        if (_vm != null) _vm.ResultChanged += RebuildGrid;
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_vm != null) _vm.ResultChanged -= RebuildGrid;
        base.OnClosed(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.F12 or Key.Escape)
        {
            e.Handled = true;
            Close();
            return;
        }
        base.OnKeyDown(e);
    }

    /// <summary>Builds grid columns dynamically from the last query result.</summary>
    private void RebuildGrid()
    {
        if (_vm == null) return;

        ResultGrid.ItemsSource = null;
        ResultGrid.Columns.Clear();

        for (int i = 0; i < _vm.Columns.Count; i++)
        {
            ResultGrid.Columns.Add(new DataGridTextColumn
            {
                Header = _vm.Columns[i],
                Binding = new Binding($"[{i}]")
            });
        }

        ResultGrid.ItemsSource = (IEnumerable<string?[]>)_vm.Rows;
    }
}