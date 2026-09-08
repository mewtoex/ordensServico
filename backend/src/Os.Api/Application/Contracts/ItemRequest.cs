using System.ComponentModel.DataAnnotations;
using Os.Api.Domain;

namespace Os.Api.Application;

public record ItemRequest(Guid CatalogItemId, [Range(1, 10000)] int Quantity);
