using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Os.Api.Application;
using Os.Api.Application.Interfaces.Services;
using Os.Api.Domain;

namespace Os.Api.Controllers;

[ApiController, Route("api/orders/{id:guid}/items"), Authorize]
public class OrderItemsController(IOrdersService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Add(Guid id, ItemRequest request) => Ok(await service.AddItem(id, request));
    [HttpDelete("{itemId:guid}")]
    public async Task<IActionResult> Remove(Guid id, Guid itemId)
    {
        await service.RemoveItem(id, itemId);
        return NoContent();
    }
}
