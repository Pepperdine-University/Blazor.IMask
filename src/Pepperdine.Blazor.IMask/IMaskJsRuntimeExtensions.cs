namespace Pepperdine.Blazor.IMask;

/// <summary>
/// Convenience methods for using IMask.js without registering <see cref="IIMaskService" />.
/// </summary>
[PublicAPI]
// ReSharper disable once InconsistentNaming
public static class IMaskJsRuntimeExtensions
{
    internal const string MODULE_ASSET_PATH = "./_content/Pepperdine.Blazor.IMask/imask-blazor.js";

    public static async ValueTask<IMaskHandle> ApplyMaskAsync(
        this IJSRuntime javaScriptRuntime,
        ElementReference targetElement,
        object? maskOptions = null,
        CancellationToken cancellationToken = default)
    {
        return await ApplyMaskWithOwnedModuleReferenceAsync(
            javaScriptRuntime,
            "apply",
            cancellationToken,
            targetElement,
            maskOptions).ConfigureAwait(false);
    }

    public static async ValueTask<IMaskHandle> ApplyMaskByElementIdentifierAsync(
        this IJSRuntime javaScriptRuntime,
        string elementIdentifier,
        object? maskOptions = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementIdentifier);

        return await ApplyMaskWithOwnedModuleReferenceAsync(
            javaScriptRuntime,
            "applyByElementIdentifier",
            cancellationToken,
            elementIdentifier,
            maskOptions).ConfigureAwait(false);
    }

    public static async ValueTask<IMaskHandle> ApplyMaskByCssSelectorAsync(
        this IJSRuntime javaScriptRuntime,
        string cssSelector,
        object? maskOptions = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cssSelector);

        return await ApplyMaskWithOwnedModuleReferenceAsync(
            javaScriptRuntime,
            "applyByCssSelector",
            cancellationToken,
            cssSelector,
            maskOptions).ConfigureAwait(false);
    }

    public static async ValueTask RefreshMasksAsync(
        this IJSRuntime javaScriptRuntime,
        CancellationToken cancellationToken = default)
    {
        await InvokeModuleFunctionAsync(
            javaScriptRuntime,
            "refresh",
            cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask DestroyMaskAsync(
        this IJSRuntime javaScriptRuntime,
        ElementReference targetElement,
        CancellationToken cancellationToken = default)
    {
        await InvokeModuleFunctionAsync(
            javaScriptRuntime,
            "destroy",
            cancellationToken,
            targetElement).ConfigureAwait(false);
    }

    public static async ValueTask DestroyMaskByElementIdentifierAsync(
        this IJSRuntime javaScriptRuntime,
        string elementIdentifier,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementIdentifier);

        await InvokeModuleFunctionAsync(
            javaScriptRuntime,
            "destroyByElementIdentifier",
            cancellationToken,
            elementIdentifier).ConfigureAwait(false);
    }

    public static async ValueTask DestroyMaskByCssSelectorAsync(
        this IJSRuntime javaScriptRuntime,
        string cssSelector,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cssSelector);

        await InvokeModuleFunctionAsync(
            javaScriptRuntime,
            "destroyByCssSelector",
            cancellationToken,
            cssSelector).ConfigureAwait(false);
    }

    private static async ValueTask<IMaskHandle> ApplyMaskWithOwnedModuleReferenceAsync(
        IJSRuntime javaScriptRuntime,
        string functionName,
        CancellationToken cancellationToken,
        params object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(javaScriptRuntime);

        IJSObjectReference moduleReference = await javaScriptRuntime
            .InvokeAsync<IJSObjectReference>("import", cancellationToken, MODULE_ASSET_PATH)
            .ConfigureAwait(false);

        try
        {
            IJSObjectReference maskReference = await moduleReference
                .InvokeAsync<IJSObjectReference>(functionName, cancellationToken, arguments)
                .ConfigureAwait(false);

            return new IMaskHandle(maskReference, moduleReference);
        }
        catch
        {
            try
            {
                await moduleReference.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException)
            {
            }

            throw;
        }
    }

    private static async ValueTask InvokeModuleFunctionAsync(
        IJSRuntime javaScriptRuntime,
        string functionName,
        CancellationToken cancellationToken,
        params object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(javaScriptRuntime);

        await using IJSObjectReference moduleReference = await javaScriptRuntime
            .InvokeAsync<IJSObjectReference>("import", cancellationToken, MODULE_ASSET_PATH)
            .ConfigureAwait(false);
        await moduleReference
            .InvokeVoidAsync(functionName, cancellationToken, arguments)
            .ConfigureAwait(false);
    }
}
