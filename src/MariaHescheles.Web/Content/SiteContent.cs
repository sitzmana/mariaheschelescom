namespace MariaHescheles.Web.Content;

/// <summary>
/// Per-page search-engine and social-preview data.
/// </summary>
/// <remarks>
/// A standalone WebAssembly app ships one HTML file, so crawlers that do not execute
/// JavaScript see only <c>index.html</c>. Every field here is therefore duplicated:
/// once in <c>index.html</c> as a sensible site-wide default, and once at runtime through
/// <c>HeadOutlet</c> for crawlers and link unfurlers that do run JavaScript
/// (Google, Bing, Slack, Discord, LinkedIn all do).
/// </remarks>
public sealed record PageMetadata
{
    /// <summary>Page title without the site suffix. Aim for under 60 characters.</summary>
    public string? Title { get; init; }

    /// <summary>Meta description. Aim for 120-160 characters.</summary>
    public string? Description { get; init; }

    /// <summary>Root-relative path to the social preview image. Ideally 1200x630.</summary>
    public string? ImageUrl { get; init; }

    /// <summary>Alt text for <see cref="ImageUrl"/>.</summary>
    public string? ImageAlt { get; init; }
}

/// <summary>A labelled external link, used for social profiles and press mentions.</summary>
public sealed record ExternalLink
{
    public required string Label { get; init; }

    public required string Url { get; init; }
}

/// <summary>A primary navigation entry.</summary>
public sealed record NavigationLink
{
    public required string Label { get; init; }

    /// <summary>Root-relative path, e.g. <c>/work</c>.</summary>
    public required string Href { get; init; }
}

/// <summary>
/// Site-wide identity and configuration, authored in <c>wwwroot/data/site.json</c>.
/// </summary>
public sealed record SiteContent
{
    public required string Name { get; init; }

    /// <summary>Professional title, e.g. "Interior Designer".</summary>
    public required string Role { get; init; }

    /// <summary>The one line that opens the site. Short, specific, memorable.</summary>
    public required string Tagline { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    /// <summary>Where she practises, e.g. "New York &amp; the Hudson Valley".</summary>
    public string? Location { get; init; }

    /// <summary>
    /// Absolute origin of the production site, e.g. <c>https://mariahescheles.com</c>.
    /// Required to emit absolute URLs in canonical tags, structured data and the sitemap.
    /// </summary>
    public required string CanonicalOrigin { get; init; }

    public IReadOnlyList<NavigationLink> Navigation { get; init; } = [];

    public IReadOnlyList<ExternalLink> Social { get; init; } = [];

    /// <summary>Site-wide fallback metadata, overridden per page where it matters.</summary>
    public PageMetadata? Metadata { get; init; }
}
