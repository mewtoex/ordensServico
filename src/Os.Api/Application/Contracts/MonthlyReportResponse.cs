using Os.Api.Domain;

namespace Os.Api.Application;

public record MonthlyReportResponse(int Year, int Month, int Created, int Completed, decimal Revenue);
