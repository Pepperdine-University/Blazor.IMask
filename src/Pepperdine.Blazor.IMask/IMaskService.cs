namespace Pepperdine.Blazor.IMask;

// ReSharper disable once InconsistentNaming
internal sealed class IMaskService : IIMaskService
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleReference;

    public IMaskService(IJSRuntime javaScriptRuntime)
    {
        ArgumentNullException.ThrowIfNull(javaScriptRuntime);

        _moduleReference = new Lazy<Task<IJSObjectReference>>(() =>
            javaScriptRuntime
                .InvokeAsync<IJSObjectReference>(
                    "import",
                    IMaskJsRuntimeExtensions.MODULE_ASSET_PATH)
                .AsTask());
    }

    public async ValueTask<IMaskHandle> ApplyMaskAsync(
        ElementReference targetElement,
        object? maskOptions = null,
        CancellationToken cancellationToken = default)
    {
        return await ApplyMaskWithModuleReferenceAsync(
            "apply",
            cancellationToken,
            targetElement,
            maskOptions).ConfigureAwait(false);
    }

    public async ValueTask<IMaskHandle> ApplyMaskByElementIdentifierAsync(
        string elementIdentifier,
        object? maskOptions = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementIdentifier);

        return await ApplyMaskWithModuleReferenceAsync(
            "applyByElementIdentifier",
            cancellationToken,
            elementIdentifier,
            maskOptions).ConfigureAwait(false);
    }

    public async ValueTask<IMaskHandle> ApplyMaskByCssSelectorAsync(
        string cssSelector,
        object? maskOptions = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cssSelector);

        return await ApplyMaskWithModuleReferenceAsync(
            "applyByCssSelector",
            cancellationToken,
            cssSelector,
            maskOptions).ConfigureAwait(false);
    }

    public async ValueTask RefreshMasksAsync(CancellationToken cancellationToken = default)
    {
        await InvokeModuleFunctionAsync("refresh", cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DestroyMaskAsync(
        ElementReference targetElement,
        CancellationToken cancellationToken = default)
    {
        await InvokeModuleFunctionAsync(
            "destroy",
            cancellationToken,
            targetElement).ConfigureAwait(false);
    }

    public async ValueTask DestroyMaskByElementIdentifierAsync(
        string elementIdentifier,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementIdentifier);

        await InvokeModuleFunctionAsync(
            "destroyByElementIdentifier",
            cancellationToken,
            elementIdentifier).ConfigureAwait(false);
    }

    public async ValueTask DestroyMaskByCssSelectorAsync(
        string cssSelector,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cssSelector);

        await InvokeModuleFunctionAsync(
            "destroyByCssSelector",
            cancellationToken,
            cssSelector).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_moduleReference.IsValueCreated)
        {
            return;
        }

        try
        {
            IJSObjectReference moduleReference = await _moduleReference.Value.ConfigureAwait(false);
            await moduleReference.DisposeAsync().ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
        }
    }

    private async ValueTask<IJSObjectReference> GetModuleReferenceAsync(
        CancellationToken cancellationToken)
    {
        IJSObjectReference moduleReference = await _moduleReference.Value.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return moduleReference;
    }

    private async ValueTask<IMaskHandle> ApplyMaskWithModuleReferenceAsync(
        string functionName,
        CancellationToken cancellationToken,
        params object?[] arguments)
    {
        IJSObjectReference moduleReference = await GetModuleReferenceAsync(cancellationToken)
            .ConfigureAwait(false);
        IJSObjectReference maskReference = await moduleReference
            .InvokeAsync<IJSObjectReference>(functionName, cancellationToken, arguments)
            .ConfigureAwait(false);

        return new IMaskHandle(maskReference);
    }

    private async ValueTask InvokeModuleFunctionAsync(
        string functionName,
        CancellationToken cancellationToken,
        params object?[] arguments)
    {
        IJSObjectReference moduleReference = await GetModuleReferenceAsync(cancellationToken)
            .ConfigureAwait(false);
        await moduleReference
            .InvokeVoidAsync(functionName, cancellationToken, arguments)
            .ConfigureAwait(false);
    }
}
