using Avalonia.Controls;
using Avalonia.Input;
using Game_Catalog.ViewModels;
using System;
using System.Threading.Tasks;

namespace Game_Catalog.Views;

/// <summary>
/// Universal confirmation and alert dialog.
/// Use <see cref="ShowAsync"/> for two-button confirmation
/// or <see cref="ShowAlertAsync"/> for a single-button alert.
/// </summary>
public partial class ConfirmationWindow : Window
{
    /// <summary>Initializes the window and loads XAML.</summary>
    public ConfirmationWindow()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is ConfirmationViewModel vm)
            vm.CloseRequested += Close;
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (DataContext is not ConfirmationViewModel vm) return;
        if (e.Key == Key.Escape && !vm.IsAlert) { vm.CancelCommand.Execute(null); e.Handled = true; }
        if (e.Key == Key.Enter) { vm.ConfirmCommand.Execute(null); e.Handled = true; }
        base.OnKeyDown(e);
    }

    /// <summary>
    /// Shows a two-button confirmation dialog and returns true if the user confirmed.
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

    /// <summary>
    /// Shows a single-button alert dialog with an OK button.
    /// </summary>
    public static async Task ShowAlertAsync(Window parent, string title, string message)
    {
        var vm = new ConfirmationViewModel(title, message, isAlert: true);
        var window = new ConfirmationWindow { DataContext = vm };
        await window.ShowDialog(parent);
    }
}