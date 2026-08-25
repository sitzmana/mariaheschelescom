namespace MariaHescheles.Web.Services;

/// <summary>
/// Runs an asynchronous factory at most once and hands the same result to every caller.
/// </summary>
/// <remarks>
/// <para>
/// Content JSON never changes for the lifetime of a page load, so re-fetching it on every
/// navigation would be pure waste. Two details matter:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///     Concurrent callers share one request. Three components asking for the project list
///     during the same render produce one network round trip, not three.
///     </description>
///   </item>
///   <item>
///     <description>
///     Failures are not cached. A request that fails because the visitor went through a
///     tunnel must be retryable; caching the faulted task would break the page until reload.
///     </description>
///   </item>
/// </list>
/// <para>
/// A caller's <see cref="CancellationToken"/> abandons that caller's <em>wait</em>, not the
/// shared work, so one component unmounting cannot cancel the fetch another is relying on.
/// </para>
/// </remarks>
/// <typeparam name="T">The cached value type.</typeparam>
internal sealed class AsyncCache<T>(Func<Task<T>> factory)
{
    private readonly Func<Task<T>> _factory = factory;
    private Task<T>? _inFlight;
    private T? _value;
    private bool _hasValue;

    public Task<T> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_hasValue)
        {
            return Task.FromResult(_value!);
        }

        _inFlight ??= LoadAsync();

        return cancellationToken.CanBeCanceled
            ? _inFlight.WaitAsync(cancellationToken)
            : _inFlight;
    }

    private async Task<T> LoadAsync()
    {
        try
        {
            _value = await _factory().ConfigureAwait(false);
            _hasValue = true;
            return _value;
        }
        catch
        {
            _inFlight = null;
            throw;
        }
    }
}
