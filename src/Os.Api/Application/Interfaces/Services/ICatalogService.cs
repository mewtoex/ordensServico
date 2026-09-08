namespace Os.Api.Application.Interfaces.Services;

public interface ICatalogService
{
    Task<PagedResponse<CatalogResponse>> Catalog(int page = 1, int pageSize = 20);
    Task<CatalogResponse> CreateCatalog(CatalogRequest request);
    Task<CatalogResponse> EditCatalog(Guid id, CatalogRequest request);
    Task DeleteCatalog(Guid id);
}
