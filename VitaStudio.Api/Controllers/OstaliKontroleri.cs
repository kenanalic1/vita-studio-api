using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VitaStudio.Api.Data;
using VitaStudio.Api.Dtos;
using VitaStudio.Api.Models;
using VitaStudio.Api.Services;

namespace VitaStudio.Api.Controllers;

[ApiController]
[Route("api/treneri")]
[Authorize]
public class TreneriController : ControllerBase
{
    private readonly VitaStudioDbContext _db;
    public TreneriController(VitaStudioDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Treneri.OrderBy(t => t.Prezime)
            .Select(t => new TrenerDto(t.Id, t.Ime, t.Prezime, t.Email, t.Telefon, t.Specijalizacija))
            .ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var t = await _db.Treneri.FindAsync(id);
        return t is null ? NotFound()
            : Ok(new TrenerDto(t.Id, t.Ime, t.Prezime, t.Email, t.Telefon, t.Specijalizacija));
    }

    [HttpPost]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> Create([FromBody] TrenerCreateDto dto)
    {
        if (await _db.Treneri.AnyAsync(t => t.Email == dto.Email))
            return BadRequest(new { message = "Trener sa tim emailom već postoji." });

        var t = new Trener
        {
            Ime = dto.Ime,
            Prezime = dto.Prezime,
            Email = dto.Email,
            Lozinka = BCrypt.Net.BCrypt.HashPassword(dto.Lozinka, 11),
            Telefon = dto.Telefon,
            Specijalizacija = dto.Specijalizacija
        };
        _db.Treneri.Add(t); await _db.SaveChangesAsync();
        return Ok(new TrenerDto(t.Id, t.Ime, t.Prezime, t.Email, t.Telefon, t.Specijalizacija));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> Update(int id, [FromBody] TrenerCreateDto dto)
    {
        var t = await _db.Treneri.FindAsync(id);
        if (t is null) return NotFound();
        t.Ime = dto.Ime; t.Prezime = dto.Prezime; t.Email = dto.Email;
        t.Telefon = dto.Telefon; t.Specijalizacija = dto.Specijalizacija;
        if (!string.IsNullOrWhiteSpace(dto.Lozinka))
            t.Lozinka = BCrypt.Net.BCrypt.HashPassword(dto.Lozinka, 11);
        await _db.SaveChangesAsync();
        return Ok(new TrenerDto(t.Id, t.Ime, t.Prezime, t.Email, t.Telefon, t.Specijalizacija));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> Delete(int id)
    {
        var t = await _db.Treneri.FindAsync(id);
        if (t is null) return NotFound();
        if (await _db.Termini.AnyAsync(x => x.TrenerId == id))
            return BadRequest(new { message = "Nije moguće obrisati trenera koji ima zakazane termine." });
        _db.Treneri.Remove(t); await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/ucesnice")]
    [Authorize(Roles = "administrator,trener")]
    public async Task<IActionResult> GetUcesnice(int id)
    {
        var rezervacije = await _db.Rezervacije
            .Where(r => r.Termin.TrenerId == id && r.Status == "aktivna")
            .Include(r => r.Clan)
            .Include(r => r.Termin).ThenInclude(t => t.Aktivnost)
            .ToListAsync();

        var ucesnice = rezervacije
            .GroupBy(r => r.ClanId)
            .Select(g => new
            {
                clanId       = g.Key,
                imePrezime   = $"{g.First().Clan.Ime} {g.First().Clan.Prezime}",
                email        = g.First().Clan.Email,
                brojSesija   = g.Count(),
                aktivnosti   = g.Select(r => r.Termin.Aktivnost.Naziv).Distinct().OrderBy(a => a).ToList(),
                poslednjiTermin = g.Max(r => r.Termin.DatumVreme)
            })
            .OrderByDescending(u => u.brojSesija)
            .ToList();

        return Ok(ucesnice);
    }
}

[ApiController]
[Route("api/aktivnosti")]
[Authorize]
public class AktivnostiController : ControllerBase
{
    private readonly VitaStudioDbContext _db;
    public AktivnostiController(VitaStudioDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var aktivnosti = await _db.Aktivnosti.ToListAsync();
        var weekEnd = DateTime.Today.AddDays(7);
        var termini = await _db.Termini
            .Include(t => t.Trener)
            .Include(t => t.Rezervacije)
            .Where(t => t.DatumVreme >= DateTime.Today && t.DatumVreme < weekEnd)
            .ToListAsync();

        return Ok(aktivnosti.Select(a =>
        {
            var terminiAkt = termini.Where(t => t.AktivnostId == a.Id).ToList();
            var treneri = terminiAkt
                .Select(t => $"{t.Trener.Ime} {t.Trener.Prezime}")
                .Distinct().ToList();
            int ukupnoMesta = terminiAkt.Sum(t => t.MaxKapacitet);
            int ukupnoRez   = terminiAkt.Sum(t => t.Rezervacije.Count(r => r.Status == "aktivna"));
            int popunjenost = ukupnoMesta > 0 ? (int)Math.Round((double)ukupnoRez / ukupnoMesta * 100) : 0;
            return new { a.Id, a.Naziv, a.Opis, a.TipAktivnosti, Treneri = treneri, Popunjenost = popunjenost };
        }));
    }

    [HttpPost]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> Create([FromBody] AktivnostCreateDto dto)
    {
        var a = new Aktivnost { Naziv = dto.Naziv, Opis = dto.Opis, TipAktivnosti = dto.TipAktivnosti };
        _db.Aktivnosti.Add(a); await _db.SaveChangesAsync();
        return Ok(new AktivnostDto(a.Id, a.Naziv, a.Opis, a.TipAktivnosti));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> Update(int id, [FromBody] AktivnostCreateDto dto)
    {
        var a = await _db.Aktivnosti.FindAsync(id);
        if (a is null) return NotFound();
        a.Naziv = dto.Naziv; a.Opis = dto.Opis; a.TipAktivnosti = dto.TipAktivnosti;
        await _db.SaveChangesAsync();
        return Ok(new AktivnostDto(a.Id, a.Naziv, a.Opis, a.TipAktivnosti));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> Delete(int id)
    {
        var a = await _db.Aktivnosti.FindAsync(id);
        if (a is null) return NotFound();
        if (await _db.Termini.AnyAsync(x => x.AktivnostId == id))
            return BadRequest(new { message = "Nije moguće obrisati aktivnost koja ima zakazane termine." });
        _db.Aktivnosti.Remove(a); await _db.SaveChangesAsync();
        return NoContent();
    }
}

[ApiController]
[Route("api/sale")]
[Authorize]
public class SaleController : ControllerBase
{
    private readonly VitaStudioDbContext _db;
    public SaleController(VitaStudioDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var sale = await _db.Sale.ToListAsync();
        int daysFromMonday = ((int)DateTime.Today.DayOfWeek + 6) % 7;
        var weekStart = DateTime.Today.AddDays(-daysFromMonday);
        var weekEnd   = weekStart.AddDays(7);
        var termini = await _db.Termini
            .Include(t => t.Rezervacije)
            .Where(t => t.DatumVreme >= weekStart && t.DatumVreme < weekEnd)
            .ToListAsync();

        return Ok(sale.Select(s =>
        {
            var salaTerm    = termini.Where(t => t.SalaId == s.Id).ToList();
            int terminiNed  = salaTerm.Count;
            int ukupnoMesta = salaTerm.Sum(t => t.MaxKapacitet);
            int ukupnoRez   = salaTerm.Sum(t => t.Rezervacije.Count(r => r.Status == "aktivna"));
            int popunjenost = ukupnoMesta > 0 ? (int)Math.Round((double)ukupnoRez / ukupnoMesta * 100) : 0;
            return new { s.Id, s.Naziv, s.Kapacitet, s.Tip, TerminiNed = terminiNed, Popunjenost = popunjenost };
        }));
    }

    [HttpPost]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> Create([FromBody] SalaCreateDto dto)
    {
        var s = new Sala { Naziv = dto.Naziv, Kapacitet = dto.Kapacitet, Tip = dto.Tip };
        _db.Sale.Add(s); await _db.SaveChangesAsync();
        return Ok(new SalaDto(s.Id, s.Naziv, s.Kapacitet, s.Tip));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> Update(int id, [FromBody] SalaCreateDto dto)
    {
        var s = await _db.Sale.FindAsync(id);
        if (s is null) return NotFound();
        s.Naziv = dto.Naziv; s.Kapacitet = dto.Kapacitet; s.Tip = dto.Tip;
        await _db.SaveChangesAsync();
        return Ok(new SalaDto(s.Id, s.Naziv, s.Kapacitet, s.Tip));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await _db.Sale.FindAsync(id);
        if (s is null) return NotFound();
        if (await _db.Termini.AnyAsync(x => x.SalaId == id))
            return BadRequest(new { message = "Nije moguće obrisati salu koja ima zakazane termine." });
        _db.Sale.Remove(s); await _db.SaveChangesAsync();
        return NoContent();
    }
}

[ApiController]
[Route("api/clanarine")]
[Authorize]
public class ClanarineController : ControllerBase
{
    private readonly VitaStudioDbContext _db;
    public ClanarineController(VitaStudioDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        var istekle = await _db.Clanarine
            .Where(c => c.Status == "aktivna" && c.DatumIsteka < DateTime.UtcNow)
            .ToListAsync();
        if (istekle.Any())
        {
            foreach (var cl in istekle) cl.Status = "istekla";
            await _db.SaveChangesAsync();
        }

        return Ok(await _db.Clanarine
            .Include(c => c.Clan)
            .Where(c => status == null || c.Status == status)
            .OrderByDescending(c => c.DatumPocetka)
            .Select(c => new ClanarinaDto(
                c.Id, c.TipPaketa, c.NazivPaketa,
                c.DatumPocetka, c.DatumIsteka, c.Cena, c.Status,
                c.ClanId, $"{c.Clan.Ime} {c.Clan.Prezime}"))
            .ToListAsync());
    }

    [HttpGet("clan/{clanId}")]
    public async Task<IActionResult> GetByClan(int clanId) =>
        Ok(await _db.Clanarine
            .Where(c => c.ClanId == clanId)
            .OrderByDescending(c => c.DatumPocetka)
            .Select(c => new ClanarinaDto(
                c.Id, c.TipPaketa, c.NazivPaketa,
                c.DatumPocetka, c.DatumIsteka, c.Cena, c.Status,
                c.ClanId, null))
            .ToListAsync());

    [HttpPost]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> Create([FromBody] ClanarinaCreateDto dto)
    {
        if (dto.DatumIsteka <= dto.DatumPocetka)
            return BadRequest(new { message = "Datum isteka mora biti nakon datuma početka." });

        var c = new Clanarina
        {
            TipPaketa = dto.TipPaketa,
            NazivPaketa = dto.NazivPaketa,
            DatumPocetka = dto.DatumPocetka,
            DatumIsteka = dto.DatumIsteka,
            Cena = dto.Cena,
            Status = "aktivna",
            ClanId = dto.ClanId
        };
        _db.Clanarine.Add(c); await _db.SaveChangesAsync();
        var clan = await _db.Clanovi.FindAsync(dto.ClanId);
        return Ok(new ClanarinaDto(c.Id, c.TipPaketa, c.NazivPaketa,
            c.DatumPocetka, c.DatumIsteka, c.Cena, c.Status,
            c.ClanId, clan is null ? null : $"{clan.Ime} {clan.Prezime}"));
    }

    [HttpPost("samoodabir")]
    [Authorize(Roles = "clan")]
    public async Task<IActionResult> Samoodabir([FromBody] SamoodabirDto dto)
    {
        var clanId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var imaAktivnu = await _db.Clanarine
            .AnyAsync(c => c.ClanId == clanId && c.DatumIsteka > DateTime.UtcNow);
        if (imaAktivnu)
            return BadRequest(new { message = "Već imate aktivnu članarinu. Možete je obnoviti tek nakon isteka." });

        var pocetakUtc = DateTime.UtcNow.Date;
        var trajanje = dto.TipPaketa switch
        {
            "mesecna"  => 30,
            "kvartalna" => 90,
            "godisnja" => 365,
            _ => 30
        };
        var c = new Clanarina
        {
            TipPaketa   = dto.TipPaketa,
            NazivPaketa = "Sve aktivnosti",
            DatumPocetka = pocetakUtc,
            DatumIsteka  = pocetakUtc.AddDays(trajanje),
            Cena    = dto.Cena,
            Status  = "aktivna",
            ClanId  = clanId
        };
        _db.Clanarine.Add(c);
        await _db.SaveChangesAsync();
        return Ok(new ClanarinaDto(c.Id, c.TipPaketa, c.NazivPaketa,
            c.DatumPocetka, c.DatumIsteka, c.Cena, c.Status, clanId, null));
    }
}

[ApiController]
[Route("api/checkin")]
[Authorize(Roles = "administrator,trener")]
public class CheckInController : ControllerBase
{
    private readonly ICheckInService _svc;
    public CheckInController(ICheckInService svc) => _svc = svc;

    [HttpGet("termin/{terminId}")]
    public async Task<IActionResult> GetLista(int terminId) =>
        Ok(await _svc.GetTerminListaAsync(terminId));

    [HttpPost]
    public async Task<IActionResult> CheckIn([FromBody] EvidencijaCreateDto dto)
    {
        try { return Ok(await _svc.CheckInAsync(dto.ClanId, dto.TerminId)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Undo(int id)
    {
        try { await _svc.UndoCheckInAsync(id); return NoContent(); }
        catch (InvalidOperationException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("statistika")]
    public async Task<IActionResult> Statistika() =>
        Ok(await _svc.GetStatistikaDanasAsync());
}

[ApiController]
[Route("api/listacekanja")]
[Authorize]
public class ListaCekanjaController : ControllerBase
{
    private readonly VitaStudioDbContext _db;
    public ListaCekanjaController(VitaStudioDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> GetAll([FromQuery] string? datum)
    {
        var query = _db.ListeCekanja
            .Include(l => l.Clan)
            .Include(l => l.Termin).ThenInclude(t => t.Aktivnost)
            .Include(l => l.Termin).ThenInclude(t => t.Trener)
            .Include(l => l.Termin).ThenInclude(t => t.Sala)
            .AsQueryable();

        if (datum != null && DateTime.TryParse(datum, out var d))
        {
            var start = d.Date;
            var end   = d.Date.AddDays(1);
            query = query.Where(l => l.Termin.DatumVreme >= start && l.Termin.DatumVreme < end);
        }

        var lista = await query.OrderBy(l => l.Termin.DatumVreme).ThenBy(l => l.Pozicija).ToListAsync();
        return Ok(lista.Select(l => new ListaCekanjaAdminDto(
            l.Id, l.DatumPrijave, l.Pozicija,
            l.ClanId, $"{l.Clan.Ime} {l.Clan.Prezime}",
            l.TerminId, l.Termin.DatumVreme, l.Termin.Aktivnost.Naziv,
            l.Termin.Sala.Naziv, $"{l.Termin.Trener.Ime} {l.Termin.Trener.Prezime}"
        )));
    }

    [HttpGet("moje")]
    [Authorize(Roles = "clan")]
    public async Task<IActionResult> GetMoje()
    {
        var clanId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var lista = await _db.ListeCekanja
            .Where(l => l.ClanId == clanId)
            .Include(l => l.Clan)
            .Include(l => l.Termin).ThenInclude(t => t.Aktivnost)
            .Include(l => l.Termin).ThenInclude(t => t.Trener)
            .Include(l => l.Termin).ThenInclude(t => t.Sala)
            .OrderBy(l => l.Termin.DatumVreme)
            .ToListAsync();

        return Ok(lista.Select(l => new ListaCekanjaDto(
            l.Id, l.DatumPrijave, l.Pozicija,
            l.ClanId, $"{l.Clan.Ime} {l.Clan.Prezime}",
            l.TerminId, l.Termin.DatumVreme, l.Termin.Aktivnost.Naziv
        )));
    }

    [HttpPost]
    [Authorize(Roles = "clan")]
    public async Task<IActionResult> Pridruzi([FromBody] PridruziListiDto dto)
    {
        var clanId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var termin = await _db.Termini.Include(t => t.Rezervacije).FirstOrDefaultAsync(t => t.Id == dto.TerminId);
        if (termin is null) return NotFound(new { message = "Termin ne postoji." });
        if (termin.Rezervacije.Count(r => r.Status == "aktivna") < termin.MaxKapacitet)
            return BadRequest(new { message = "Termin ima slobodnih mesta. Napravite rezervaciju." });

        var vecNaListi = await _db.ListeCekanja.AnyAsync(l => l.ClanId == clanId && l.TerminId == dto.TerminId);
        if (vecNaListi) return BadRequest(new { message = "Već ste na listi čekanja za ovaj termin." });

        var clanarina = await _db.Clanarine
            .Where(c => c.ClanId == clanId && c.Status == "aktivna" && c.DatumIsteka >= DateTime.UtcNow)
            .FirstOrDefaultAsync();
        if (clanarina is null) return BadRequest(new { message = "Nemate aktivnu članarinu." });

        var pozicija = await _db.ListeCekanja.CountAsync(l => l.TerminId == dto.TerminId) + 1;
        var entry = new ListaCekanja { ClanId = clanId, TerminId = dto.TerminId, DatumPrijave = DateTime.UtcNow, Pozicija = pozicija };
        _db.ListeCekanja.Add(entry);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Dodati ste na listu čekanja.", pozicija });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "clan,administrator")]
    public async Task<IActionResult> Ukloni(int id)
    {
        var entry = await _db.ListeCekanja.FindAsync(id);
        if (entry is null) return NotFound();

        var isAdmin = User.IsInRole("administrator");
        if (!isAdmin)
        {
            var clanId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (entry.ClanId != clanId) return Forbid();
        }

        var pozicija = entry.Pozicija;
        var terminId = entry.TerminId;
        _db.ListeCekanja.Remove(entry);

        var ostali = await _db.ListeCekanja
            .Where(l => l.TerminId == terminId && l.Pozicija > pozicija)
            .ToListAsync();
        foreach (var l in ostali) l.Pozicija--;

        await _db.SaveChangesAsync();
        return NoContent();
    }
}

[ApiController]
[Route("api/notifikacije")]
[Authorize]
public class NotifikacijeController : ControllerBase
{
    private readonly INotifikacijaService _svc;
    public NotifikacijeController(INotifikacijaService svc) => _svc = svc;

    [HttpGet("moje")]
    public async Task<IActionResult> GetMoje()
    {
        var id  = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var tip = User.IsInRole("clan") ? "clan" : User.IsInRole("trener") ? "trener" : "administrator";
        return Ok(await _svc.GetMojeAsync(id, tip));
    }

    [HttpGet("broj")]
    public async Task<IActionResult> GetBroj()
    {
        var id  = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var tip = User.IsInRole("clan") ? "clan" : User.IsInRole("trener") ? "trener" : "administrator";
        return Ok(new { broj = await _svc.BrojNeprocitanihAsync(id, tip) });
    }

    [HttpPut("{id}/procitaj")]
    public async Task<IActionResult> Procitaj(int id)
    {
        var korisnikId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _svc.ProcitajAsync(id, korisnikId);
        return NoContent();
    }

    [HttpPut("procitaj-sve")]
    public async Task<IActionResult> ProcitajSve()
    {
        var id  = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var tip = User.IsInRole("clan") ? "clan" : User.IsInRole("trener") ? "trener" : "administrator";
        await _svc.ProcitajSveAsync(id, tip);
        return NoContent();
    }
}

[ApiController]
[Route("api/pregled")]
[Authorize]
public class PregledController : ControllerBase
{
    private readonly IIzvestajService _svc;
    public PregledController(IIzvestajService svc) => _svc = svc;

    [HttpGet("admin")]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> AdminPregled() =>
        Ok(await _svc.GetAdminPregledAsync());

    [HttpGet("trener/{trenerId}")]
    [Authorize(Roles = "trener")]
    public async Task<IActionResult> TrenerPregled(int trenerId) =>
        Ok(await _svc.GetTrenerPregledAsync(trenerId));

    [HttpGet("clan/{clanId}")]
    [Authorize(Roles = "clan")]
    public async Task<IActionResult> ClanPocetna(int clanId) =>
        Ok(await _svc.GetClanPocetnaAsync(clanId));

    [HttpGet("izvestaji")]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> Izvestaj([FromQuery] int meseci = 7) =>
        Ok(await _svc.GetIzvestajAsync(meseci));
}