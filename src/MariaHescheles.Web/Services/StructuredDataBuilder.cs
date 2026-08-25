using System.Text;
using System.Text.Json;
using MariaHescheles.Web.Content;

namespace MariaHescheles.Web.Services;

/// <summary>
/// Builds schema.org JSON-LD documents for the pages that benefit from them.
/// </summary>
/// <remarks>
/// <para>
/// Written with <see cref="Utf8JsonWriter"/> rather than by serialising an object graph.
/// JSON-LD keys such as <c>@context</c> and <c>@type</c> do not map cleanly onto C# members,
/// the shape differs per page, and hand-writing the tokens is both trim-safe and correctly
/// escaped — which string concatenation would not be.
/// </para>
/// <para>
/// This is what lets a search result show "Maria Hescheles &#183; Interior Designer" with a
/// photograph, instead of a bare URL and a truncated sentence.
/// </para>
/// </remarks>
public static class StructuredDataBuilder
{
    private const string Context = "https://schema.org";

    /// <summary>Describes the designer herself. Emitted on the home and about pages.</summary>
    public static string Person(SiteContent site)
    {
        ArgumentNullException.ThrowIfNull(site);

        return Write(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("@context", Context);
            writer.WriteString("@type", "Person");
            writer.WriteString("name", site.Name);
            writer.WriteString("jobTitle", site.Role);
            writer.WriteString("description", site.Tagline);
            writer.WriteString("url", NormaliseOrigin(site.CanonicalOrigin));

            if (!string.IsNullOrWhiteSpace(site.Email))
            {
                writer.WriteString("email", $"mailto:{site.Email}");
            }

            if (!string.IsNullOrWhiteSpace(site.Location))
            {
                writer.WriteStartObject("address");
                writer.WriteString("@type", "PostalAddress");
                writer.WriteString("addressLocality", site.Location);
                writer.WriteEndObject();
            }

            if (site.Social.Count > 0)
            {
                writer.WriteStartArray("sameAs");
                foreach (var link in site.Social)
                {
                    writer.WriteStringValue(link.Url);
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        });
    }

    /// <summary>Describes a single case study.</summary>
    /// <param name="project">The project being viewed.</param>
    /// <param name="site">Site identity, used for the author and to absolutise URLs.</param>
    public static string Project(Project project, SiteContent site)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(site);

        var origin = NormaliseOrigin(site.CanonicalOrigin);

        return Write(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("@context", Context);
            writer.WriteString("@type", "CreativeWork");
            writer.WriteString("name", project.Title);
            writer.WriteString("description", project.Summary);
            writer.WriteString("url", $"{origin}/work/{project.Slug}");
            writer.WriteString("image", Absolute(origin, project.Cover.Src));
            writer.WriteString("genre", project.Category);

            if (project.Year > 0)
            {
                writer.WriteString("dateCreated", project.Year.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrWhiteSpace(project.Location))
            {
                writer.WriteString("locationCreated", project.Location);
            }

            writer.WriteStartObject("author");
            writer.WriteString("@type", "Person");
            writer.WriteString("name", site.Name);
            writer.WriteString("url", origin);
            writer.WriteEndObject();

            writer.WriteEndObject();
        });
    }

    private static string Write(Action<Utf8JsonWriter> body)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            body(writer);
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static string NormaliseOrigin(string origin) => origin.TrimEnd('/');

    private static string Absolute(string origin, string path)
        => path.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? path
            : $"{origin}/{path.TrimStart('/')}";
}
