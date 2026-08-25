using MariaHescheles.Web.Content;

namespace MariaHescheles.Web.Services;

/// <summary>
/// The projects immediately before and after a given project in display order,
/// used to offer "previous / next" navigation at the foot of a case study.
/// </summary>
/// <param name="Previous">The preceding project, or <see langword="null"/> at the start of the list.</param>
/// <param name="Next">The following project, or <see langword="null"/> at the end of the list.</param>
public readonly record struct ProjectNeighbours(Project? Previous, Project? Next);

/// <summary>
/// Read access to everything authored in <c>wwwroot/data</c>.
/// </summary>
/// <remarks>
/// Components depend on this interface rather than on <see cref="HttpClient"/> so that the
/// content source is an implementation detail. Swapping the flat JSON files for a headless
/// CMS later means writing one new class and changing one line of registration.
/// </remarks>
public interface IContentService
{
    /// <summary>Loads site-wide identity, navigation and default metadata.</summary>
    Task<SiteContent> GetSiteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads every project in display order: <c>displayOrder</c> ascending, then newest first,
    /// then alphabetical. Ties are broken deterministically so the grid never reshuffles.
    /// </summary>
    Task<IReadOnlyList<Project>> GetProjectsAsync(CancellationToken cancellationToken = default);

    /// <summary>Loads the projects marked <c>featured</c>, in display order.</summary>
    Task<IReadOnlyList<Project>> GetFeaturedProjectsAsync(CancellationToken cancellationToken = default);

    /// <summary>Finds a project by slug, or <see langword="null"/> when no such project exists.</summary>
    Task<Project?> GetProjectAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Finds the projects either side of <paramref name="slug"/> in display order.</summary>
    Task<ProjectNeighbours> GetNeighboursAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the distinct project categories in display order, suitable for the portfolio
    /// filter. Derived from the project data, so adding a category needs no code change.
    /// </summary>
    Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Loads the About page content.</summary>
    Task<AboutContent> GetAboutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads every collection of non-interiors work — ceramics, furniture, textiles — in
    /// display order.
    /// </summary>
    Task<IReadOnlyList<Collection>> GetCollectionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Loads the collections marked <c>featured</c>, in display order.</summary>
    Task<IReadOnlyList<Collection>> GetFeaturedCollectionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Finds a collection by slug, or <see langword="null"/> when no such collection exists.</summary>
    Task<Collection?> GetCollectionAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the distinct disciplines across all collections, in display order, for the
    /// studio index filter.
    /// </summary>
    Task<IReadOnlyList<string>> GetDisciplinesAsync(CancellationToken cancellationToken = default);
}
