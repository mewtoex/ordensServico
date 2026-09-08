using Os.Api.Application.Interfaces.Repositories;
using Os.Api.Application.Interfaces.Services;
using Os.Api.Domain;

namespace Os.Api.Application.Services;

public class ReportsService(IReportRepository reports) : IReportsService
{
    public Task<MonthlyReportResponse> Report(int year, int month)
    {
        if (year < 2000 || year > 9998 || month < 1 || month > 12)
        {
            throw new BusinessException("Mês ou ano inválido.");
        }
        var start = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        return reports.GetMonthlyAsync(start, start.AddMonths(1));
    }
}
