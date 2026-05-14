using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Game_Catalog.ViewModels;

namespace Game_Catalog.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is SettingsViewModel vm)
        {
            vm.Reload();
        }
    }
}