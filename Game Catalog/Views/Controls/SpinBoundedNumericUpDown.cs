using Avalonia;
using Avalonia.Controls;
using System;

namespace Game_Catalog.Views;

/// <summary>
/// A NumericUpDown whose spin buttons, arrow keys and mouse wheel stay within
/// <see cref="SpinMinimum"/> and <see cref="SpinMaximum"/>, while a typed value reaches
/// the binding unchanged, so the view model can report it instead of it being clipped silently.
/// </summary>
public class SpinBoundedNumericUpDown : NumericUpDown
{
    /// <summary> Lowest value the spin can reach. Null means no lower bound. </summary>
    public static readonly StyledProperty<decimal?> SpinMinimumProperty =
        AvaloniaProperty.Register<SpinBoundedNumericUpDown, decimal?>(nameof(SpinMinimum));

    /// <summary> Highest value the spin can reach. Null means no upper bound. </summary>
    public static readonly StyledProperty<decimal?> SpinMaximumProperty =
        AvaloniaProperty.Register<SpinBoundedNumericUpDown, decimal?>(nameof(SpinMaximum));

    /// <summary> Value set by the first spin of an empty field. Null means <see cref="SpinMinimum"/>. </summary>
    public static readonly StyledProperty<decimal?> SpinStartProperty =
        AvaloniaProperty.Register<SpinBoundedNumericUpDown, decimal?>(nameof(SpinStart));

    public decimal? SpinMinimum
    {
        get => GetValue(SpinMinimumProperty);
        set => SetValue(SpinMinimumProperty, value);
    }

    public decimal? SpinMaximum
    {
        get => GetValue(SpinMaximumProperty);
        set => SetValue(SpinMaximumProperty, value);
    }

    public decimal? SpinStart
    {
        get => GetValue(SpinStartProperty);
        set => SetValue(SpinStartProperty, value);
    }

    /// <summary> Styled as a regular NumericUpDown, so the theme template applies. </summary>
    protected override Type StyleKeyOverride => typeof(NumericUpDown);

    /// <summary>
    /// Replaces the base spin: an empty field gets the start value; otherwise the value moves by
    /// the increment and stops at the bounds. A typed value outside the bounds is brought back
    /// into them by a spin towards the range, while a spin away from it does nothing.
    /// </summary>
    protected override void OnSpin(SpinEventArgs e)
    {
        if (Value is not { } value)
        {
            SetCurrentValue(ValueProperty, SpinStart ?? SpinMinimum ?? 0m);
            return;
        }

        var increase = e.Direction == SpinDirection.Increase;
        if (increase && value >= SpinMaximum) return;
        if (!increase && value <= SpinMinimum) return;

        var next = increase ? value + Increment : value - Increment;
        if (SpinMinimum is { } min && next < min) next = min;
        if (SpinMaximum is { } max && next > max) next = max;

        SetCurrentValue(ValueProperty, next);
    }
}
