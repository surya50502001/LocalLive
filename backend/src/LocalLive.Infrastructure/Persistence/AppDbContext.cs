using LocalLive.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocalLive.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<ShopCategory> ShopCategories => Set<ShopCategory>();
    public DbSet<LiveRequest> LiveRequests => Set<LiveRequest>();
    public DbSet<ShopRequest> ShopRequests => Set<ShopRequest>();
    public DbSet<ShopResponse> ShopResponses => Set<ShopResponse>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<AdminAction> AdminActions => Set<AdminAction>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<FavoriteShop> FavoriteShops => Set<FavoriteShop>();
    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();
    public DbSet<ShopVerification> ShopVerifications => Set<ShopVerification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
