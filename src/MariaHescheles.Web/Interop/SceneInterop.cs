using System.Text.Json;
using System.Text.Json.Serialization;
using MariaHescheles.Web.Content;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MariaHescheles.Web.Interop;

/// <summary>
/// Flattened, wire-ready form of a <see cref="Scene3D"/>, with the model URL already resolved
/// to an absolute address.
/// </summary>
internal sealed record SceneOptions
{
    public required string ModelUrl { get; init; }

    public double CameraDistance { get; init; }

    public double CameraAzimuth { get; init; }

    public double CameraElevation { get; init; }

    public double FieldOfView { get; init; }

    public bool AutoRotate { get; init; }

    public double ExposureStops { get; init; }

    public bool GroundShadow { get; init; }

    public string? Background { get; init; }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(SceneOptions))]
internal sealed partial class InteropJsonContext : JsonSerializerContext;

/// <summary>
/// A live WebGL viewer. Disposing it releases the GPU context, the geometry, the textures and
/// every observer the scene registered.
/// </summary>
/// <remarks>
/// Browsers cap the number of simultaneous WebGL contexts at around sixteen and silently drop
/// the oldest one past that limit. Deterministic disposal is what keeps a long browsing
/// session from ending with blank canvases.
/// </remarks>
public sealed class SceneViewer : IAsyncDisposable
{
    private readonly IJSObjectReference _handle;
    private bool _disposed;

    internal SceneViewer(IJSObjectReference handle) => _handle = handle;

    /// <summary>Starts or stops the idle turntable rotation.</summary>
    public ValueTask SetAutoRotateAsync(bool enabled)
        => _disposed ? ValueTask.CompletedTask : _handle.InvokeVoidAsync("setAutoRotate", enabled);

    /// <summary>Animates the camera back to its authored framing.</summary>
    public ValueTask ResetCameraAsync()
        => _disposed ? ValueTask.CompletedTask : _handle.InvokeVoidAsync("resetCamera");

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            await _handle.InvokeVoidAsync("dispose").ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
            // Page is tearing down; the GPU context goes with it.
        }
        catch (JSException)
        {
            // Never let a failed teardown surface as an unhandled exception during navigation.
        }
        finally
        {
            await _handle.DisposeAsync().ConfigureAwait(false);
        }
    }
}

/// <summary>
/// The bridge to <c>wwwroot/js/scene.js</c>, which owns all three.js usage.
/// </summary>
internal sealed class SceneInterop(IJSRuntime jsRuntime, NavigationManager navigation)
    : JsModuleInterop(jsRuntime, navigation, "js/scene.js")
{
    private readonly NavigationManager _navigation = navigation;

    /// <summary>Projects a content-authored <see cref="Scene3D"/> onto the wire format.</summary>
    public SceneOptions ToOptions(Scene3D scene)
    {
        ArgumentNullException.ThrowIfNull(scene);

        return new SceneOptions
        {
            ModelUrl = _navigation.ToAbsoluteUri(scene.ModelUrl).ToString(),
            CameraDistance = scene.CameraDistance,
            CameraAzimuth = scene.CameraAzimuth,
            CameraElevation = scene.CameraElevation,
            FieldOfView = scene.FieldOfView,
            AutoRotate = scene.AutoRotate,
            ExposureStops = scene.ExposureStops,
            GroundShadow = scene.GroundShadow,
            Background = scene.Background,
        };
    }

    /// <summary>
    /// Builds a viewer inside <paramref name="host"/> and begins streaming the model.
    /// </summary>
    /// <param name="host">An empty element that the canvas is appended to and sized against.</param>
    /// <param name="options">Camera, lighting and model settings.</param>
    /// <param name="callbacks">
    /// Receives <c>OnSceneProgress(double fraction)</c> and <c>OnSceneReady(string? error)</c>.
    /// </param>
    /// <typeparam name="T">The component receiving the callbacks.</typeparam>
    public async ValueTask<SceneViewer> CreateViewerAsync<T>(
        ElementReference host,
        SceneOptions options,
        DotNetObjectReference<T> callbacks)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(options);

        // Serialised here with a compile-time contract instead of being handed to Blazor's
        // reflection-based interop serialiser, which is not guaranteed to survive IL trimming.
        var json = JsonSerializer.Serialize(options, InteropJsonContext.Default.SceneOptions);

        var module = await GetModuleAsync().ConfigureAwait(false);
        var handle = await module.InvokeAsync<IJSObjectReference>("createViewer", host, json, callbacks)
                                 .ConfigureAwait(false);

        return new SceneViewer(handle);
    }
}
