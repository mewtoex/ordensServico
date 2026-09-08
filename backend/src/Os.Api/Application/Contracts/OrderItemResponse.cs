using Os.Api.Domain;

namespace Os.Api.Application;

public record OrderItemResponse(Guid Id, Guid CatalogItemId, string Name, ItemKind Kind, int Quantity, decimal UnitPrice, decimal Subtotal);
