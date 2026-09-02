using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace LocalLive.Application;

/// <summary>Marker for assembly scanning (validators).</summary>
public sealed class ApplicationMarker { }

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ApplicationMarker>();
        return services;
    }
}
