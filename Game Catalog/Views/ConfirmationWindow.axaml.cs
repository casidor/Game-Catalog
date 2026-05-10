using Avalonia.Controls;
using Avalonia.Input;
using Game_Catalog.ViewModels;
using System;
using System.Threading.Tasks;

namespace Game_Catalog.Views;

/// <summary>
/// Universal confirmation dialog. Use <see cref="ShowAsync"/> to display it.
/// </summary>
public partial class ConfirmationWindow : Window
{
    public ConfirmationWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is ConfirmationViewModel vm)
            vm.CloseRequested += Close;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (DataContext is not ConfirmationViewModel vm) return;
        if (e.Key == Key.Escape) { vm.CancelCommand.Execute(null); e.Handled = true; }
        if (e.Key == Key.Enter) { vm.ConfirmCommand.Execute(null); e.Handled = true; }
        base.OnKeyDown(e);
    }

    /// <summary>
    /// Shows a confirmation dialog and returns true if the user confirmed.
    /// </summary>
    public static async Task<bool> ShowAsync(
        Window parent,
        string title,
        string message,
        string confirmText = "Так",
        string cancelText = "Скасувати")
    {
        var vm = new ConfirmationViewModel(title, message, confirmText, cancelText);
        var window = new ConfirmationWindow { DataContext = vm };
        await window.ShowDialog(parent);
        return vm.Confirmed;
    }
}