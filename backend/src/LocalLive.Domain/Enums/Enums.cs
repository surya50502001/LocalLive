namespace LocalLive.Domain.Enums;

public enum UserRole
{
    Customer = 0,
    ShopOwner = 1,
    Admin = 2
}

public enum ShopStatus
{
    Pending = 0,
    Verified = 1,
    Disabled = 2
}

public enum RequestStatus
{
    Active = 0,
    Fulfilled = 1,
    Cancelled = 2,
    Expired = 3
}

public enum ShopRequestStatus
{
    Notified = 0,
    Responded = 1,
    Expired = 2,
    Fulfilled = 3,
    Cancelled = 4
}

public enum NotificationType
{
    ShopAvailable = 0,
    RequestStatusChanged = 1,
    NewRequest = 2,
    RequestCancelledOrExpired = 3,
    System = 4
}

public enum ReportTargetType
{
    Shop = 0,
    Request = 1
}

public enum ReportStatus
{
    Open = 0,
    Resolved = 1,
    Dismissed = 2
}

public enum AdminActionTarget
{
    Shop = 0,
    User = 1,
    Request = 2,
    Report = 3,
    Category = 4
}
