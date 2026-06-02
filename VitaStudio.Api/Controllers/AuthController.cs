using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VitaStudio.Api.Data;
using VitaStudio.Api.Dtos;
using VitaStudio.Api.Services;

namespace VitaStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var result = await _auth.LoginAsync(dto);
            if (result is null) return Unauthorized(new { message = "Pogrešan email ili lozinka." });
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message == "pending")
        {
            return Unauthorized(new { message = "Nalog čeka odobrenje admina. Kontaktiraj recepciju.", code = "pending" });
        }
        catch (InvalidOperationException ex) when (ex.Message == "odbijen")
        {
            return Unauthorized(new { message = "Nalog je odbijen. Kontaktiraj recepciju za više informacija.", code = "odbijen" });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterClanDto dto)
    {
        try
        {
            await _auth.RegisterClanAsync(dto);
            return Ok(new { message = "Nalog uspješno kreiran. Čeka odobrenje admina." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Pokreni jednom da hashiras sve lozinke u bazi</summary>
    [HttpPost("hash-lozinke")]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> HashLozinke([FromServices] VitaStudioDbContext db)
    {
        var admini = await db.Administratori.ToListAsync();
        foreach (var a in admini)
            if (!a.Lozinka.StartsWith("$2"))
                a.Lozinka = BCrypt.Net.BCrypt.HashPassword(a.Lozinka, 11);

        var treneri = await db.Treneri.ToListAsync();
        foreach (var t in treneri)
            if (!t.Lozinka.StartsWith("$2"))
                t.Lozinka = BCrypt.Net.BCrypt.HashPassword(t.Lozinka, 11);

        var clanovi = await db.Clanovi.ToListAsync();
        foreach (var c in clanovi)
            if (!c.Lozinka.StartsWith("$2"))
                c.Lozinka = BCrypt.Net.BCrypt.HashPassword(c.Lozinka, 11);

        await db.SaveChangesAsync();
        return Ok(new { message = "Sve lozinke su hashirane." });
    }
}
