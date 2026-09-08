namespace Os.Api.Application.Abstractions;

public interface IOrderPdfRenderer
{
    byte[] Render(OrderResponse order);
}
