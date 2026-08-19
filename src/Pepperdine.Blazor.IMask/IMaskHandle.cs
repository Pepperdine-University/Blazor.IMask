namespace Pepperdine.Blazor.IMask;

/// <summary>
/// Represents a JavaScript IMask instance attached to a field.
/// </summary>
[PublicAPI]
// ReSharper disable once InconsistentNaming
public sealed class IMaskHandle : IAsyncDisposable
{
    private readonly IJSObjectReference _maskReference;
    private readonly IJSObjectReference? _ownedModuleReference;
    private bool _isDisposed;

    internal IMaskHandle(
        IJSObjectReference maskReference,
        IJSObjectReference? ownedModuleReference = null)
    {
        _maskReference = maskReference;
        _ownedModuleReference = ownedModuleReference;
    }

    public ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = default)
    {
        return InvokeFunctionAsync<string>("getValue", cancellationToken);
    }

    public ValueTask SetValueAsync(
        string? maskedValue,
        CancellationToken cancellationToken = default)
    {
        return InvokeFunctionAsync("setValue", cancellationToken, maskedValue);
    }

    public ValueTask<string?> GetUnmaskedValueAsync(CancellationToken cancellationToken = default)
    {
        return InvokeFunctionAsync<string>("getUnmaskedValue", cancellationToken);
    }

    public ValueTask SetUnmaskedValueAsync(
        string? unmaskedValue,
        CancellationToken cancellationToken = default)
    {
        return InvokeFunctionAsync("setUnmaskedValue", cancellationToken, unmaskedValue);
    }

    public ValueTask<TValue?> GetTypedValueAsync<TValue>(
        CancellationToken cancellationToken = default)
    {
        return InvokeFunctionAsync<TValue>("getTypedValue", cancellationToken);
    }

    public ValueTask SetTypedValueAsync<TValue>(
        TValue? typedValue,
        CancellationToken cancellationToken = default)
    {
        return InvokeFunctionAsync("setTypedValue", cancellationToken, typedValue);
    }

    public ValueTask UpdateOptionsAsync(
        object maskOptions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(maskOptions);
        return InvokeFunctionAsync("updateOptions", cancellationToken, maskOptions);
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        try
        {
            await _maskReference.InvokeVoidAsync("destroy").ConfigureAwait(false);
            await _maskReference.DisposeAsync().ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
        }

        if (_ownedModuleReference is not null)
        {
            try
            {
                await _ownedModuleReference.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }

    private ValueTask<TValue?> InvokeFunctionAsync<TValue>(
        string functionName,
        CancellationToken cancellationToken,
        params object?[] arguments)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return _maskReference.InvokeAsync<TValue?>(functionName, cancellationToken, arguments);
    }

    private ValueTask InvokeFunctionAsync(
        string functionName,
        CancellationToken cancellationToken,
        params object?[] arguments)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return _maskReference.InvokeVoidAsync(functionName, cancellationToken, arguments);
    }
}
