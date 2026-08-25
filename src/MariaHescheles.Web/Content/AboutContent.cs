namespace MariaHescheles.Web.Content;

/// <summary>
/// A dated entry in the practice timeline: a role, an award, a publication.
/// </summary>
public sealed record TimelineEntry
{
    /// <summary>Free-form period, e.g. "2019 — present". Kept as text so ranges read naturally.</summary>
    public required string Period { get; init; }

    public required string Title { get; init; }

    /// <summary>Studio, publication, or institution.</summary>
    public string? Organisation { get; init; }

    public string? Detail { get; init; }
}

/// <summary>
/// The About page, authored in <c>wwwroot/data/about.json</c>.
/// </summary>
public sealed record AboutContent
{
    /// <summary>Opening statement, set at display size.</summary>
    public required string Headline { get; init; }

    /// <summary>Biography. One entry per paragraph.</summary>
    public IReadOnlyList<string> Biography { get; init; } = [];

    public MediaAsset? Portrait { get; init; }

    /// <summary>The convictions behind the work. Three or four, no more.</summary>
    public IReadOnlyList<Principle> Principles { get; init; } = [];

    /// <summary>Career, education, recognition.</summary>
    public IReadOnlyList<TimelineEntry> Timeline { get; init; } = [];

    public PageMetadata? Metadata { get; init; }
}

/// <summary>A named design conviction shown on the About page.</summary>
public sealed record Principle
{
    public required string Title { get; init; }

    public required string Description { get; init; }
}
