using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VitaStudio.Api.Dtos;
using VitaStudio.Api.Services;

namespace VitaStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TerminiController : ControllerBase
{
    private readonly ITerminService _svc;
    public TerminiController(ITerminService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetByDatum([FromQuery] DateTime datum) =>
        Ok(await _svc.GetByDatumAsync(datum));

    [HttpGet("nedelja")]
    public async Task<IActionResult> GetByNedelja([FromQuery] DateTime ponedeljak) =>
        Ok(await _svc.GetByNedeljaAsync(ponedeljak));

    [HttpGet("trener/{trenerId}/nedelja")]
    public async Task<IActionResult> GetByTrener(int trenerId, [FromQuery] DateTime ponedeljak) =>
        Ok(await _svc.GetByTrenerAsync(trenerId, ponedeljak));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var t = await _svc.GetByIdAsync(id);
        return t is null ? NotFound() : Ok(t);
    }

    [HttpPost]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> Create([FromBody] TerminCreateDto dto)
    {
        try { return Ok(await _svc.CreateAsync(dto)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> Update(int id, [FromBody] TerminCreateDto dto)
    {
        try { return Ok(await _svc.UpdateAsync(id, dto)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> Delete(int id)
    {
        try { await _svc.DeleteAsync(id); return NoContent(); }
        catch (InvalidOperationException ex) { return NotFound(new { message = ex.Message }); }
    }
}