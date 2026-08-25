namespace MariaHescheles.Web.Content;

/// <summary>
/// Everything the WebGL viewer needs to present a single glTF model.
/// </summary>
/// <remarks>
/// <para>
/// Keep models under roughly 5 MB. Export <c>.glb</c> (binary glTF) with Draco geometry
/// compression and KTX2/Basis textures; see <c>docs/motion-and-3d.md</c> for the exact
/// export recipe. Large models are the single easiest way to ruin the feel of the site.
/// </para>
/// </remarks>
public sealed record Scene3D
{
    /// <summary>Root-relative path to a <c>.glb</c> or <c>.gltf</c> file.</summary>
    public required string ModelUrl { get; init; }

    /// <summary>
    /// Text alternative describing the object. The 3D canvas is inert to assistive
    /// technology, so this is the only description a screen reader receives.
    /// </summary>
    public required string Alt { get; init; }

    /// <summary>
    /// Still image shown before the viewer is loaded. The heavy WebGL code and the model
    /// are only fetched once the poster scrolls into view, so this is what most visitors
    /// on slow connections will actually see first.
    /// </summary>
    public MediaAsset? Poster { get; init; }

    /// <summary>
    /// Camera distance as a multiple of the model's bounding-sphere radius.
    /// <c>1.0</c> frames the model tightly; larger values pull back.
    /// </summary>
    public double CameraDistance { get; init; } = 2.2;

    /// <summary>Initial horizontal camera angle in degrees, measured clockwise from the front.</summary>
    public double CameraAzimuth { get; init; } = 35;

    /// <summary>Initial vertical camera angle in degrees above the horizon.</summary>
    public double CameraElevation { get; init; } = 12;

    /// <summary>Vertical field of view in degrees. Lower values flatten perspective.</summary>
    public double FieldOfView { get; init; } = 35;

    /// <summary>
    /// Slow turntable rotation while the model is on screen and untouched.
    /// Automatically suppressed when the visitor prefers reduced motion.
    /// </summary>
    public bool AutoRotate { get; init; } = true;

    /// <summary>Exposure compensation in stops applied to the tone mapper.</summary>
    public double ExposureStops { get; init; }

    /// <summary>Renders a soft contact shadow beneath the model.</summary>
    public bool GroundShadow { get; init; } = true;

    /// <summary>
    /// Any CSS colour used to clear the canvas, or <see langword="null"/> for a transparent
    /// canvas that lets the page background show through.
    /// </summary>
    public string? Background { get; init; }
}
