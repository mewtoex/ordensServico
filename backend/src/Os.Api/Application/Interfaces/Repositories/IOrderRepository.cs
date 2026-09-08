using Os.Api.Domain;

namespace Os.Api.Application.Interfaces.Repositories;

public interface IOrderRepository
{
    Task<ServiceOrder?> GetByIdAsync(Guid id, Guid? technicianId);
    Task<PagedResponse<ServiceOrder>> ListAsync(OrderFilter filter, Guid? technicianId);
    Task<IReadOnlyList<AuditResponse>> GetHistoryAsync(Guid id);
    void Add(ServiceOrder order);
    void MarkChanged(ServiceOrder order);
}
