using Avalonia;
using Avalonia.Controls;
using Game_Catalog.ViewModels;

namespace Game_Catalog.Views;

public partial class DictionariesView : UserControl
{
    public DictionariesView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is DictionariesViewModel vm)
            vm.Load();
    }
}