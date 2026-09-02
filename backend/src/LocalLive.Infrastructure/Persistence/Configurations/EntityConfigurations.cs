using LocalLive.Domain.Common;
using LocalLive.Domain.Entities;
using LocalLive.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LocalLive.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.FullName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.PasswordHash).IsRequired();
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.BlockReason).HasMaxLength(500);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasMany(x => x.OwnedShops).WithOne(s => s.OwnerUser).HasForeignKey(s => s.OwnerUserId);
        builder.HasMany(x => x.Requests).WithOne(r => r.CustomerUser).HasForeignKey(r => r.CustomerUserId);
        builder.HasMany(x => x.RefreshTokens).WithOne(t => t.User).HasForeignKey(t => t.UserId);
        builder.HasMany(x => x.Notifications).WithOne(n => n.RecipientUser).HasForeignKey(n => n.RecipientUserId);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.Property(x => x.DeviceInfo).HasMaxLength(255);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.Property(x => x.Icon).HasMaxLength(80);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}

public class ShopConfiguration : IEntityTypeConfiguration<Shop>
{
    public void Configure(EntityTypeBuilder<Shop> builder)
    {
        builder.ToTable("shops");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Phone).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ImageUrl).HasMaxLength(500);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        var hoursConverter = new HoursOfOperationConverter();
        builder.Property(x => x.Hours).HasColumnType("jsonb").HasConversion(hoursConverter);

        builder.HasIndex(x => new { x.Latitude, x.Longitude });
        builder.HasIndex(x => x.OwnerUserId).IsUnique();
        builder.HasIndex(x => x.Status);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}

public class ShopCategoryConfiguration : IEntityTypeConfiguration<ShopCategory>
{
    public void Configure(EntityTypeBuilder<ShopCategory> builder)
    {
        builder.ToTable("shop_categories");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.ShopId, x.CategoryId }).IsUnique();
        builder.HasOne(x => x.Shop).WithMany(s => s.ShopCategories).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Category).WithMany(c => c.ShopCategories).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}

public class LiveRequestConfiguration : IEntityTypeConfiguration<LiveRequest>
{
    public void Configure(EntityTypeBuilder<LiveRequest> builder)
    {
        builder.ToTable("live_requests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CustomerUserId);
        builder.HasIndex(x => new { x.CustomerUserId, x.Status });
        builder.HasIndex(x => x.ExpiresAt);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}

public class ShopRequestConfiguration : IEntityTypeConfiguration<ShopRequest>
{
    public void Configure(EntityTypeBuilder<ShopRequest> builder)
    {
        builder.ToTable("shop_requests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(x => new { x.RequestId, x.ShopId }).IsUnique();
        builder.HasIndex(x => new { x.ShopId, x.Status });

        builder.HasOne(x => x.Request).WithMany(r => r.ShopRequests).HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Shop).WithMany(s => s.ShopRequests).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}

public class ShopResponseConfiguration : IEntityTypeConfiguration<ShopResponse>
{
    public void Configure(EntityTypeBuilder<ShopResponse> builder)
    {
        builder.ToTable("shop_responses");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Message).HasMaxLength(500);

        builder.HasIndex(x => new { x.RequestId, x.ShopId }).IsUnique();
        builder.HasOne(x => x.Request).WithMany(r => r.ShopResponses).HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Shop).WithMany(s => s.ShopResponses).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ShopRequest).WithOne(sr => sr.Response)
            .HasForeignKey<ShopResponse>(x => x.ShopRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb");
        builder.Property(x => x.LinkedEntity).HasMaxLength(30);

        builder.HasIndex(x => new { x.RecipientUserId, x.IsRead });
        builder.HasOne(x => x.RecipientUser).WithMany(u => u.Notifications).HasForeignKey(x => x.RecipientUserId);
    }
}

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("reports");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Details).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.ResolutionNote).HasMaxLength(1000);
        builder.Property(x => x.TargetType).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.TargetType, x.TargetId });
    }
}

public class AdminActionConfiguration : IEntityTypeConfiguration<AdminAction>
{
    public void Configure(EntityTypeBuilder<AdminAction> builder)
    {
        builder.ToTable("admin_actions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DetailJson).HasColumnType("jsonb");
        builder.Property(x => x.TargetType).HasConversion<string>().HasMaxLength(20);
    }
}
