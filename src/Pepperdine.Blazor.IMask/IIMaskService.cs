namespace Pepperdine.Blazor.IMask;

/// <summary>
/// Applies IMask.js behavior to existing DOM elements.
/// </summary>
[PublicAPI]
public interface IIMaskService : IAsyncDisposable
{
    /// <summary>
    /// Applies an IMask instance to an element. Pass a string preset,
    /// an <see cref="IMaskOptions" />, or an anonymous object.
    /// </summary>
    ValueTask<IMaskHandle> ApplyMaskAsync(
        ElementReference targetElement,
        object? maskOptions = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies an IMask instance to the element with the supplied element identifier.
    /// </summary>
    ValueTask<IMaskHandle> ApplyMaskByElementIdentifierAsync(
        string elementIdentifier,
        object? maskOptions = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies an IMask instance to the first element matching the supplied CSS selector.
    /// </summary>
    ValueTask<IMaskHandle> ApplyMaskByCssSelectorAsync(
        string cssSelector,
        object? maskOptions = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-scans the document for fields that declare data-imask attributes.
    /// </summary>
    ValueTask RefreshMasksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Destroys the IMask instance attached to an element, when one exists.
    /// </summary>
    ValueTask DestroyMaskAsync(
        ElementReference targetElement,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Destroys the IMask instance attached to the element with the supplied element identifier,
    /// when one exists.
    /// </summary>
    ValueTask DestroyMaskByElementIdentifierAsync(
        string elementIdentifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Destroys the IMask instance attached to the first element matching the supplied
    /// CSS selector, when one exists.
    /// </summary>
    ValueTask DestroyMaskByCssSelectorAsync(
        string cssSelector,
        CancellationToken cancellationToken = default);
}
