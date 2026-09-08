using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Os.Api.Application;
using Os.Api.Application.Interfaces.Services;
using Os.Api.Domain;

namespace Os.Api.Controllers;

[ApiController, Route("api/orders/{id:guid}/summary"), Authorize]
public class OrderSummaryController(IOrderSummaryService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Download(Guid id)
    {
        var file = await service.Export(id);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
