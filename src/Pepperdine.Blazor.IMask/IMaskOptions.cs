namespace Pepperdine.Blazor.IMask;

/// <summary>
/// Flexible option bag for IMask.js.
/// </summary>
[PublicAPI]
// ReSharper disable once InconsistentNaming
public sealed class IMaskOptions
{
    [JsonPropertyName("mask")]
    public object? Mask { get; set; }

    [JsonExtensionData]
    public IDictionary<string, object?> AdditionalOptions { get; } =
        new Dictionary<string, object?>(StringComparer.Ordinal);

    public IMaskOptions()
    {
    }

    public IMaskOptions(object? maskOption)
    {
        Mask = maskOption;
    }

    public IMaskOptions SetOption(string optionName, object? optionValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(optionName);

        AdditionalOptions[optionName] = optionValue;
        return this;
    }
}
