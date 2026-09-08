using Os.Api.Application.Abstractions;
using Os.Api.Application.Interfaces.Repositories;
using Os.Api.Application.Interfaces.Services;
using Os.Api.Domain;

namespace Os.Api.Application.Services;

public class CatalogService(ICatalogRepository catalog, IUnitOfWork unitOfWork) : ICatalogService
{

    public async Task<PagedResponse<CatalogResponse>> Catalog(int page = 1, int pageSize = 20)
    {
        Pagination.Validate(page, pageSize);
        var result = await catalog.ListAsync(page, pageSize);
        return new PagedResponse<CatalogResponse>(result.Total, result.Page, result.PageSize,
            result.Data.Select(CatalogResponse.From).ToList());
    }

    public async Task<CatalogResponse> CreateCatalog(CatalogRequest request)
    {
        var catalogItem = new CatalogItem { Name = request.Name.Trim(), Kind = request.Kind, Price = decimal.Round(request.Price, 2) };
        catalog.Add(catalogItem);
        await unitOfWork.SaveChangesAsync();
        return CatalogResponse.From(catalogItem);
    }

    public async Task<CatalogResponse> EditCatalog(Guid id, CatalogRequest request)
    {
        var catalogItem = await catalog.GetByIdAsync(id) ?? throw new KeyNotFoundException();
        catalogItem.Name = request.Name.Trim();
        catalogItem.Kind = request.Kind;
        catalogItem.Price = decimal.Round(request.Price, 2);
        await unitOfWork.SaveChangesAsync();
        return CatalogResponse.From(catalogItem);
    }

    public async Task DeleteCatalog(Guid id)
    {
        catalog.SoftDelete(await catalog.GetByIdAsync(id) ?? throw new KeyNotFoundException());
        await unitOfWork.SaveChangesAsync();
    }
}
