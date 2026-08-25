using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MariaHescheles.Web.Interop;

/// <summary>
/// Base class for a lazily imported JavaScript ES module.
/// </summary>
/// <remarks>
/// <para>
/// The module is fetched on first use rather than at start-up, so a visitor who never reaches
/// a 3D model never downloads the WebGL code.
/// </para>
/// <para>
/// The relative path is resolved against <see cref="NavigationManager.BaseUri"/> rather than
/// being handed to <c>import()</c> as-is. Relative specifier resolution inside a dynamic
/// import depends on the importing script's URL, which is inside <c>_framework/</c> — resolving
/// explicitly is what makes the app work unchanged when it is served from a subpath such as
/// <c>https://user.github.io/mariaheschelescom/</c>.
/// </para>
/// </remarks>
internal abstract class JsModuleInterop : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly string _absoluteModuleUrl;
    private Task<IJSObjectReference>? _module;
    private bool _disposed;

    /// <param name="jsRuntime">The Blazor JavaScript runtime.</param>
    /// <param name="navigation">Used to resolve <paramref name="relativeModulePath"/> to an absolute URL.</param>
    /// <param name="relativeModulePath">Path below the app base, e.g. <c>js/motion.js</c>.</param>
    protected JsModuleInterop(IJSRuntime jsRuntime, NavigationManager navigation, string relativeModulePath)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativeModulePath);

        _jsRuntime = jsRuntime;
        _absoluteModuleUrl = navigation.ToAbsoluteUri(relativeModulePath).ToString();
    }

    /// <summary>Imports the module, or returns the already imported instance.</summary>
    protected ValueTask<IJSObjectReference> GetModuleAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _module ??= _jsRuntime.InvokeAsync<IJSObjectReference>("import", _absoluteModuleUrl).AsTask();
        return new ValueTask<IJSObjectReference>(_module);
    }

    /// <summary>Releases derived-class resources before the module reference itself is released.</summary>
    protected virtual ValueTask DisposeCoreAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        GC.SuppressFinalize(this);

        try
        {
            await DisposeCoreAsync().ConfigureAwait(false);

            if (_module is not null)
            {
                var module = await _module.ConfigureAwait(false);
                await module.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (JSDisconnectedException)
        {
            // The browser context is already gone; there is nothing left to release.
        }
        catch (OperationCanceledException)
        {
            // Teardown raced with a navigation. Same conclusion.
        }
    }
}
