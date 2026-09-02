using System.ComponentModel.DataAnnotations;
using LocalLive.Domain.Common;

namespace LocalLive.Application.Features.Shops;

public record CreateShopRequest
{
    [Required, MinLength(2), MaxLength(120)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; init; }

    [Required, Phone, MaxLength(30)]
    public string Phone { get; init; } = string.Empty;

    [Required, MinLength(5), MaxLength(300)]
    public string Address { get; init; } = string.Empty;

    [Range(-90, 90)]
    public double Latitude { get; init; }

    [Range(-180, 180)]
    public double Longitude { get; init; }

    [MaxLength(500)]
    public string? ImageUrl { get; init; }

    [MinLength(1)]
    public List<Guid> CategoryIds { get; init; } = new();

    public HoursOfOperation? Hours { get; init; }
}

public record UpdateShopRequest
{
    [Required, MinLength(2), MaxLength(120)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; init; }

    [Required, Phone, MaxLength(30)]
    public string Phone { get; init; } = string.Empty;

    [Required, MinLength(5), MaxLength(300)]
    public string Address { get; init; } = string.Empty;

    [Range(-90, 90)]
    public double Latitude { get; init; }

    [Range(-180, 180)]
    public double Longitude { get; init; }

    [MaxLength(500)]
    public string? ImageUrl { get; init; }

    [MinLength(1)]
    public List<Guid> CategoryIds { get; init; } = new();

    public HoursOfOperation? Hours { get; init; }
}

public record UpdateShopStatusRequest
{
    [Required]
    public bool IsOpen { get; init; }
}

public record ShopDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Phone { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string? ImageUrl { get; init; }
    public bool IsOpen { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsVerified { get; init; }
    public Guid OwnerUserId { get; init; }
    public List<CategoryDtoRef> Categories { get; init; } = new();
    public double? DistanceM { get; init; }
    public string? NavigationUrl { get; init; }
}

public record CategoryDtoRef
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
}

public record NearbyShopQuery
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double RadiusKm { get; init; } = 10;
    public Guid? CategoryId { get; init; }
}
