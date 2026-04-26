using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Game_Catalog.ViewModels;
using System;

namespace Game_Catalog.Views;

public partial class AddGameWindow : Window
{
    public AddGameWindow()
    {
        InitializeComponent();
    }
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is AddGameViewModel vm)
            vm.CloseRequested += Close;
    }
}