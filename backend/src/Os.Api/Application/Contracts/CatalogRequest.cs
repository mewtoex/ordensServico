using System.ComponentModel.DataAnnotations;
using Os.Api.Domain;

namespace Os.Api.Application;

public record CatalogRequest([Required, StringLength(160)] string Name, [EnumDataType(typeof(ItemKind))] ItemKind Kind, [Range(typeof(decimal), "0.01", "99999999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)] decimal Price);
