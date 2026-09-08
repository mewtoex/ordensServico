using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Os.Api.Application.Interfaces.Services;

namespace Os.Api.Controllers;

[ApiController, Route("api/orders/{id:guid}/pdf"), Authorize]
public class OrderPdfController(IOrderPdfService service) : ControllerBase
{
    [HttpGet, Produces("application/pdf")]
    public async Task<IActionResult> Download(Guid id)
    {
        var file = await service.Export(id);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
