namespace Pepperdine.Blazor.IMask;

/// <summary>
/// Built-in IMask.js preset names that can be used with data-imask attributes or passed to
/// <see cref="IIMaskService.ApplyMaskAsync" />.
/// </summary>
[PublicAPI]
// ReSharper disable once InconsistentNaming
public static class IMaskPresets
{
    public static string PhoneUs => "phone-us";

    public static string Ssn => "ssn";

    public static string ZipUs => "zip-us";

    public static string Number => "number";

    public static string Integer => "integer";

    public static string CurrencyUs => "currency-us";
}
