namespace Os.Api.Application.Interfaces.Services;

public interface IOrdersService
{
    Task<PagedResponse<OrderResponse>> Orders(OrderFilter filter);
    Task<OrderResponse> GetOrder(Guid id);
    Task<OrderResponse> CreateOrder(OrderRequest request);
    Task<OrderResponse> Status(Guid id, StatusRequest request);
    Task<OrderResponse> AddItem(Guid id, ItemRequest request);
    Task RemoveItem(Guid id, Guid itemId);
    Task<IReadOnlyList<AuditResponse>> History(Guid id);
}
