using Os.Api.Application.Abstractions;
using Os.Api.Application.Interfaces.Repositories;
using Os.Api.Application.Interfaces.Services;
using Os.Api.Domain;

namespace Os.Api.Application.Services;

public class OrdersService(
    IOrderRepository orders,
    ICustomerRepository customers,
    IUserRepository users,
    ICatalogRepository catalog,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IOrdersService
{
    private Guid? TechnicianScope => currentUser.IsAdmin ? null : currentUser.Id;

    public async Task<PagedResponse<OrderResponse>> Orders(OrderFilter filter)
    {
        Pagination.Validate(filter.Page, filter.PageSize);
        if (filter.From > filter.To)
        {
            throw new BusinessException("Período inválido.");
        }
        if (filter.Status.HasValue && !Enum.IsDefined(filter.Status.Value))
        {
            throw new BusinessException("Status inválido.");
        }
        var result = await orders.ListAsync(filter, TechnicianScope);
        return new PagedResponse<OrderResponse>(result.Total, result.Page, result.PageSize,
            result.Data.Select(OrderResponse.From).ToList());
    }

    public async Task<OrderResponse> GetOrder(Guid id) => OrderResponse.From(await LoadAsync(id));

    public async Task<OrderResponse> CreateOrder(OrderRequest request)
    {
        var customer = await customers.GetByIdAsync(request.CustomerId)
            ?? throw new BusinessException("Cliente inválido.");
        if (!await users.IsActiveTechnicianAsync(request.TechnicianId))
        {
            throw new BusinessException("Técnico inválido.");
        }
        var order = new ServiceOrder
        {
            CustomerId = customer.Id,
            Customer = customer,
            TechnicianId = request.TechnicianId,
            Description = request.Description.Trim()
        };
        order.History.Add(new AuditEntry { ActorId = currentUser.Id, Action = "Criacao", Detail = "OS aberta" });
        orders.Add(order);
        await unitOfWork.SaveChangesAsync();
        return OrderResponse.From(order);
    }

    public async Task<OrderResponse> Status(Guid id, StatusRequest request)
    {
        var order = await LoadAsync(id);
        order.ChangeStatus(request.Status, currentUser.Id);
        await unitOfWork.SaveChangesAsync();
        return OrderResponse.From(order);
    }

    public async Task<OrderResponse> AddItem(Guid id, ItemRequest request)
    {
        var order = await LoadAsync(id);
        order.EnsureEditable();
        var catalogItem = await catalog.GetByIdAsync(request.CatalogItemId) ?? throw new KeyNotFoundException();
        order.AddItem(catalogItem, request.Quantity, currentUser.Id);
        orders.MarkChanged(order);
        await unitOfWork.SaveChangesAsync();
        return OrderResponse.From(order);
    }

    public async Task RemoveItem(Guid id, Guid itemId)
    {
        var order = await LoadAsync(id);
        order.RemoveItem(itemId, currentUser.Id);
        orders.MarkChanged(order);
        await unitOfWork.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AuditResponse>> History(Guid id)
    {
        await LoadAsync(id);
        return await orders.GetHistoryAsync(id);
    }

    private async Task<ServiceOrder> LoadAsync(Guid id) =>
        await orders.GetByIdAsync(id, TechnicianScope) ?? throw new KeyNotFoundException();
}
