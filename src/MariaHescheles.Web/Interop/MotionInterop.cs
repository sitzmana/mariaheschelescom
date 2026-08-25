using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MariaHescheles.Web.Interop;

/// <summary>
/// How an element's scroll progress is measured.
/// </summary>
public enum ProgressMode
{
    /// <summary>
    /// <c>0</c> when the element's top edge touches the bottom of the viewport, <c>1</c> when its
    /// bottom edge leaves the top. Use for elements that pass through the viewport, such as a
    /// parallax photograph.
    /// </summary>
    Cover,

    /// <summary>
    /// <c>0</c> when the element's top reaches the top of the viewport, <c>1</c> when its bottom
    /// does. Use for tall containers holding a <c>position: sticky</c> child — this is the
    /// measurement behind pinned, scroll-scrubbed sequences.
    /// </summary>
    Contain,
}

/// <summary>
/// The bridge to <c>wwwroot/js/motion.js</c>: reveal-on-scroll and scroll-linked progress.
/// </summary>
/// <remarks>
/// <para>
/// Only primitives cross this boundary. Blazor's default JavaScript interop serialiser is
/// reflection-based, which is fragile once the published output has been IL-trimmed;
/// primitives are immune to that entire class of problem. It costs a slightly longer
/// parameter list and buys certainty that the site behaves identically in development and
/// in production.
/// </para>
/// </remarks>
internal sealed class MotionInterop(IJSRuntime jsRuntime, NavigationManager navigation)
    : JsModuleInterop(jsRuntime, navigation, "js/motion.js")
{
    /// <summary>
    /// Adds <c>is-revealed</c> to <paramref name="element"/> the first time it enters the
    /// viewport, which is what the CSS transitions key off.
    /// </summary>
    /// <param name="element">The element to watch.</param>
    /// <param name="once">When <see langword="true"/> the element stops being observed after the first reveal.</param>
    /// <remarks>
    /// The trigger point is fixed in <c>motion.js</c> rather than being passed per element.
    /// One shared observer can only hold one configuration, and a uniform trigger line is
    /// what makes a long page read as a single document instead of a stack of sections
    /// animating to their own rhythms.
    /// </remarks>
    public async ValueTask ObserveRevealAsync(ElementReference element, bool once = true)
    {
        var module = await GetModuleAsync().ConfigureAwait(false);
        await module.InvokeVoidAsync("observeReveal", element, once).ConfigureAwait(false);
    }

    /// <summary>
    /// Continuously writes the element's scroll progress into its own <c>--progress</c> CSS
    /// custom property, as a unitless number from 0 to 1.
    /// </summary>
    /// <remarks>
    /// Everything downstream is pure CSS. Handing the value to the style system instead of
    /// re-rendering Blazor keeps scroll work off the .NET thread entirely — no component ever
    /// re-renders because the page scrolled.
    /// </remarks>
    public async ValueTask TrackProgressAsync(ElementReference element, ProgressMode mode)
    {
        var module = await GetModuleAsync().ConfigureAwait(false);
        await module.InvokeVoidAsync("trackProgress", element, mode == ProgressMode.Contain ? "contain" : "cover")
                    .ConfigureAwait(false);
    }

    /// <summary>Stops all observation of an element. Safe to call for an element that was never registered.</summary>
    public async ValueTask ReleaseAsync(ElementReference element)
    {
        var module = await GetModuleAsync().ConfigureAwait(false);
        await module.InvokeVoidAsync("release", element).ConfigureAwait(false);
    }

    /// <summary>
    /// Reports whether the visitor has asked their operating system to reduce motion.
    /// </summary>
    /// <remarks>
    /// Honouring this is not optional. For people with vestibular disorders, parallax and
    /// scroll-scrubbed animation can cause genuine nausea.
    /// </remarks>
    public async ValueTask<bool> PrefersReducedMotionAsync()
    {
        var module = await GetModuleAsync().ConfigureAwait(false);
        return await module.InvokeAsync<bool>("prefersReducedMotion").ConfigureAwait(false);
    }

    protected override async ValueTask DisposeCoreAsync()
    {
        try
        {
            var module = await GetModuleAsync().ConfigureAwait(false);
            await module.InvokeVoidAsync("shutdown").ConfigureAwait(false);
        }
        catch (JSException)
        {
            // The module never finished importing. Nothing was registered, so nothing leaks.
        }
    }
}
