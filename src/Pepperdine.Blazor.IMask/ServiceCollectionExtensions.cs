namespace Pepperdine.Blazor.IMask;

[PublicAPI]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIMask(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IIMaskService, IMaskService>();

        return services;
    }
}
