using Microsoft.EntityFrameworkCore;
using Os.Api.Application;
using Os.Api.Application.Interfaces.Repositories;
using Os.Api.Domain;

namespace Os.Api.Infra.Repositories;

public class CatalogRepository(OsDb database) : ICatalogRepository
{
    public Task<CatalogItem?> GetByIdAsync(Guid id) => database.Catalog.SingleOrDefaultAsync(item => item.Id == id);
    public async Task<PagedResponse<CatalogItem>> ListAsync(int page, int pageSize)
    {
        var total = await database.Catalog.CountAsync();
        var items = await database.Catalog.AsNoTracking()
            .OrderBy(item => item.Name)
            .ThenBy(item => item.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return new PagedResponse<CatalogItem>(total, page, pageSize, items);
    }
    public void Add(CatalogItem item) => database.Catalog.Add(item);
    public void Remove(CatalogItem item) => database.Catalog.Remove(item);
}
