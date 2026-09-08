using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Os.Api.Application;
using Os.Api.Application.Interfaces.Services;
using Os.Api.Domain;

namespace Os.Api.Controllers;

[ApiController, Route("api/reports"), Authorize(Roles = "Admin")]
public class ReportsController(IReportsService service) : ControllerBase
{
    [HttpGet("monthly")]
    public async Task<ActionResult<MonthlyReportResponse>> Monthly(int year, int month) => Ok(await service.Report(year, month));
}
