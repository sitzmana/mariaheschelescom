namespace MariaHescheles.Web.Content;

/// <summary>
/// A body of work outside interior design — ceramics, furniture, textiles — authored in
/// <c>wwwroot/data/collections.json</c>.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately a separate type from <see cref="Project"/> rather than a flag on it. An
/// interiors project is a narrative: a brief, a problem, a sequence of rooms. A collection is
/// a set of objects, where the individual piece is the unit of interest and the through-line
/// is material rather than story. Forcing both through one model would compromise both.
/// </para>
/// <para>
/// What they do share is the primitives — <see cref="MediaAsset"/>, <see cref="Scene3D"/>,
/// the gallery and the lightbox — so a new discipline costs a JSON file, not new components.
/// </para>
/// </remarks>
public sealed record Collection
{
    /// <summary>URL segment: <c>"vessels"</c> resolves to <c>/studio/vessels</c>.</summary>
    public required string Slug { get; init; }

    public required string Title { get; init; }

    public string? Subtitle { get; init; }

    /// <summary>
    /// The craft, e.g. "Ceramics". Drives the filter on the studio index, so a new
    /// discipline appears without any code change.
    /// </summary>
    public required string Discipline { get; init; }

    /// <summary>One or two sentences, used on the index and as the social description.</summary>
    public required string Summary { get; init; }

    /// <summary>Longer statement about the work. One entry per paragraph.</summary>
    public IReadOnlyList<string> Statement { get; init; } = [];

    /// <summary>Year the collection was made, or the year it began for ongoing work.</summary>
    public int Year { get; init; }

    /// <summary>Manual ordering override; lower sorts first, then newest, then alphabetical.</summary>
    public int DisplayOrder { get; init; }

    /// <summary>Promotes the collection onto the home page.</summary>
    public bool Featured { get; init; }

    /// <summary>Accent colour sampled from the work, scoped to this collection's pages.</summary>
    public string? Accent { get; init; }

    public required MediaAsset Cover { get; init; }

    /// <summary>The individual objects.</summary>
    public IReadOnlyList<Piece> Pieces { get; init; } = [];

    public PageMetadata? Metadata { get; init; }
}

/// <summary>
/// A single object within a <see cref="Collection"/>.
/// </summary>
public sealed record Piece
{
    public required string Title { get; init; }

    public required MediaAsset Image { get; init; }

    public int Year { get; init; }

    /// <summary>Body, glaze and firing, e.g. "Stoneware, ash glaze, reduction fired".</summary>
    public string? Materials { get; init; }

    /// <summary>Conventional order for objects: height &#215; width &#215; depth.</summary>
    public string? Dimensions { get; init; }

    /// <summary>Optional note about the piece.</summary>
    public string? Description { get; init; }

    /// <summary>
    /// Optional 3D model. Objects are exactly where this earns its keep: a photograph fixes
    /// one viewpoint, and the whole point of a thrown vessel is its profile in the round.
    /// </summary>
    /// <remarks>
    /// The viewer loads nothing until the visitor activates it, so a page of pieces with
    /// models costs no more than a page of photographs until someone chooses otherwise.
    /// </remarks>
    public Scene3D? Scene { get; init; }
}
