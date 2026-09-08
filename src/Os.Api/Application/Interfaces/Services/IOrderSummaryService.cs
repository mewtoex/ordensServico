namespace Os.Api.Application.Interfaces.Services;

public interface IOrderSummaryService
{
    Task<ExportFile> Export(Guid id);
}
