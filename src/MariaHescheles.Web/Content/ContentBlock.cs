using System.Text.Json.Serialization;

namespace MariaHescheles.Web.Content;

/// <summary>
/// Base type for the ordered list of content sections that makes up a project case study.
/// </summary>
/// <remarks>
/// <para>
/// Blocks are the extension point of the whole content system. Adding a new kind of section
/// to the site is three steps and touches no existing logic:
/// </para>
/// <list type="number">
///   <item><description>Add a record deriving from <see cref="ContentBlock"/>.</description></item>
///   <item><description>Add a <see cref="JsonDerivedTypeAttribute"/> line below with a new discriminator.</description></item>
///   <item><description>Add a matching <c>case</c> to <c>ContentBlockRenderer.razor</c>.</description></item>
/// </list>
/// <para>
/// The discriminator is serialised as <c>"$type"</c>, so a block in <c>projects.json</c> looks
/// like <c>{ "$type": "quote", "text": "..." }</c>.
/// </para>
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ProseBlock), "prose")]
[JsonDerivedType(typeof(ImageBlock), "image")]
[JsonDerivedType(typeof(GalleryBlock), "gallery")]
[JsonDerivedType(typeof(QuoteBlock), "quote")]
[JsonDerivedType(typeof(SpecificationsBlock), "specs")]
[JsonDerivedType(typeof(ComparisonBlock), "comparison")]
[JsonDerivedType(typeof(SceneBlock), "scene")]
public abstract record ContentBlock
{
    /// <summary>
    /// Optional anchor id. When set the block becomes linkable as <c>/work/slug#anchor</c>.
    /// </summary>
    public string? Anchor { get; init; }
}

/// <summary>Headed body copy. The workhorse block.</summary>
public sealed record ProseBlock : ContentBlock
{
    /// <summary>Small label above the heading, e.g. "The brief".</summary>
    public string? Eyebrow { get; init; }

    /// <summary>Section heading. Rendered as an <c>&lt;h2&gt;</c>.</summary>
    public string? Heading { get; init; }

    /// <summary>One entry per paragraph. Plain text; no markup is interpreted.</summary>
    public IReadOnlyList<string> Paragraphs { get; init; } = [];
}

/// <summary>How much horizontal room a media block occupies.</summary>
public enum MediaWidth
{
    /// <summary>Constrained to the reading column. Good for detail shots.</summary>
    Inset,

    /// <summary>Wider than the reading column but still inside the page gutters.</summary>
    Wide,

    /// <summary>Edge to edge. Reserve for the strongest photographs.</summary>
    Full,
}

/// <summary>A single photograph.</summary>
public sealed record ImageBlock : ContentBlock
{
    public required MediaAsset Image { get; init; }

    public MediaWidth Width { get; init; } = MediaWidth.Wide;

    /// <summary>
    /// Depth of the parallax drift, as a fraction of the element height.
    /// <c>0</c> disables it. <c>0.08</c> is a good default: perceptible, never distracting.
    /// </summary>
    public double Parallax { get; init; }
}

/// <summary>How a <see cref="GalleryBlock"/> arranges its images.</summary>
public enum GalleryLayout
{
    /// <summary>Two equal columns. Best for paired detail shots.</summary>
    Pair,

    /// <summary>Three equal columns.</summary>
    Triptych,

    /// <summary>Masonry-ish responsive grid that respects each image's own aspect ratio.</summary>
    Mosaic,

    /// <summary>Horizontally scrollable filmstrip with scroll snapping.</summary>
    Filmstrip,
}

/// <summary>A set of photographs shown together and openable in the lightbox.</summary>
public sealed record GalleryBlock : ContentBlock
{
    public IReadOnlyList<MediaAsset> Images { get; init; } = [];

    public GalleryLayout Layout { get; init; } = GalleryLayout.Mosaic;

    public MediaWidth Width { get; init; } = MediaWidth.Wide;
}

/// <summary>A pull quote, typically from the client.</summary>
public sealed record QuoteBlock : ContentBlock
{
    public required string Text { get; init; }

    public string? Attribution { get; init; }
}

/// <summary>A label/value pair in a <see cref="SpecificationsBlock"/>.</summary>
public sealed record Specification
{
    public required string Label { get; init; }

    public required string Value { get; init; }
}

/// <summary>Project facts: square footage, completion date, trades, materials.</summary>
public sealed record SpecificationsBlock : ContentBlock
{
    public string? Heading { get; init; }

    public IReadOnlyList<Specification> Items { get; init; } = [];
}

/// <summary>A draggable before/after slider. Both images must share an aspect ratio.</summary>
public sealed record ComparisonBlock : ContentBlock
{
    public required MediaAsset Before { get; init; }

    public required MediaAsset After { get; init; }

    public string BeforeLabel { get; init; } = "Before";

    public string AfterLabel { get; init; } = "After";

    public MediaWidth Width { get; init; } = MediaWidth.Wide;
}

/// <summary>An interactive 3D model embedded in the case study.</summary>
public sealed record SceneBlock : ContentBlock
{
    public required Scene3D Scene { get; init; }

    public string? Heading { get; init; }

    public string? Description { get; init; }

    public MediaWidth Width { get; init; } = MediaWidth.Wide;
}
