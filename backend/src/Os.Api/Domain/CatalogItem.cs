namespace Os.Api.Domain;

public class CatalogItem
{
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public ItemKind Kind
    { get; set; }
    public decimal Price
    { get; set; }
}
