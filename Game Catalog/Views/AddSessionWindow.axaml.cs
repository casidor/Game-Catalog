using Avalonia.Controls;
using Avalonia.Input;
using Game_Catalog.ViewModels;
using System;

namespace Game_Catalog.Views;

public partial class AddSessionWindow : Window
{
    public AddSessionWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is AddSessionViewModel vm)
            vm.CloseRequested += Close;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (DataContext is not AddSessionViewModel vm) { base.OnKeyDown(e); return; }
        if (e.Key == Key.Escape) { vm.CancelCommand.Execute(null); e.Handled = true; }
        else if (e.Key == Key.Enter && !e.Handled) { vm.ConfirmCommand.Execute(null); e.Handled = true; }
        base.OnKeyDown(e);
    }
}