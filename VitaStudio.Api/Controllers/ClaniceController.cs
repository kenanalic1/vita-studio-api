using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VitaStudio.Api.Data;
using VitaStudio.Api.Dtos;
using VitaStudio.Api.Models;
using VitaStudio.Api.Services;

namespace VitaStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClaniceController : ControllerBase
{
    private readonly VitaStudioDbContext  _db;
    private readonly INotifikacijaService _notif;
    public ClaniceController(VitaStudioDbContext db, INotifikacijaService notif)
    {
        _db    = db;
        _notif = notif;
    }

    [HttpGet("pending")]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> GetPending() =>
        Ok(await _db.Clanovi
            .Where(c => c.Status == "pending")
            .OrderBy(c => c.DatumKreiranja)
            .Select(c => new { c.Id, c.Ime, c.Prezime, c.Email, c.Telefon, c.DatumKreiranja })
            .ToListAsync());

    [HttpPut("{id}/odobri")]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> Odobri(int id)
    {
        var c = await _db.Clanovi.FindAsync(id);
        if (c is null) return NotFound();
        c.Status = "aktivan";
        await _db.SaveChangesAsync();
        await _notif.KreirajAsync(c.Id, "clan",
            "Dobrodošli u Vita Studio! Vaš nalog je odobren. Možete se prijaviti.", "uspeh");
        return Ok(MapDto(c));
    }

    [HttpDelete("{id}/odbij")]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> Odbij(int id)
    {
        var c = await _db.Clanovi.FindAsync(id);
        if (c is null) return NotFound();
        _db.Clanovi.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? search)
    {
        var q = _db.Clanovi.Include(c => c.Clanarine).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(c => c.Ime.Contains(search) || c.Prezime.Contains(search) || c.Email.Contains(search));
        var lista = await q.OrderBy(c => c.Prezime).ToListAsync();
        return Ok(lista.Select(c => MapDto(c)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var clan = await _db.Clanovi
            .Include(c => c.Clanarine)
            .Include(c => c.Rezervacije).ThenInclude(r => r.Termin).ThenInclude(t => t.Aktivnost)
            .Include(c => c.Rezervacije).ThenInclude(r => r.Termin).ThenInclude(t => t.Trener)
            .Include(c => c.Rezervacije).ThenInclude(r => r.Termin).ThenInclude(t => t.Sala)
            .Include(c => c.EvidencijeDolazaka).ThenInclude(e => e.Termin).ThenInclude(t => t.Aktivnost)
            .Include(c => c.EvidencijeDolazaka).ThenInclude(e => e.Termin).ThenInclude(t => t.Trener)
            .Include(c => c.ListeCekanja)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (clan is null) return NotFound();

        var aktivnaClanarina = clan.Clanarine
            .Where(cl => cl.Status == "aktivna")
            .OrderByDescending(cl => cl.DatumPocetka)
            .FirstOrDefault();

        var predstojeciTermini = clan.Rezervacije
            .Where(r => r.Status == "aktivna" && r.Termin.DatumVreme >= DateTime.UtcNow)
            .OrderBy(r => r.Termin.DatumVreme).Take(5)
            .Select(r => new RezervacijaDto(
                r.Id, r.DatumRezervacije, r.Status,
                r.ClanId, $"{clan.Ime} {clan.Prezime}",
                r.TerminId, r.Termin.DatumVreme, r.Termin.Aktivnost.Naziv,
                r.Termin.Sala.Naziv, $"{r.Termin.Trener.Ime} {r.Termin.Trener.Prezime}"
            )).ToList();

        var istorijaDolazaka = clan.EvidencijeDolazaka
            .OrderByDescending(e => e.DatumDolaska).Take(10)
            .Select(e => new EvidencijaDolaskaDto(
                e.Id, e.DatumDolaska,
                clan.Id, $"{clan.Ime} {clan.Prezime}",
                e.TerminId, e.Termin.Aktivnost.Naziv,
                $"{e.Termin.Trener.Ime} {e.Termin.Trener.Prezime}", "stigla"
            )).ToList();

        var profil = new ClanProfilDto(
            clan.Id, clan.Ime, clan.Prezime, clan.Email,
            clan.Telefon, clan.DatumRodjenja, clan.DatumKreiranja,
            aktivnaClanarina is null ? null : new ClanarinaDto(
                aktivnaClanarina.Id, aktivnaClanarina.TipPaketa, aktivnaClanarina.NazivPaketa,
                aktivnaClanarina.DatumPocetka, aktivnaClanarina.DatumIsteka,
                aktivnaClanarina.Cena, aktivnaClanarina.Status, clan.Id, null),
            clan.EvidencijeDolazaka.Count,
            clan.Rezervacije.Count(r => r.Status == "otkazana"),
            clan.ListeCekanja.Count,
            clan.EvidencijeDolazaka.Count(e => e.DatumDolaska >= DateTime.Today.AddDays(-30)),
            istorijaDolazaka, predstojeciTermini
        );
        return Ok(profil);
    }

    [HttpPost]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> Create([FromBody] ClanCreateDto dto)
    {
        if (await _db.Clanovi.AnyAsync(c => c.Email == dto.Email))
            return BadRequest(new { message = "Email vec postoji." });
        var clan = new Clan
        {
            Ime = dto.Ime,
            Prezime = dto.Prezime,
            Email = dto.Email,
            Lozinka = BCrypt.Net.BCrypt.HashPassword(dto.Lozinka, 11),
            Telefon = dto.Telefon,
            DatumRodjenja = dto.DatumRodjenja
        };
        _db.Clanovi.Add(clan);
        await _db.SaveChangesAsync();
        return Ok(MapDto(clan));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "administrator,clan")]
    public async Task<IActionResult> Update(int id, [FromBody] ClanUpdateDto dto)
    {
        var c = await _db.Clanovi.FindAsync(id);
        if (c is null) return NotFound();
        c.Ime = dto.Ime; c.Prezime = dto.Prezime; c.Email = dto.Email;
        c.Telefon = dto.Telefon; c.DatumRodjenja = dto.DatumRodjenja;
        await _db.SaveChangesAsync();
        return Ok(MapDto(c));
    }

    [HttpPut("{id}/lozinka")]
    [Authorize(Roles = "clan")]
    public async Task<IActionResult> PromijeniLozinku(int id, [FromBody] LozinkaDto dto)
    {
        var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (requesterId != id) return Forbid();
        var c = await _db.Clanovi.FindAsync(id);
        if (c is null) return NotFound();
        if (!BCrypt.Net.BCrypt.Verify(dto.StaraLozinka, c.Lozinka))
            return BadRequest(new { message = "Pogrešna trenutna lozinka." });
        c.Lozinka = BCrypt.Net.BCrypt.HashPassword(dto.NovaLozinka, 11);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Clanovi.FindAsync(id);
        if (c is null) return NotFound();
        _db.Clanovi.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static ClanDto MapDto(Clan c) => new(
        c.Id, c.Ime, c.Prezime, c.Email,
        c.Telefon, c.DatumRodjenja, c.DatumKreiranja, c.Status
    );
}