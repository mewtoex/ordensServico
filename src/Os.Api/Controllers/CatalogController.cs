using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Os.Api.Application;
using Os.Api.Application.Interfaces.Services;
using Os.Api.Domain;

namespace Os.Api.Controllers;

[ApiController, Route("api/catalog"), Authorize]
public class CatalogController(ICatalogService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<CatalogResponse>>> List(int page = 1, int pageSize = 20) => Ok(await service.Catalog(page, pageSize));
    [HttpPost, Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<CatalogResponse>> Create(CatalogRequest request) => StatusCode(201, await service.CreateCatalog(request));
    [HttpPut("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<CatalogResponse>> Update(Guid id, CatalogRequest request) => Ok(await service.EditCatalog(id, request));
    [HttpDelete("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteCatalog(id);
        return NoContent();
    }
}
