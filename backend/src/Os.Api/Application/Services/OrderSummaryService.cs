using System.Globalization;
using System.Text;
using Os.Api.Application.Interfaces.Repositories;
using Os.Api.Application.Interfaces.Services;

namespace Os.Api.Application.Services;

public class OrderSummaryService(IOrdersService orders) : IOrderSummaryService
{
    public async Task<ExportFile> Export(Guid id)
    {
        var order = await orders.GetOrder(id);
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        var lines = order.Items.Select(item => $"{item.Name} — {item.Quantity} x {item.UnitPrice.ToString("C", culture)}");
        var content = $"ORDEM DE SERVIÇO {order.Id}\nCliente: {order.CustomerName}\nStatus: {order.Status}\nDescrição: {order.Description}\n{string.Join("\n", lines)}\nTotal: {order.Total.ToString("C", culture)}\n";
        return new ExportFile(Encoding.UTF8.GetBytes(content), "text/plain; charset=utf-8", $"os-{id}.txt");
    }
}
