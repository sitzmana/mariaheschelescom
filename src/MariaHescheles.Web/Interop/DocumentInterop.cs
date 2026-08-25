using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MariaHescheles.Web.Interop;

/// <summary>
/// The bridge to <c>wwwroot/js/document.js</c>: the handful of document-level operations that
/// have no Blazor equivalent.
/// </summary>
internal sealed class DocumentInterop(IJSRuntime jsRuntime, NavigationManager navigation)
    : JsModuleInterop(jsRuntime, navigation, "js/document.js")
{
    /// <summary>
    /// Publishes a <c>&lt;script type="application/ld+json"&gt;</c> block, replacing any previous
    /// block with the same <paramref name="id"/>.
    /// </summary>
    /// <remarks>
    /// Structured data is what makes a search result show the project name, photograph and
    /// location rather than a bare URL. It cannot go through <c>HeadOutlet</c>: Blazor renders
    /// script elements as inert markup, so the JSON never becomes a parsable script node.
    /// </remarks>
    public async ValueTask SetStructuredDataAsync(string id, string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var module = await GetModuleAsync().ConfigureAwait(false);
        await module.InvokeVoidAsync("setStructuredData", id, json).ConfigureAwait(false);
    }

    /// <summary>
    /// Restores the scroll position after a route change.
    /// </summary>
    /// <param name="smooth">
    /// Ignored when the visitor prefers reduced motion. Even when honoured, a smooth jump
    /// across a long page is disorienting, so callers should reserve it for in-page anchors.
    /// </param>
    public async ValueTask ScrollToTopAsync(bool smooth = false)
    {
        var module = await GetModuleAsync().ConfigureAwait(false);
        await module.InvokeVoidAsync("scrollToTop", smooth).ConfigureAwait(false);
    }

    /// <summary>Scrolls an element with the given id into view, accounting for the fixed header.</summary>
    public async ValueTask ScrollToAnchorAsync(string elementId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementId);

        var module = await GetModuleAsync().ConfigureAwait(false);
        await module.InvokeVoidAsync("scrollToAnchor", elementId).ConfigureAwait(false);
    }

    /// <summary>
    /// Opens a native <c>&lt;dialog&gt;</c> as a modal and locks background scrolling.
    /// </summary>
    /// <param name="dialog">The dialog element.</param>
    /// <param name="owner">
    /// Notified via <c>OnDialogClosed()</c> however the dialog is dismissed — close button,
    /// Escape key or backdrop click. Routing every dismissal through one callback is what
    /// keeps the component's open/closed flag from drifting out of sync with the DOM.
    /// </param>
    /// <typeparam name="T">The component owning the dialog.</typeparam>
    /// <remarks>
    /// <c>showModal()</c> is used rather than a styled overlay because the platform already
    /// implements focus trapping, Escape to dismiss, background inertness and top-layer
    /// stacking. Every one of those is easy to reimplement almost correctly.
    /// </remarks>
    public async ValueTask OpenDialogAsync<T>(ElementReference dialog, DotNetObjectReference<T> owner)
        where T : class
    {
        var module = await GetModuleAsync().ConfigureAwait(false);
        await module.InvokeVoidAsync("openDialog", dialog, owner).ConfigureAwait(false);
    }

    /// <summary>Dismisses a dialog opened by <see cref="OpenDialogAsync{T}"/>.</summary>
    public async ValueTask CloseDialogAsync(ElementReference dialog)
    {
        var module = await GetModuleAsync().ConfigureAwait(false);
        await module.InvokeVoidAsync("closeDialog", dialog).ConfigureAwait(false);
    }

    /// <summary>
    /// Mirrors a range input's value into a CSS custom property on another element, in
    /// JavaScript, without involving the Blazor renderer.
    /// </summary>
    /// <param name="range">An <c>&lt;input type="range"&gt;</c>.</param>
    /// <param name="target">The element whose custom property is written.</param>
    /// <param name="property">The custom property name, including the leading <c>--</c>.</param>
    /// <remarks>
    /// Dragging a slider produces input events at the display refresh rate. Round-tripping
    /// those through <c>@bind</c> would mean a component render per frame to move one line on
    /// screen. Writing the value straight into the style system keeps the interaction on the
    /// compositor where it belongs.
    /// </remarks>
    public async ValueTask BindRangeToPropertyAsync(ElementReference range, ElementReference target, string property)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(property);

        var module = await GetModuleAsync().ConfigureAwait(false);
        await module.InvokeVoidAsync("bindRangeToProperty", range, target, property).ConfigureAwait(false);
    }

    /// <summary>Removes a binding created by <see cref="BindRangeToPropertyAsync"/>.</summary>
    public async ValueTask ReleaseRangeBindingAsync(ElementReference range)
    {
        var module = await GetModuleAsync().ConfigureAwait(false);
        await module.InvokeVoidAsync("releaseRangeBinding", range).ConfigureAwait(false);
    }
}
