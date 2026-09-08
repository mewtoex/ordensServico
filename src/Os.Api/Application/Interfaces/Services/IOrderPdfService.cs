namespace Os.Api.Application.Interfaces.Services;

public interface IOrderPdfService
{
    Task<ExportFile> Export(Guid id);
}
