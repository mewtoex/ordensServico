using Os.Api.Application.Abstractions;
using Os.Api.Application.Interfaces.Services;

namespace Os.Api.Application.Services;

public class OrderPdfService(IOrdersService orders, IOrderPdfRenderer renderer) : IOrderPdfService
{
    public async Task<ExportFile> Export(Guid id)
    {
        var order = await orders.GetOrder(id);
        return new ExportFile(renderer.Render(order), "application/pdf", $"os-{id}.pdf");
    }
}
