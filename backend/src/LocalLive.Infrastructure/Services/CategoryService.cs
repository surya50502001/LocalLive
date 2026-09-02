using LocalLive.Application.Common.Interfaces;
using LocalLive.Application.Features.Categories;
using LocalLive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocalLive.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;

    public CategoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CategoryDto>> GetActiveAsync()
        => await _db.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Icon = c.Icon,
                SortOrder = c.SortOrder
            })
            .AsNoTracking()
            .ToListAsync();
}
