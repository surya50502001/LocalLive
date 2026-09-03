using LocalLive.Application.Features.Search;

namespace LocalLive.Application.Common.Interfaces;

public interface ISearchService
{
    Task<SearchResultDto> SearchAsync(string query, double? latitude = null, double? longitude = null, double radiusKm = 15);
}
