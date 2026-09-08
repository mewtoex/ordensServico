using Os.Api.Domain;

namespace Os.Api.Application;

public record OrderResponse(Guid Id, Guid CustomerId, string CustomerName, Guid TechnicianId, string Description,
    OrderStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? ClosedAt, decimal Total, IReadOnlyList<OrderItemResponse> Items)
{
    public static OrderResponse From(ServiceOrder order) => new(order.Id, order.CustomerId, order.Customer.Name,
        order.TechnicianId, order.Description, order.Status, order.CreatedAt, order.ClosedAt, order.Total,
        order.Items.Select(item => new OrderItemResponse(item.Id, item.CatalogItemId, item.Name, item.Kind,
            item.Quantity, item.UnitPrice, item.Quantity * item.UnitPrice)).ToList());
}
