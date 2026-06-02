using Microsoft.EntityFrameworkCore;
using VitaStudio.Api.Data;
using VitaStudio.Api.Dtos;
using VitaStudio.Api.Models;

namespace VitaStudio.Api.Services;

public interface INotifikacijaService
{
    Task KreirajAsync(int korisnikId, string tipKorisnika, string tekst, string tip = "info");
    Task<List<NotifikacijaDto>> GetMojeAsync(int korisnikId, string tipKorisnika);
    Task<int>  BrojNeprocitanihAsync(int korisnikId, string tipKorisnika);
    Task ProcitajAsync(int id, int korisnikId);
    Task ProcitajSveAsync(int korisnikId, string tipKorisnika);
}

public class NotifikacijaService : INotifikacijaService
{
    private readonly VitaStudioDbContext _db;
    public NotifikacijaService(VitaStudioDbContext db) => _db = db;

    public async Task KreirajAsync(int korisnikId, string tipKorisnika, string tekst, string tip = "info")
    {
        _db.Notifikacije.Add(new Notifikacija
        {
            KorisnikId    = korisnikId,
            TipKorisnika  = tipKorisnika,
            Tekst         = tekst,
            Tip           = tip,
            DatumKreiranja = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();
    }

    public async Task<List<NotifikacijaDto>> GetMojeAsync(int korisnikId, string tipKorisnika)
    {
        return await _db.Notifikacije
            .Where(n => n.KorisnikId == korisnikId && n.TipKorisnika == tipKorisnika)
            .OrderByDescending(n => n.DatumKreiranja)
            .Take(20)
            .Select(n => new NotifikacijaDto(n.Id, n.Tekst, n.Tip, n.Procitana, n.DatumKreiranja))
            .ToListAsync();
    }

    public async Task<int> BrojNeprocitanihAsync(int korisnikId, string tipKorisnika)
    {
        return await _db.Notifikacije
            .CountAsync(n => n.KorisnikId == korisnikId && n.TipKorisnika == tipKorisnika && !n.Procitana);
    }

    public async Task ProcitajAsync(int id, int korisnikId)
    {
        var n = await _db.Notifikacije.FindAsync(id);
        if (n is null || n.KorisnikId != korisnikId) return;
        n.Procitana = true;
        await _db.SaveChangesAsync();
    }

    public async Task ProcitajSveAsync(int korisnikId, string tipKorisnika)
    {
        var lista = await _db.Notifikacije
            .Where(n => n.KorisnikId == korisnikId && n.TipKorisnika == tipKorisnika && !n.Procitana)
            .ToListAsync();
        foreach (var n in lista) n.Procitana = true;
        await _db.SaveChangesAsync();
    }
}
