using Os.Api.Domain;

namespace Os.Api.Application.Interfaces.Repositories;

public interface ICatalogRepository
{
    Task<CatalogItem?> GetByIdAsync(Guid id);
    Task<PagedResponse<CatalogItem>> ListAsync(int page, int pageSize);
    void Add(CatalogItem item);
    void SoftDelete(CatalogItem item);
}
