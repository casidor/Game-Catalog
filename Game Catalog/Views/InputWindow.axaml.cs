using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Threading.Tasks;

namespace Game_Catalog.Views;

/// <summary>
/// Universal single-field input dialog. The handler validates or processes the text
/// and returns an error message, or null to accept and close.
/// </summary>
public partial class InputWindow : Window
{
    private readonly Func<string, Task<string?>> _handler;

    /// <summary> Text accepted by the handler, or null if the dialog was cancelled. </summary>
    public string? Result { get; private set; }

    public InputWindow()
    {
        InitializeComponent();
        _handler = _ => Task.FromResult<string?>(null);
    }

    public InputWindow(string title, string prompt, string placeholder,
                       string confirmText, Func<string, Task<string?>> handler) : this()
    {
        Title = title;
        PromptText.Text = prompt;
        InputBox.PlaceholderText = placeholder;
        ConfirmButton.Content = confirmText;
        _handler = handler;

        CancelButton.Click += (_, _) => Close();
        ConfirmButton.Click += async (_, _) => await ConfirmAsync();
    }

    private async Task ConfirmAsync()
    {
        ConfirmButton.IsEnabled = false;
        ErrorText.IsVisible = false;

        var text = InputBox.Text ?? string.Empty;
        var error = await _handler(text);

        if (error == null)
        {
            Result = text;
            Close();
            return;
        }

        ErrorText.Text = error;
        ErrorText.IsVisible = true;
        ConfirmButton.IsEnabled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { Close(); e.Handled = true; }
        else if (e.Key == Key.Enter && ConfirmButton.IsEnabled)
        {
            e.Handled = true;
            _ = ConfirmAsync();
        }
        base.OnKeyDown(e);
    }
}