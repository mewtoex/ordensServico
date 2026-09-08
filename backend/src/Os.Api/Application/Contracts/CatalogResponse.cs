using Os.Api.Domain;

namespace Os.Api.Application;

public record CatalogResponse(Guid Id, string Name, ItemKind Kind, decimal Price)
{
    public static CatalogResponse From(CatalogItem item) => new(item.Id, item.Name, item.Kind, item.Price);
}
