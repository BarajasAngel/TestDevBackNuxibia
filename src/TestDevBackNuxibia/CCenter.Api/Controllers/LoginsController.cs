using CCenter.Services.Contracts;
using CCenter.Services.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CCenter.Api.Controllers;

[ApiController]
[Route("logins")]
public sealed class LoginsController : ControllerBase
{
    private readonly ILoginService _service;

    public LoginsController(ILoginService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _service.GetAllAsync(ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LoginCreateRequest req, CancellationToken ct)
    {
        var (ok, error, created) = await _service.CreateAsync(req, ct);
        if (!ok) return Conflict(new ProblemDetails { Title = "Validación", Detail = error });

        return CreatedAtAction(nameof(GetById), new { id = created!.LogId }, created);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] LoginUpdateRequest req, CancellationToken ct)
    {
        var (ok, error) = await _service.UpdateAsync(id, req, ct);
        if (!ok) return Conflict(new ProblemDetails { Title = "Validación", Detail = error });

        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var (ok, error) = await _service.DeleteAsync(id, ct);
        if (!ok) return NotFound(new ProblemDetails { Title = "NotFound", Detail = error });

        return NoContent();
    }
}
