using Os.Api.Domain;

namespace Os.Api.Application.Interfaces.Repositories;

public interface IRefreshSessionRepository
{
    Task<RefreshSession?> GetByHashAsync(string tokenHash);
    void Add(RefreshSession session);
}
