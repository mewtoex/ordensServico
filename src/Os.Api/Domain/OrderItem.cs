namespace Os.Api.Domain;

public class OrderItem
{
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServiceOrderId
    { get; set; }
    public Guid CatalogItemId
    { get; set; }
    public CatalogItem CatalogItem { get; set; } = null!;
    public string Name { get; set; } = "";
    public ItemKind Kind
    { get; set; }
    public int Quantity
    { get; set; }
    public decimal UnitPrice
    { get; set; }
}
