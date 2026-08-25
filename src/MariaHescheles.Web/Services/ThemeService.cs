using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MariaHescheles.Web.Services;

/// <summary>The visitor's colour-scheme choice.</summary>
public enum ThemePreference
{
    /// <summary>Follow the operating system, and keep following it when it changes.</summary>
    System,

    Light,

    Dark,
}

/// <summary>
/// Reads and writes the colour scheme, and raises <see cref="Changed"/> whenever the
/// effective scheme changes — including when the operating system switches while the tab
/// is open and the preference is <see cref="ThemePreference.System"/>.
/// </summary>
public interface IThemeService
{
    /// <summary>The stored choice, which may be <see cref="ThemePreference.System"/>.</summary>
    ThemePreference Preference { get; }

    /// <summary>The scheme actually in effect once <see cref="ThemePreference.System"/> is resolved.</summary>
    bool IsDark { get; }

    event EventHandler? Changed;

    /// <summary>Synchronises with the value the inline boot script already applied. Idempotent.</summary>
    Task InitialiseAsync();

    Task SetAsync(ThemePreference preference);

    /// <summary>Flips between light and dark, resolving <see cref="ThemePreference.System"/> first.</summary>
    Task ToggleAsync();
}

/// <inheritdoc cref="IThemeService"/>
/// <remarks>
/// <para>
/// The scheme is applied to <c>&lt;html data-theme&gt;</c> by a small synchronous script in
/// <c>index.html</c> that runs before first paint. That script, not this service, is what
/// prevents a white flash on a dark-mode device — WebAssembly starts far too late to help.
/// This type's job is to keep the C# world in agreement with what the DOM already shows.
/// </para>
/// </remarks>
internal sealed class ThemeService : IThemeService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly string _moduleUrl;
    private DotNetObjectReference<ThemeService>? _selfReference;
    private IJSObjectReference? _module;
    private bool _initialised;

    public ThemeService(IJSRuntime jsRuntime, NavigationManager navigation)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        ArgumentNullException.ThrowIfNull(navigation);

        _jsRuntime = jsRuntime;
        _moduleUrl = navigation.ToAbsoluteUri("js/theme.js").ToString();
    }

    public ThemePreference Preference { get; private set; } = ThemePreference.System;

    public bool IsDark { get; private set; }

    public event EventHandler? Changed;

    public async Task InitialiseAsync()
    {
        if (_initialised)
        {
            return;
        }

        _initialised = true;
        _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", _moduleUrl).ConfigureAwait(false);
        _selfReference = DotNetObjectReference.Create(this);

        // Returns "light" or "dark": the scheme the boot script already committed to.
        var resolved = await _module.InvokeAsync<string>("initialise", _selfReference).ConfigureAwait(false);
        var stored = await _module.InvokeAsync<string?>("readPreference").ConfigureAwait(false);

        Preference = Parse(stored);
        IsDark = string.Equals(resolved, "dark", StringComparison.Ordinal);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task SetAsync(ThemePreference preference)
    {
        await InitialiseAsync().ConfigureAwait(false);

        Preference = preference;
        var resolved = await _module!.InvokeAsync<string>("apply", Serialise(preference)).ConfigureAwait(false);

        IsDark = string.Equals(resolved, "dark", StringComparison.Ordinal);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public Task ToggleAsync() => SetAsync(IsDark ? ThemePreference.Light : ThemePreference.Dark);

    /// <summary>
    /// Invoked from JavaScript when the OS colour scheme changes while the preference is
    /// <see cref="ThemePreference.System"/>.
    /// </summary>
    /// <param name="resolved">Either <c>"light"</c> or <c>"dark"</c>.</param>
    [JSInvokable]
    public void OnSystemSchemeChanged(string resolved)
    {
        var isDark = string.Equals(resolved, "dark", StringComparison.Ordinal);
        if (isDark == IsDark)
        {
            return;
        }

        IsDark = isDark;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
            {
                await _module.InvokeVoidAsync("shutdown").ConfigureAwait(false);
                await _module.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (JSDisconnectedException)
        {
            // Browser context already gone.
        }
        finally
        {
            _selfReference?.Dispose();
        }
    }

    private static string Serialise(ThemePreference preference) => preference switch
    {
        ThemePreference.Light => "light",
        ThemePreference.Dark => "dark",
        _ => "system",
    };

    private static ThemePreference Parse(string? value) => value switch
    {
        "light" => ThemePreference.Light,
        "dark" => ThemePreference.Dark,
        _ => ThemePreference.System,
    };
}
