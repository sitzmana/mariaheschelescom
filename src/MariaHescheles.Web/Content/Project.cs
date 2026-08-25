namespace MariaHescheles.Web.Content;

/// <summary>
/// One case study. Authored as an entry in <c>wwwroot/data/projects.json</c>.
/// </summary>
public sealed record Project
{
    /// <summary>
    /// URL segment, e.g. <c>"greenwich-townhouse"</c> resolves to <c>/work/greenwich-townhouse</c>.
    /// Lowercase, hyphenated, and permanent: changing it breaks every link ever shared.
    /// </summary>
    public required string Slug { get; init; }

    public required string Title { get; init; }

    /// <summary>Short qualifier shown under the title, e.g. "A pre-war apartment, rethought".</summary>
    public string? Subtitle { get; init; }

    /// <summary>
    /// One or two sentences. Used on the index grid, in search results, and as the
    /// social-share description, so it must read well out of context.
    /// </summary>
    public required string Summary { get; init; }

    /// <summary>Year of completion. Drives the default sort order.</summary>
    public int Year { get; init; }

    public string? Location { get; init; }

    /// <summary>
    /// Free-form category used by the portfolio filter, e.g. "Residential".
    /// Filters are derived from the data, so a new category needs no code change.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>Services rendered, shown as tags on the case study.</summary>
    public IReadOnlyList<string> Services { get; init; } = [];

    /// <summary>Promotes the project onto the home page.</summary>
    public bool Featured { get; init; }

    /// <summary>
    /// Manual ordering override. Lower sorts first; projects sharing a value fall back to
    /// newest-first. Leave at <c>0</c> to use the default ordering.
    /// </summary>
    public int DisplayOrder { get; init; }

    /// <summary>Hero image for the grid card and the top of the case study.</summary>
    public required MediaAsset Cover { get; init; }

    /// <summary>
    /// Accent colour sampled from the project's palette. Applied as a CSS custom property
    /// scoped to the case study, so each project subtly re-tints the interface.
    /// </summary>
    public string? Accent { get; init; }

    /// <summary>Ordered body of the case study. See <see cref="ContentBlock"/>.</summary>
    public IReadOnlyList<ContentBlock> Blocks { get; init; } = [];

    /// <summary>Optional per-project overrides for the page title and social preview image.</summary>
    public PageMetadata? Metadata { get; init; }
}
