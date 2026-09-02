using LocalLive.Domain.Entities;
using LocalLive.Domain.Enums;
using LocalLive.Infrastructure.Auth;
using LocalLive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LocalLive.Api.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration? configuration = null)
    {
        if (!await db.Categories.AnyAsync())
        {
            db.Categories.AddRange(new[]
            {
                new Category { Name = "Clothing", Slug = "clothing", Icon = "👕", SortOrder = 10 },
                new Category { Name = "Food & Snacks", Slug = "food-snacks", Icon = "🍔", SortOrder = 20 },
                new Category { Name = "Groceries", Slug = "groceries", Icon = "🛒", SortOrder = 30 },
                new Category { Name = "Electronics", Slug = "electronics", Icon = "🔌", SortOrder = 40 },
                new Category { Name = "Pharmacy", Slug = "pharmacy", Icon = "💊", SortOrder = 50 },
                new Category { Name = "Home & Tools", Slug = "home-tools", Icon = "🔧", SortOrder = 60 },
                new Category { Name = "Books & Stationery", Slug = "books-stationery", Icon = "📚", SortOrder = 70 },
                new Category { Name = "Beauty & Personal Care", Slug = "beauty-personal-care", Icon = "🧴", SortOrder = 80 },
                new Category { Name = "Sports & Fitness", Slug = "sports-fitness", Icon = "⚽", SortOrder = 90 },
                new Category { Name = "Other", Slug = "other", Icon = "📦", SortOrder = 100 }
            });
            await db.SaveChangesAsync();
        }

        await EnsureAdminUserAsync(db, configuration);

        var demoEnabled = configuration?.GetValue<bool>("SeedDemoData") ?? false;
        if (demoEnabled && !await db.Shops.AnyAsync() && !await db.Users.AnyAsync(u => u.Role == UserRole.ShopOwner))
        {
            await SeedDemoDataAsync(db);
        }
    }

    private static async Task EnsureAdminUserAsync(AppDbContext db, IConfiguration? configuration)
    {
        var adminEmail = configuration?.GetValue<string>("Admin:Email") ?? "admin@locallive.app";
        var adminPassword = configuration?.GetValue<string>("Admin:Password");
        if (string.IsNullOrWhiteSpace(adminPassword) || adminPassword.Length < 8)
        {
            // No bootstrap password configured: don't create an admin. Admins can be created manually.
            return;
        }

        if (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == adminEmail))
        {
            return;
        }

        var hasher = new PasswordHasherService();
        db.Users.Add(new User
        {
            Email = adminEmail,
            FullName = "System Administrator",
            PasswordHash = hasher.HashPassword(adminPassword),
            Role = UserRole.Admin,
            IsVerified = true
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedDemoDataAsync(AppDbContext db)
    {
        var hasher = new PasswordHasherService();

        var customer = new User
        {
            Email = "customer@example.com",
            FullName = "Demo Customer",
            PasswordHash = hasher.HashPassword("DemoPass123!"),
            Role = UserRole.Customer,
            IsVerified = true
        };
        db.Users.Add(customer);

        var clothing = await db.Categories.FirstAsync(c => c.Slug == "clothing");
        var food = await db.Categories.FirstAsync(c => c.Slug == "food-snacks");
        var grocery = await db.Categories.FirstAsync(c => c.Slug == "groceries");
        var electronics = await db.Categories.FirstAsync(c => c.Slug == "electronics");
        var pharma = await db.Categories.FirstAsync(c => c.Slug == "pharmacy");

        var shopOwners = new[]
        {
            new { Email = "shop1@example.com", Name = "ABC Fashion", Cat = clothing, Lat = 12.9348, Lng = 77.6109 },
            new { Email = "shop2@example.com", Name = "Fresh Mart", Cat = grocery, Lat = 12.9364, Lng = 77.6075 },
            new { Email = "shop3@example.com", Name = "QuickFix Electronics", Cat = electronics, Lat = 12.9312, Lng = 77.6142 },
            new { Email = "shop4@example.com", Name = "City Pharmacy", Cat = pharma, Lat = 12.9332, Lng = 77.6168 },
            new { Email = "shop5@example.com", Name = "Spice & Grill", Cat = food, Lat = 12.9381, Lng = 77.6100 }
        };

        foreach (var s in shopOwners)
        {
            var owner = new User
            {
                Email = s.Email,
                FullName = $"Owner of {s.Name}",
                PasswordHash = hasher.HashPassword("DemoPass123!"),
                Role = UserRole.ShopOwner,
                IsVerified = true
            };
            db.Users.Add(owner);

            var shop = new Shop
            {
                OwnerUser = owner,
                Name = s.Name,
                Description = $"Demo {s.Name} shop. Responds to nearby LIVE requests.",
                Phone = "080-1234-5678",
                Address = "Demo Road, Indiranagar",
                Latitude = s.Lat,
                Longitude = s.Lng,
                IsOpen = true,
                Status = ShopStatus.Verified
            };
            shop.ShopCategories.Add(new ShopCategory { Shop = shop, Category = s.Cat });
            db.Shops.Add(shop);
        }

        await db.SaveChangesAsync();
    }
}
