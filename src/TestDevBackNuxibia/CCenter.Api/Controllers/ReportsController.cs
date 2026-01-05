using CCenter.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace CCenter.Api.Controllers;

[ApiController]
[Route("reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    [HttpGet("worked-hours.csv")]
    public async Task<IActionResult> WorkedHoursCsv(CancellationToken ct)
    {
        var stream = await _reports.BuildWorkedHoursCsvAsync(ct);
        return File(stream, "text/csv; charset=utf-8", "worked-hours.csv");
    }
}
