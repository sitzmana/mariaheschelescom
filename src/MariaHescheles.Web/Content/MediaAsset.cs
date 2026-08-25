namespace MariaHescheles.Web.Content;

/// <summary>
/// A single image or video asset together with everything needed to render it without
/// layout shift.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Width"/> and <see cref="Height"/> are the <em>intrinsic</em> pixel dimensions of
/// the source file. They are not used to size the element on screen; they are used to reserve
/// the correct aspect ratio before the bytes arrive, which is what keeps Cumulative Layout
/// Shift at zero.
/// </para>
/// </remarks>
public sealed record MediaAsset
{
    /// <summary>Root-relative path to the default (largest) source, e.g. <c>/media/projects/x/hero.jpg</c>.</summary>
    public required string Src { get; init; }

    /// <summary>
    /// Alternative text. Write what the photograph shows, not "photo of project".
    /// Use an empty string only when the image is purely decorative and adjacent text
    /// already conveys the same meaning.
    /// </summary>
    public required string Alt { get; init; }

    /// <summary>Intrinsic width of <see cref="Src"/> in pixels.</summary>
    public int Width { get; init; }

    /// <summary>Intrinsic height of <see cref="Src"/> in pixels.</summary>
    public int Height { get; init; }

    /// <summary>
    /// Optional <c>srcset</c> descriptor list, e.g.
    /// <c>"/media/x/hero-800.jpg 800w, /media/x/hero-1600.jpg 1600w"</c>.
    /// When present the browser downloads the smallest file that still looks sharp.
    /// </summary>
    public string? SrcSet { get; init; }

    /// <summary>
    /// Optional <c>sizes</c> hint describing how wide the image renders at each breakpoint.
    /// Ignored unless <see cref="SrcSet"/> is also set. Defaults to <c>100vw</c>.
    /// </summary>
    public string? Sizes { get; init; }

    /// <summary>
    /// A single averaged colour (any CSS colour) painted behind the image while it loads.
    /// Sampling the dominant colour of the photograph makes loading feel intentional
    /// rather than broken.
    /// </summary>
    public string? Placeholder { get; init; }

    /// <summary>Optional visible caption rendered beneath the image.</summary>
    public string? Caption { get; init; }

    /// <summary>Optional photographer or styling credit rendered with the caption.</summary>
    public string? Credit { get; init; }

    /// <summary>
    /// Aspect ratio used to reserve space. Falls back to 3:2 when the authoring data omits
    /// intrinsic dimensions, which is the most common photographic ratio.
    /// </summary>
    public double AspectRatio => Width > 0 && Height > 0 ? (double)Width / Height : 3d / 2d;
}
