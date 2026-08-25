using MariaHescheles.Web.Interop;
using Microsoft.Extensions.DependencyInjection;

namespace MariaHescheles.Web.Services;

/// <summary>
/// Registers everything the application resolves at runtime.
/// </summary>
/// <remarks>
/// Keeping registration in one place means <c>Program.cs</c> stays a three-line file and the
/// composition of the app is readable at a glance.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds content loading, theming and the JavaScript interop bridges.
    /// </summary>
    /// <param name="services">The container being built.</param>
    /// <param name="baseAddress">
    /// <c>WebAssemblyHostBuilder.HostEnvironment.BaseAddress</c>. Every content request is made
    /// relative to it, which is what makes root and subpath deployments share one build.
    /// </param>
    public static IServiceCollection AddSiteServices(this IServiceCollection services, string baseAddress)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseAddress);

        services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(baseAddress) });

        services.AddScoped<IContentService, ContentService>();
        services.AddScoped<IThemeService, ThemeService>();

        // In WebAssembly there is exactly one scope for the lifetime of the tab, so "scoped"
        // here means one shared instance. That is intentional for the interop bridges: one
        // imported module, one IntersectionObserver, one animation frame loop for the whole app.
        services.AddScoped<MotionInterop>();
        services.AddScoped<DocumentInterop>();
        services.AddScoped<SceneInterop>();

        return services;
    }
}
