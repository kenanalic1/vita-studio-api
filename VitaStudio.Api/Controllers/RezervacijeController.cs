using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VitaStudio.Api.Dtos;
using VitaStudio.Api.Services;

namespace VitaStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RezervacijeController : ControllerBase
{
    private readonly IRezervacijaService _svc;
    public RezervacijeController(IRezervacijaService svc) => _svc = svc;

    [HttpGet]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? datum) =>
        Ok(await _svc.GetAllAsync(status, datum));

    [HttpDelete("{id}/admin")]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> AdminOtkazi(int id)
    {
        try { await _svc.AdminOtkaziAsync(id); return NoContent(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("moje")]
    [Authorize(Roles = "clan")]
    public async Task<IActionResult> Moje()
    {
        var clanId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _svc.GetByClanAsync(clanId));
    }

    [HttpGet("termin/{terminId}")]
    [Authorize(Roles = "administrator,trener")]
    public async Task<IActionResult> GetByTermin(int terminId) =>
        Ok(await _svc.GetByTerminAsync(terminId));

    [HttpPost]
    [Authorize(Roles = "clan")]
    public async Task<IActionResult> Rezervisi([FromBody] RezervacijaCreateDto dto)
    {
        var clanId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try { return Ok(await _svc.RezervisiAsync(clanId, dto.TerminId)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Otkazi(int id)
    {
        var clanId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try { await _svc.OtkaziAsync(id, clanId); return NoContent(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}