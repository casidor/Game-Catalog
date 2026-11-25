using Avalonia.Controls;
using Game_Catalog.ViewModels;
using System;

namespace Game_Catalog.Views;

public partial class GameDlcTabView : UserControl
{
    /// <summary> Context menu of the grid, kept to put it back when the DLC may be changed. </summary>
    private readonly ContextMenu? _menu;

    public GameDlcTabView()
    {
        InitializeComponent();
        _menu = DlcGrid.ContextMenu;
    }

    /// <summary> DLC of an archived game may only be viewed, so their grid has no context menu. </summary>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        DlcGrid.ContextMenu = DataContext is GameDlcTabViewModel { IsReadOnly: true } ? null : _menu;
    }
}
