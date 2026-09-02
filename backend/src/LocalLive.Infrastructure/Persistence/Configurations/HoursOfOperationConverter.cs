using System.Text.Json;
using LocalLive.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LocalLive.Infrastructure.Persistence.Configurations;

/// <summary>
/// Serializes the HoursOfOperation value object to a JSONB string column.
/// Uses explicit static methods to avoid the expression-tree/optional-argument
/// limitation of the lambda-based ValueConverter constructor.
/// </summary>
public sealed class HoursOfOperationConverter : ValueConverter<HoursOfOperation?, string>
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public HoursOfOperationConverter()
        : base(
            v => Serialize(v),
            v => Deserialize(v),
            new ConverterMappingHints(size: null))
    { }

    private static string Serialize(HoursOfOperation? v)
        => v is null ? "null" : JsonSerializer.Serialize(v, Options);

    private static HoursOfOperation? Deserialize(string v)
        => string.IsNullOrWhiteSpace(v) || v == "null"
            ? null
            : JsonSerializer.Deserialize<HoursOfOperation>(v, Options);
}
