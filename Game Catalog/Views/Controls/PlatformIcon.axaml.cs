using Avalonia;
using Avalonia.Controls;

namespace Game_Catalog.Views;

/// <summary>
/// Icon of a platform from the media folder, or a circle with a question mark when there is none.
/// The size is set by Width and Height; 16 by default.
/// </summary>
public partial class PlatformIcon : UserControl
{
    public static readonly StyledProperty<string?> IconPathProperty =
        AvaloniaProperty.Register<PlatformIcon, string?>(nameof(IconPath));

    /// <summary> Stored icon path of the platform. Empty or missing file shows the placeholder. </summary>
    public string? IconPath
    {
        get => GetValue(IconPathProperty);
        set => SetValue(IconPathProperty, value);
    }

    public PlatformIcon()
    {
        InitializeComponent();
    }
}