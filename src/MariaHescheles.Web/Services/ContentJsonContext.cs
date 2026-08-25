using System.Text.Json.Serialization;
using MariaHescheles.Web.Content;

namespace MariaHescheles.Web.Services;

/// <summary>
/// Compile-time JSON contracts for every type loaded from <c>wwwroot/data</c>.
/// </summary>
/// <remarks>
/// <para>
/// Source generation is not an optimisation here, it is a correctness requirement.
/// Published WebAssembly output is IL-trimmed, and reflection-based
/// <c>JsonSerializer</c> calls silently lose members that the trimmer could not see.
/// Generating the contracts at compile time means the trimmer keeps exactly what is used,
/// and a missing contract is a build error instead of an empty page in production.
/// </para>
/// <para>
/// Adding a new content type? Add a <see cref="JsonSerializableAttribute"/> line for it here.
/// </para>
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    UseStringEnumConverter = true,
    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    // Without this, the "$type" discriminator on a content block must be the very first
    // property in its JSON object or deserialisation throws. That is an unreasonable trap
    // for someone hand-editing content: adding a note above the type would break the site.
    // The cost is a little buffering while reading, which is irrelevant at this file size.
    AllowOutOfOrderMetadataProperties = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(SiteContent))]
[JsonSerializable(typeof(AboutContent))]
[JsonSerializable(typeof(IReadOnlyList<Project>))]
[JsonSerializable(typeof(Project))]
[JsonSerializable(typeof(IReadOnlyList<StudioCollection>))]
[JsonSerializable(typeof(StudioCollection))]
[JsonSerializable(typeof(ContentBlock))]
[JsonSerializable(typeof(ProseBlock))]
[JsonSerializable(typeof(ImageBlock))]
[JsonSerializable(typeof(GalleryBlock))]
[JsonSerializable(typeof(QuoteBlock))]
[JsonSerializable(typeof(SpecificationsBlock))]
[JsonSerializable(typeof(ComparisonBlock))]
[JsonSerializable(typeof(SceneBlock))]
internal sealed partial class ContentJsonContext : JsonSerializerContext;
