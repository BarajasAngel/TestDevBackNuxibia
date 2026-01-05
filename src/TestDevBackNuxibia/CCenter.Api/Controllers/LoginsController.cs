using CCenter.Services;
using CCenter.Services.Contracts;
using CCenter.Services.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CCenter.Api.Controllers;

[ApiController]
[Route("logins")]
public sealed class LoginsController : ControllerBase
{
    private readonly ILoginService _service;

    public LoginsController(ILoginService service)
        => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _service.GetAllAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LoginCreateDto dto, CancellationToken ct)
    {
        var (ok, error, created) = await _service.CreateAsync(dto, ct);

        if (!ok)
            return BadRequest(new { error });
        
        return Created($"/logins/{created!.UserId}", created);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] LoginUpdateDto dto, CancellationToken ct)
    {
        var (ok, error, updated) = await _service.UpdateAsync(id, dto, ct);

        if (!ok)
            return NotFound(new { error });
        
        return Ok(updated);        
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var (ok, error) = await _service.DeleteAsync(id, ct);

        if (!ok)
            return NotFound(new { error });

        return NoContent();
    }

}
