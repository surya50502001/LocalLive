using System.ComponentModel.DataAnnotations;

namespace LocalLive.Application.Features.Requests;

public record CreateRequestRequest
{
    [Required, MinLength(2), MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; init; }

    [Required]
    public Guid CategoryId { get; init; }

    [Range(-90, 90)]
    public double Latitude { get; init; }

    [Range(-180, 180)]
    public double Longitude { get; init; }

    [Range(5, 120)]
    public int? TtlMinutes { get; init; }
}

public record RequestDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public int NotifiedShopsCount { get; init; }
    public List<ShopAvailableDto> AvailableShops { get; init; } = new();
    public double? DistanceM { get; init; }
}

public record ShopAvailableDto
{
    public Guid ShopId { get; init; }
    public string ShopName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Address { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public double? DistanceM { get; init; }
    public bool IsVerified { get; init; }
    public string? NavigationUrl { get; init; }
    public string? Message { get; init; }
    public DateTime RespondedAt { get; init; }
}

public record AvailableRequest
{
    [MaxLength(500)]
    public string? Message { get; init; }
}
