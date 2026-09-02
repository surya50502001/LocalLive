using LocalLive.Application.Common.Interfaces;
using LocalLive.Domain.Common.Services;
using LocalLive.Infrastructure.Auth;
using LocalLive.Infrastructure.GeoSpatial;
using LocalLive.Infrastructure.Persistence;
using LocalLive.Infrastructure.Realtime;
using LocalLive.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LocalLive.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            });
        });

        // Options
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<NavigationOptions>(configuration.GetSection("Navigation"));

        // Geo
        services.AddSingleton<IDistanceCalculator, HaversineDistanceCalculator>();
        services.AddSingleton<INavigationProvider, NavigationProvider>();

        // Auth
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Realtime
        services.AddSingleton<IRealtimeNotifier, RealtimeNotifier>();

        // Application services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IShopService, ShopService>();
        services.AddScoped<IRequestService, RequestService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAdminService, AdminService>();

        services.AddHostedService<RequestExpiryHostedService>();

        return services;
    }
}
