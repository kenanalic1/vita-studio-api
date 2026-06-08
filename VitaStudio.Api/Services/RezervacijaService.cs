using Microsoft.EntityFrameworkCore;
using VitaStudio.Api.Data;
using VitaStudio.Api.Dtos;
using VitaStudio.Api.Models;

namespace VitaStudio.Api.Services;

public interface IRezervacijaService
{
    Task<List<RezervacijaDto>> GetAllAsync(string? status, string? datum);
    Task<List<RezervacijaDto>> GetByClanAsync(int clanId);
    Task<List<RezervacijaDto>> GetByTerminAsync(int terminId);
    Task<RezervacijaDto>       RezervisiAsync(int clanId, int terminId);
    Task                       OtkaziAsync(int rezervacijaId, int clanId);
    Task                       AdminOtkaziAsync(int rezervacijaId);
}

public class RezervacijaService : IRezervacijaService
{
    private readonly VitaStudioDbContext  _db;
    private readonly INotifikacijaService _notif;
    private readonly IEmailService        _email;

    public RezervacijaService(VitaStudioDbContext db, INotifikacijaService notif, IEmailService email)
    {
        _db    = db;
        _notif = notif;
        _email = email;
    }

    public async Task<List<RezervacijaDto>> GetAllAsync(string? status, string? datum)
    {
        var query = _db.Rezervacije
            .Include(r => r.Clan)
            .Include(r => r.Termin).ThenInclude(t => t.Aktivnost)
            .Include(r => r.Termin).ThenInclude(t => t.Trener)
            .Include(r => r.Termin).ThenInclude(t => t.Sala)
            .AsQueryable();

        if (status != null)
            query = query.Where(r => r.Status == status);

        if (datum != null && DateTime.TryParse(datum, out var d))
        {
            var start = d.Date;
            var end   = d.Date.AddDays(1);
            query = query.Where(r => r.Termin.DatumVreme >= start && r.Termin.DatumVreme < end);
        }

        var list = await query.OrderByDescending(r => r.Termin.DatumVreme).ToListAsync();
        return list.Select(MapRez).ToList();
    }

    public async Task AdminOtkaziAsync(int rezervacijaId)
    {
        var rez = await _db.Rezervacije.FindAsync(rezervacijaId)
            ?? throw new InvalidOperationException("Rezervacija ne postoji.");

        if (rez.Status != "aktivna")
            throw new InvalidOperationException("Rezervacija nije aktivna.");

        rez.Status = "otkazana";
        await _db.SaveChangesAsync();

        await PromovisaPrvogSaListeAsync(rez.TerminId);
    }

    public async Task<List<RezervacijaDto>> GetByClanAsync(int clanId)
    {
        var rez = await _db.Rezervacije
            .Where(r => r.ClanId == clanId)
            .Include(r => r.Termin).ThenInclude(t => t.Aktivnost)
            .Include(r => r.Termin).ThenInclude(t => t.Trener)
            .Include(r => r.Termin).ThenInclude(t => t.Sala)
            .Include(r => r.Clan)
            .OrderByDescending(r => r.Termin.DatumVreme)
            .ToListAsync();
        return rez.Select(MapRez).ToList();
    }

    public async Task<List<RezervacijaDto>> GetByTerminAsync(int terminId)
    {
        var rez = await _db.Rezervacije
            .Where(r => r.TerminId == terminId && r.Status == "aktivna")
            .Include(r => r.Clan)
            .Include(r => r.Termin).ThenInclude(t => t.Aktivnost)
            .Include(r => r.Termin).ThenInclude(t => t.Trener)
            .Include(r => r.Termin).ThenInclude(t => t.Sala)
            .ToListAsync();
        return rez.Select(MapRez).ToList();
    }

    public async Task<RezervacijaDto> RezervisiAsync(int clanId, int terminId)
    {
        var imaClanarinu = await _db.Clanarine
            .AnyAsync(c => c.ClanId == clanId
                        && c.Status == "aktivna"
                        && c.DatumIsteka >= DateTime.UtcNow);
        if (!imaClanarinu)
            throw new InvalidOperationException(
                "Nemate aktivnu članarinu. Kontaktirajte recepciju za obnovu kako biste mogli da rezervišete termine.");

        var termin = await _db.Termini
            .Include(t => t.Aktivnost).Include(t => t.Trener).Include(t => t.Sala)
            .FirstOrDefaultAsync(t => t.Id == terminId)
            ?? throw new InvalidOperationException("Termin ne postoji.");

        if (termin.DatumVreme <= DateTime.UtcNow)
            throw new InvalidOperationException("Ne možete rezervisati termin koji je već prošao.");

        var existing = await _db.Rezervacije
            .FirstOrDefaultAsync(r => r.ClanId == clanId && r.TerminId == terminId && r.Status != "otkazana");
        if (existing is not null)
            throw new InvalidOperationException("Vec ste rezervisali ovaj termin.");

        var aktivnih = await _db.Rezervacije
            .CountAsync(r => r.TerminId == terminId && r.Status == "aktivna");

        if (aktivnih >= termin.MaxKapacitet)
        {
            var naListi = await _db.ListeCekanja
                .AnyAsync(l => l.ClanId == clanId && l.TerminId == terminId);
            if (naListi) throw new InvalidOperationException("Vec ste na listi cekanja.");

            var maxPoz = await _db.ListeCekanja
                .Where(l => l.TerminId == terminId)
                .MaxAsync(l => (int?)l.Pozicija) ?? 0;

            _db.ListeCekanja.Add(new ListaCekanja
            {
                ClanId = clanId, TerminId = terminId,
                Pozicija = maxPoz + 1, DatumPrijave = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            throw new InvalidOperationException($"Termin je popunjen. Dodate ste na listu cekanja (pozicija {maxPoz + 1}).");
        }

        var otkazana = await _db.Rezervacije
            .FirstOrDefaultAsync(r => r.ClanId == clanId && r.TerminId == terminId && r.Status == "otkazana");

        Rezervacija rez;
        if (otkazana is not null)
        {
            otkazana.Status = "aktivna";
            otkazana.DatumRezervacije = DateTime.UtcNow;
            rez = otkazana;
        }
        else
        {
            rez = new Rezervacija { ClanId = clanId, TerminId = terminId, Status = "aktivna", DatumRezervacije = DateTime.UtcNow };
            _db.Rezervacije.Add(rez);
        }
        await _db.SaveChangesAsync();

        var clan = await _db.Clanovi.FindAsync(clanId);
        return MapRezFull(rez, clan!, termin);
    }

    public async Task OtkaziAsync(int rezervacijaId, int clanId)
    {
        var rez = await _db.Rezervacije.FindAsync(rezervacijaId)
            ?? throw new InvalidOperationException("Rezervacija ne postoji.");

        if (rez.ClanId != clanId)
            throw new UnauthorizedAccessException("Nije vasa rezervacija.");

        var termin = await _db.Termini.FindAsync(rez.TerminId);
        if (termin is not null && termin.DatumVreme <= DateTime.UtcNow.AddHours(2))
            throw new InvalidOperationException(
                "Nije moguće otkazati termin manje od 2 sata prije početka.");

        rez.Status = "otkazana";
        await _db.SaveChangesAsync();

        await PromovisaPrvogSaListeAsync(rez.TerminId);
    }

    // ---- Promocija sa liste cekanja ----
    private async Task PromovisaPrvogSaListeAsync(int terminId)
    {
        var prvi = await _db.ListeCekanja
            .Include(l => l.Clan)
            .Include(l => l.Termin).ThenInclude(t => t.Aktivnost)
            .Where(l => l.TerminId == terminId)
            .OrderBy(l => l.Pozicija)
            .FirstOrDefaultAsync();

        if (prvi is null) return;

        _db.Rezervacije.Add(new Rezervacija
        {
            ClanId = prvi.ClanId, TerminId = terminId,
            Status = "aktivna", DatumRezervacije = DateTime.UtcNow,
            PromovisanSaListe = true
        });
        _db.ListeCekanja.Remove(prvi);

        var ostali = await _db.ListeCekanja
            .Where(l => l.TerminId == terminId && l.Pozicija > 1)
            .ToListAsync();
        foreach (var l in ostali) l.Pozicija--;

        await _db.SaveChangesAsync();

        var aktivnost = prvi.Termin.Aktivnost.Naziv;
        var datum     = prvi.Termin.DatumVreme.ToString("dd.MM.yyyy HH:mm");
        var tekst     = $"Dobili ste mesto na terminu {aktivnost} ({datum}). Vaša rezervacija je aktivna.";

        await _notif.KreirajAsync(prvi.ClanId, "clan", tekst, "uspeh");

        _ = _email.PosaljiAsync(
            prvi.Clan.Email,
            $"{prvi.Clan.Ime} {prvi.Clan.Prezime}",
            "Vita Studio — Dobili ste mesto!",
            tekst
        );
    }

    // ---- Maperi ----
    private static RezervacijaDto MapRez(Rezervacija r) => new(
        r.Id, r.DatumRezervacije, r.Status,
        r.ClanId, $"{r.Clan.Ime} {r.Clan.Prezime}",
        r.TerminId, r.Termin.DatumVreme, r.Termin.Aktivnost.Naziv,
        r.Termin.Sala.Naziv, $"{r.Termin.Trener.Ime} {r.Termin.Trener.Prezime}"
    );

    private static RezervacijaDto MapRezFull(Rezervacija r, Clan c, Termin t) => new(
        r.Id, r.DatumRezervacije, r.Status,
        c.Id, $"{c.Ime} {c.Prezime}",
        t.Id, t.DatumVreme, t.Aktivnost.Naziv,
        t.Sala.Naziv, $"{t.Trener.Ime} {t.Trener.Prezime}"
    );
}
