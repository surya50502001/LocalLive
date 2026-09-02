namespace LocalLive.Domain.Common;

public record GeoPoint(double Latitude, double Longitude)
{
    public static readonly GeoPoint Empty = new(0, 0);

    public bool IsValid =>
        Latitude is >= -90 and <= 90
        && Longitude is >= -180 and <= 180
        && !(Latitude == 0 && Longitude == 0);
}

public record HoursOfOperationEntry(DayOfWeek Day, string Open, string Close, bool ClosedAllDay = false);

public record HoursOfOperation
{
    public List<HoursOfOperationEntry> Entries { get; init; } = new();
}
