using Microsoft.EntityFrameworkCore;
using VitaStudio.Api.Data;
using VitaStudio.Api.Dtos;
using VitaStudio.Api.Models;

namespace VitaStudio.Api.Services;

public interface ICheckInService
{
    Task<List<CheckInListDto>> GetTerminListaAsync(int terminId);
    Task<EvidencijaDolaskaDto> CheckInAsync(int clanId, int terminId);
    Task UndoCheckInAsync(int id);
    Task<CheckInStatistikaDto> GetStatistikaDanasAsync();
}

public record CheckInListDto(
    int ClanId, string ImePrezime, string BrojClanske,
    string StatusClanarine, bool Stigao
);

public record CheckInStatistikaDto(
    int Prijavljeno, int Stiglo, int Otkazano, int ListaCekanjaPromovisan
);

public class CheckInService : ICheckInService
{
    private readonly VitaStudioDbContext _db;
    public CheckInService(VitaStudioDbContext db) => _db = db;

    public async Task<List<CheckInListDto>> GetTerminListaAsync(int terminId)
    {
        var rezervacije = await _db.Rezervacije
            .Where(r => r.TerminId == terminId && r.Status == "aktivna")
            .Include(r => r.Clan)
                .ThenInclude(c => c.Clanarine)
            .ToListAsync();

        var stigli = await _db.EvidencijeDolazaka
            .Where(e => e.TerminId == terminId)
            .Select(e => e.ClanId)
            .ToListAsync();

        return rezervacije.Select(r =>
        {
            var aktivnaClanarina = r.Clan.Clanarine
                .FirstOrDefault(c => c.Status == "aktivna");
            return new CheckInListDto(
                r.ClanId,
                $"{r.Clan.Ime} {r.Clan.Prezime}",
                $"#{r.ClanId:D3}",
                aktivnaClanarina?.NazivPaketa ?? "—",
                stigli.Contains(r.ClanId)
            );
        }).ToList();
    }

    public async Task<EvidencijaDolaskaDto> CheckInAsync(int clanId, int terminId)
    {
        var rez = await _db.Rezervacije
            .AnyAsync(r => r.ClanId == clanId && r.TerminId == terminId && r.Status == "aktivna");
        if (!rez) throw new InvalidOperationException("Clan nema aktivnu rezervaciju.");

        var vec = await _db.EvidencijeDolazaka
            .AnyAsync(e => e.ClanId == clanId && e.TerminId == terminId);
        if (vec) throw new InvalidOperationException("Clan je vec evidentiran.");

        var evidencija = new EvidencijaDolaska
        {
            ClanId = clanId,
            TerminId = terminId,
            DatumDolaska = DateTime.UtcNow
        };
        _db.EvidencijeDolazaka.Add(evidencija);

        var rezervacija = await _db.Rezervacije
            .FirstAsync(r => r.ClanId == clanId && r.TerminId == terminId);
        rezervacija.Status = "zavrsena";

        await _db.SaveChangesAsync();

        var termin = await _db.Termini
            .Include(t => t.Aktivnost).Include(t => t.Trener)
            .FirstAsync(t => t.Id == terminId);
        var clan = await _db.Clanovi.FindAsync(clanId);

        return new EvidencijaDolaskaDto(
            evidencija.Id, evidencija.DatumDolaska,
            clanId, $"{clan!.Ime} {clan.Prezime}",
            terminId, termin.Aktivnost.Naziv,
            $"{termin.Trener.Ime} {termin.Trener.Prezime}", "stigla"
        );
    }

    public async Task UndoCheckInAsync(int id)
    {
        var e = await _db.EvidencijeDolazaka.FindAsync(id)
            ?? throw new InvalidOperationException("Evidencija ne postoji.");
        _db.EvidencijeDolazaka.Remove(e);

        var rez = await _db.Rezervacije
            .FirstOrDefaultAsync(r => r.ClanId == e.ClanId && r.TerminId == e.TerminId);
        if (rez is not null) rez.Status = "aktivna";

        await _db.SaveChangesAsync();
    }

    public async Task<CheckInStatistikaDto> GetStatistikaDanasAsync()
    {
        var danas = DateTime.Today;
        var sutra = danas.AddDays(1);

        var terminiDanas = await _db.Termini
            .Where(t => t.DatumVreme >= danas && t.DatumVreme < sutra)
            .Select(t => t.Id).ToListAsync();

        var prijavljeno = await _db.Rezervacije
            .CountAsync(r => terminiDanas.Contains(r.TerminId) && r.Status != "otkazana");
        var stiglo = await _db.EvidencijeDolazaka
            .CountAsync(e => terminiDanas.Contains(e.TerminId));
        var otkazano = await _db.Rezervacije
            .CountAsync(r => terminiDanas.Contains(r.TerminId) && r.Status == "otkazana");

        return new CheckInStatistikaDto(prijavljeno, stiglo, otkazano, 0);
    }
}