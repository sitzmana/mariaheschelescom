using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using MariaHescheles.Web.Content;

namespace MariaHescheles.Web.Services;

/// <summary>
/// Loads content from static JSON files served alongside the application.
/// </summary>
/// <remarks>
/// <para>
/// Paths are deliberately <em>relative</em> (<c>data/site.json</c>, not <c>/data/site.json</c>).
/// <see cref="HttpClient.BaseAddress"/> is set to the host base address, so the same build
/// works whether the site is served from <c>https://mariahescheles.com/</c> or from a GitHub
/// Pages project subpath such as <c>https://user.github.io/mariaheschelescom/</c>.
/// </para>
/// </remarks>
internal sealed class ContentService : IContentService
{
    private const string SitePath = "data/site.json";
    private const string ProjectsPath = "data/projects.json";
    private const string CollectionsPath = "data/collections.json";
    private const string AboutPath = "data/about.json";

    private readonly AsyncCache<SiteContent> _site;
    private readonly AsyncCache<IReadOnlyList<Project>> _projects;
    private readonly AsyncCache<IReadOnlyList<StudioCollection>> _collections;
    private readonly AsyncCache<AboutContent> _about;

    public ContentService(HttpClient http)
    {
        ArgumentNullException.ThrowIfNull(http);

        _site = new AsyncCache<SiteContent>(() => LoadAsync(http, SitePath, ContentJsonContext.Default.SiteContent));
        _about = new AsyncCache<AboutContent>(() => LoadAsync(http, AboutPath, ContentJsonContext.Default.AboutContent));

        _projects = new AsyncCache<IReadOnlyList<Project>>(async () =>
        {
            var projects = await LoadAsync(http, ProjectsPath, ContentJsonContext.Default.IReadOnlyListProject)
                .ConfigureAwait(false);

            return [.. projects.OrderBy(static p => p.DisplayOrder)
                               .ThenByDescending(static p => p.Year)
                               .ThenBy(static p => p.Title, StringComparer.Ordinal)];
        });

        _collections = new AsyncCache<IReadOnlyList<StudioCollection>>(async () =>
        {
            var collections = await LoadAsync(http, CollectionsPath, ContentJsonContext.Default.IReadOnlyListStudioCollection)
                .ConfigureAwait(false);

            return [.. collections.OrderBy(static c => c.DisplayOrder)
                                  .ThenByDescending(static c => c.Year)
                                  .ThenBy(static c => c.Title, StringComparer.Ordinal)];
        });
    }

    public Task<SiteContent> GetSiteAsync(CancellationToken cancellationToken = default)
        => _site.GetAsync(cancellationToken);

    public Task<IReadOnlyList<Project>> GetProjectsAsync(CancellationToken cancellationToken = default)
        => _projects.GetAsync(cancellationToken);

    public Task<AboutContent> GetAboutAsync(CancellationToken cancellationToken = default)
        => _about.GetAsync(cancellationToken);

    public async Task<IReadOnlyList<Project>> GetFeaturedProjectsAsync(CancellationToken cancellationToken = default)
    {
        var projects = await GetProjectsAsync(cancellationToken).ConfigureAwait(false);
        return [.. projects.Where(static p => p.Featured)];
    }

    public async Task<Project?> GetProjectAsync(string slug, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        var projects = await GetProjectsAsync(cancellationToken).ConfigureAwait(false);
        return projects.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<ProjectNeighbours> GetNeighboursAsync(string slug, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        var projects = await GetProjectsAsync(cancellationToken).ConfigureAwait(false);
        var index = -1;

        for (var i = 0; i < projects.Count; i++)
        {
            if (string.Equals(projects[i].Slug, slug, StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            return default;
        }

        return new ProjectNeighbours(
            Previous: index > 0 ? projects[index - 1] : null,
            Next: index < projects.Count - 1 ? projects[index + 1] : null);
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var projects = await GetProjectsAsync(cancellationToken).ConfigureAwait(false);

        // Distinct() preserves first-seen order, which is already display order.
        return [.. projects.Select(static p => p.Category).Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    public Task<IReadOnlyList<StudioCollection>> GetCollectionsAsync(CancellationToken cancellationToken = default)
        => _collections.GetAsync(cancellationToken);

    public async Task<IReadOnlyList<StudioCollection>> GetFeaturedCollectionsAsync(CancellationToken cancellationToken = default)
    {
        var collections = await GetCollectionsAsync(cancellationToken).ConfigureAwait(false);
        return [.. collections.Where(static c => c.Featured)];
    }

    public async Task<StudioCollection?> GetCollectionAsync(string slug, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        var collections = await GetCollectionsAsync(cancellationToken).ConfigureAwait(false);
        return collections.FirstOrDefault(c => string.Equals(c.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<string>> GetDisciplinesAsync(CancellationToken cancellationToken = default)
    {
        var collections = await GetCollectionsAsync(cancellationToken).ConfigureAwait(false);
        return [.. collections.Select(static c => c.Discipline).Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    private static async Task<T> LoadAsync<T>(HttpClient http, string path, JsonTypeInfo<T> typeInfo)
    {
        var value = await http.GetFromJsonAsync(path, typeInfo).ConfigureAwait(false);

        return value ?? throw new InvalidOperationException(
            $"'{path}' deserialised to null. The file must contain a JSON document, not the literal 'null'.");
    }
}
