using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Os.Api.Application;
using Os.Api.Application.Interfaces.Services;
using Os.Api.Domain;

namespace Os.Api.Controllers;

[ApiController, Route("api/orders"), Authorize]
public class OrdersController(IOrdersService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<OrderResponse>>> List([FromQuery] OrderFilter filter) => Ok(await service.Orders(filter));
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> Get(Guid id) => Ok(await service.GetOrder(id));
    [HttpPost, Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<OrderResponse>> Create(OrderRequest request)
    {
        var order = await service.CreateOrder(request);
        return CreatedAtAction(nameof(Get), new
        {
            id = order.Id
        }, order);
    }
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<OrderResponse>> ChangeStatus(Guid id, StatusRequest request) => Ok(await service.Status(id, request));
}
