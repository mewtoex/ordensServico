using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Os.Api.Application;
using Os.Api.Application.Interfaces.Services;
using Os.Api.Domain;

namespace Os.Api.Controllers;

[ApiController, Route("api/customers"), Authorize]
public class CustomersController(ICustomersService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<CustomerResponse>>> List([FromQuery] string? search, int page = 1, int pageSize = 20) => Ok(await service.Customers(search, page, pageSize));
    [HttpPost, Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerResponse>> Create(CustomerRequest request) => StatusCode(201, await service.CreateCustomer(request));
    [HttpPut("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<CustomerResponse>> Update(Guid id, CustomerRequest request) => Ok(await service.EditCustomer(id, request));
    [HttpDelete("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteCustomer(id);
        return NoContent();
    }
}
