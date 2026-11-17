namespace Game_Catalog.Services;

/// <summary> Static access point to the dialog service, set once at application start. </summary>
public static class Dialogs
{
    /// <summary> Current dialog service used by ViewModels. </summary>
    public static IDialogService Current { get; set; } = null!;
}