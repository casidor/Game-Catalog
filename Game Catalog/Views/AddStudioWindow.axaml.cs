using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Game_Catalog.ViewModels;
using System;

namespace Game_Catalog.Views;

public partial class AddStudioWindow : Window
{
    public AddStudioWindow()
    {
        InitializeComponent();
    }
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is AddStudioViewModel vm)
            vm.CloseRequested += Close;
    }
}