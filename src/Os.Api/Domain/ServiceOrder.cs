namespace Os.Api.Domain;

public class ServiceOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId
    { get; set; }
    public Customer Customer { get; set; } = null!;
    public Guid TechnicianId
    { get; set; }
    public User Technician { get; set; } = null!;
    public string Description { get; set; } = "";
    public OrderStatus Status { get; private set; } = OrderStatus.Aberta;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAt
    { get; private set; }
    public byte[] Version { get; set; } = [];
    public List<OrderItem> Items { get; set; } = [];
    public List<AuditEntry> History { get; set; } = [];
    public decimal Total => Items.Sum(i => i.UnitPrice * i.Quantity);

    public void AddItem(CatalogItem catalogItem, int quantity, Guid actorId)
    {
        EnsureEditable();
        if (quantity is < 1 or > 10000)
        {
            throw new BusinessException("Quantidade deve estar entre 1 e 10000.");
        }

        Items.Add(new OrderItem
        {
            CatalogItemId = catalogItem.Id,
            Name = catalogItem.Name,
            Kind = catalogItem.Kind,
            UnitPrice = catalogItem.Price,
            Quantity = quantity
        });
        History.Add(new AuditEntry
        {
            ActorId = actorId,
            Action = "ItemAdicionado",
            Detail = $"{catalogItem.Name}: {quantity} x {catalogItem.Price}"
        });
    }

    public void RemoveItem(Guid itemId, Guid actorId)
    {
        EnsureEditable();
        var item = Items.SingleOrDefault(item => item.Id == itemId) ?? throw new KeyNotFoundException();
        Items.Remove(item);
        History.Add(new AuditEntry
        {
            ActorId = actorId,
            Action = "ItemRemovido",
            Detail = $"{item.Name}: {item.Quantity} x {item.UnitPrice}"
        });
    }

    public void EnsureEditable()
    {
        if (Status is OrderStatus.Concluida or OrderStatus.Cancelada)
            throw new BusinessException("OS encerrada não pode ser alterada.");
    }
    public void ChangeStatus(OrderStatus next, Guid actor)
    {
        EnsureEditable();
        var valid = next == OrderStatus.Cancelada || (Status, next) switch
        {
            (OrderStatus.Aberta, OrderStatus.EmAndamento) => true,
            (OrderStatus.EmAndamento, OrderStatus.AguardandoPeca or OrderStatus.Concluida) => true,
            (OrderStatus.AguardandoPeca, OrderStatus.EmAndamento) => true,
            _ => false
        };
        if (!valid)
            throw new BusinessException("Transição de status inválida.");
        History.Add(new AuditEntry { ActorId = actor, Action = "Status", Detail = $"{Status} -> {next}" });
        Status = next;
        if (next is OrderStatus.Concluida or OrderStatus.Cancelada)
            ClosedAt = DateTimeOffset.UtcNow;
    }
}
