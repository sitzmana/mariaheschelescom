namespace MariaHescheles.Web.Components.Motion;

/// <summary>
/// The entrance animations available to <c>Reveal</c>.
/// </summary>
/// <remarks>
/// Each value maps to a <c>.reveal--{name}</c> rule in <c>wwwroot/css/motion.css</c>.
/// Adding a variant means adding an enum member and a CSS rule; no component changes.
/// </remarks>
public enum RevealVariant
{
    /// <summary>Fade upward from a short offset. The default, and the right answer most of the time.</summary>
    Rise,

    /// <summary>Opacity only. For elements where movement would fight the layout.</summary>
    Fade,

    /// <summary>Fade in from a barely-reduced scale. Reserve for hero imagery.</summary>
    Scale,

    /// <summary>Wipe upward behind a mask. Suits large display headings.</summary>
    Mask,

    /// <summary>Fade in from the inline-start edge. Good for list items and captions.</summary>
    Slide,
}
