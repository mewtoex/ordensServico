namespace Os.Api.Application.Interfaces.Services;

public interface IReportsService
{
    Task<MonthlyReportResponse> Report(int year, int month);
}
