using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Game_Catalog.ViewModels;
using System;

namespace Game_Catalog.Views;

public partial class LegacyAddStudioWindow : Window
{
    public LegacyAddStudioWindow()
    {
        InitializeComponent();
    }
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is LegacyAddStudioViewModel vm)
            vm.CloseRequested += Close;
    }
        protected override void OnKeyDown(KeyEventArgs e)
    {
        if (DataContext is not LegacyAddStudioViewModel vm) { base.OnKeyDown(e); return; }
        if (e.Key == Key.Escape) { vm.CancelCommand.Execute(null); e.Handled = true; }
        else if (e.Key == Key.Enter && !e.Handled) { vm.ConfirmCommand.Execute(null); e.Handled = true; }
        base.OnKeyDown(e);
    }
}