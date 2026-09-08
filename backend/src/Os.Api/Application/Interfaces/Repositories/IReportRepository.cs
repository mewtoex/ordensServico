using Os.Api.Domain;

namespace Os.Api.Application.Interfaces.Repositories;

public interface IReportRepository
{
    Task<MonthlyReportResponse> GetMonthlyAsync(DateTimeOffset start, DateTimeOffset end);
}
