using Microsoft.EntityFrameworkCore;
using Os.Api.Application.Interfaces.Repositories;
using Os.Api.Domain;

namespace Os.Api.Infra.Repositories;

public class RefreshSessionRepository(OsDb database) : IRefreshSessionRepository
{
    public Task<RefreshSession?> GetByHashAsync(string tokenHash) => database.RefreshSessions
        .Include(session => session.User).SingleOrDefaultAsync(session => session.TokenHash == tokenHash);
    public void Add(RefreshSession session) => database.RefreshSessions.Add(session);
}
