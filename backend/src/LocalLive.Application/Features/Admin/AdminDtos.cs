using System.ComponentModel.DataAnnotations;
using LocalLive.Domain.Enums;

namespace LocalLive.Application.Features.Admin;

public record AdminStatsDto
{
    public int TotalUsers { get; init; }
    public int TotalCustomers { get; init; }
    public int TotalShopOwners { get; init; }
    public int TotalShops { get; init; }
    public int VerifiedShops { get; init; }
    public int PendingShops { get; init; }
    public int DisabledShops { get; init; }
    public int ActiveShopsNow { get; init; }
    public int TotalRequests { get; init; }
    public int ActiveRequestsNow { get; init; }
    public int FulfilledRequests { get; init; }
    public int CancelledRequests { get; init; }
    public int ExpiredRequests { get; init; }
    public int TotalResponses { get; init; }
    public double AvgDistanceToRespondingShopM { get; init; }
    public int OpenReports { get; init; }
    public List<CategoryStatDto> RequestsByCategory { get; init; } = new();
    public List<DailyStatDto> RequestsLast7Days { get; init; } = new();
}

public record CategoryStatDto
{
    public string CategoryName { get; init; } = string.Empty;
    public int Count { get; init; }
}

public record DailyStatDto
{
    public string Day { get; init; } = string.Empty;
    public int Requests { get; init; }
    public int Responses { get; init; }
    public int Fulfilled { get; init; }
}

public record AdminShopDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Phone { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public bool IsOpen { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid OwnerUserId { get; init; }
    public string OwnerName { get; init; } = string.Empty;
    public string OwnerEmail { get; init; } = string.Empty;
    public List<string> Categories { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}

public record AdminUserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string Role { get; init; } = string.Empty;
    public bool IsBlocked { get; init; }
    public bool IsVerified { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record AdminRequestDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public int NotifiedShops { get; init; }
    public int ResponseCount { get; init; }
}

public record AdminReportDto
{
    public Guid Id { get; init; }
    public Guid ReporterUserId { get; init; }
    public string TargetType { get; init; } = string.Empty;
    public Guid TargetId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record AdminCategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public record UpsertCategoryRequest
{
    [Required, MinLength(2), MaxLength(80)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(80)]
    public string? Slug { get; init; }

    [MaxLength(80)]
    public string? Icon { get; init; }

    public int SortOrder { get; init; }
    public bool IsActive { get; init; } = true;
}

public record BlockUserRequest
{
    [MaxLength(500)]
    public string? Reason { get; init; }
}

public record PaginatedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
