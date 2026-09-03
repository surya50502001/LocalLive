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
        var connectionString = ResolveConnectionString(configuration);

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

    private static string ResolveConnectionString(IConfiguration configuration)
    {
        var conn = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(conn))
        {
            conn = configuration["DATABASE_URL"]
                ?? configuration["DATABASE_URI"]
                ?? configuration["POSTGRES_URL"];
        }

        if (string.IsNullOrWhiteSpace(conn))
        {
            throw new InvalidOperationException(
                "Database connection string is not configured. Set 'ConnectionStrings:DefaultConnection' or 'DATABASE_URL'.");
        }

        conn = conn.Trim();

        if (conn.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            conn.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(conn);
            var userInfo = uri.UserInfo.Split(':');
            var username = Uri.UnescapeDataString(userInfo[0]);
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.TrimStart('/');

            var builder = new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Database = database,
                Username = username,
                Password = password,
                SslMode = Npgsql.SslMode.Prefer
            };
            return builder.ConnectionString;
        }

        return conn;
    }
}
