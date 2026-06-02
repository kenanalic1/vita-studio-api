using Microsoft.EntityFrameworkCore;
using VitaStudio.Api.Data;
using VitaStudio.Api.Dtos;
using VitaStudio.Api.Models;

namespace VitaStudio.Api.Services;

public interface ITerminService
{
    Task<List<TerminDto>> GetByDatumAsync(DateTime datum);
    Task<List<TerminDto>> GetByNedeljaAsync(DateTime ponedeljak);
    Task<List<TerminDto>> GetByTrenerAsync(int trenerId, DateTime ponedeljak);
    Task<TerminDto?> GetByIdAsync(int id);
    Task<TerminDto> CreateAsync(TerminCreateDto dto);
    Task<TerminDto> UpdateAsync(int id, TerminCreateDto dto);
    Task DeleteAsync(int id);
}

public class TerminService : ITerminService
{
    private readonly VitaStudioDbContext _db;
    public TerminService(VitaStudioDbContext db) => _db = db;

    private IQueryable<Termin> WithIncludes() =>
        _db.Termini
            .Include(t => t.Aktivnost)
            .Include(t => t.Trener)
            .Include(t => t.Sala)
            .Include(t => t.Rezervacije)
            .Include(t => t.ListeCekanja);

    public async Task<List<TerminDto>> GetByDatumAsync(DateTime datum)
    {
        var start = datum.Date;
        var end = start.AddDays(1);
        return await WithIncludes()
            .Where(t => t.DatumVreme >= start && t.DatumVreme < end)
            .OrderBy(t => t.DatumVreme)
            .Select(t => Map(t))
            .ToListAsync();
    }

    public async Task<List<TerminDto>> GetByNedeljaAsync(DateTime ponedeljak)
    {
        var end = ponedeljak.AddDays(7);
        return await WithIncludes()
            .Where(t => t.DatumVreme >= ponedeljak && t.DatumVreme < end)
            .OrderBy(t => t.DatumVreme)
            .Select(t => Map(t))
            .ToListAsync();
    }

    public async Task<List<TerminDto>> GetByTrenerAsync(int trenerId, DateTime ponedeljak)
    {
        var end = ponedeljak.AddDays(7);
        return await WithIncludes()
            .Where(t => t.TrenerId == trenerId && t.DatumVreme >= ponedeljak && t.DatumVreme < end)
            .OrderBy(t => t.DatumVreme)
            .Select(t => Map(t))
            .ToListAsync();
    }

    public async Task<TerminDto?> GetByIdAsync(int id) =>
        await WithIncludes()
            .Where(t => t.Id == id)
            .Select(t => Map(t))
            .FirstOrDefaultAsync();

    public async Task<TerminDto> CreateAsync(TerminCreateDto dto)
    {
        await CheckKonflikteAsync(dto);
        var termin = new Termin
        {
            DatumVreme = dto.DatumVreme,
            Trajanje = dto.Trajanje,
            MaxKapacitet = dto.MaxKapacitet,
            AktivnostId = dto.AktivnostId,
            TrenerId = dto.TrenerId,
            SalaId = dto.SalaId
        };
        _db.Termini.Add(termin);
        await _db.SaveChangesAsync();
        return (await GetByIdAsync(termin.Id))!;
    }

    public async Task<TerminDto> UpdateAsync(int id, TerminCreateDto dto)
    {
        var t = await _db.Termini.FindAsync(id)
            ?? throw new InvalidOperationException("Termin ne postoji.");
        await CheckKonflikteAsync(dto, excludeId: id);
        t.DatumVreme = dto.DatumVreme; t.Trajanje = dto.Trajanje;
        t.MaxKapacitet = dto.MaxKapacitet; t.AktivnostId = dto.AktivnostId;
        t.TrenerId = dto.TrenerId; t.SalaId = dto.SalaId;
        await _db.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    private async Task CheckKonflikteAsync(TerminCreateDto dto, int? excludeId = null)
    {
        var newStart = dto.DatumVreme;
        var newEnd   = dto.DatumVreme.AddMinutes(dto.Trajanje);
        var dayStart = newStart.Date;
        var dayEnd   = dayStart.AddDays(1);

        var terminiDana = await _db.Termini
            .Where(t => t.DatumVreme >= dayStart && t.DatumVreme < dayEnd)
            .Where(t => excludeId == null || t.Id != excludeId)
            .ToListAsync();

        var konfSala = terminiDana
            .Where(t => t.SalaId == dto.SalaId)
            .FirstOrDefault(t => newStart < t.DatumVreme.AddMinutes(t.Trajanje) && t.DatumVreme < newEnd);

        if (konfSala is not null)
            throw new InvalidOperationException(
                $"Sala je već zauzeta od {konfSala.DatumVreme:HH:mm} do {konfSala.DatumVreme.AddMinutes(konfSala.Trajanje):HH:mm}.");

        var konfTrener = terminiDana
            .Where(t => t.TrenerId == dto.TrenerId)
            .FirstOrDefault(t => newStart < t.DatumVreme.AddMinutes(t.Trajanje) && t.DatumVreme < newEnd);

        if (konfTrener is not null)
            throw new InvalidOperationException(
                $"Trener je već zauzet od {konfTrener.DatumVreme:HH:mm} do {konfTrener.DatumVreme.AddMinutes(konfTrener.Trajanje):HH:mm}.");
    }

    public async Task DeleteAsync(int id)
    {
        var t = await _db.Termini.FindAsync(id)
            ?? throw new InvalidOperationException("Termin ne postoji.");
        _db.Termini.Remove(t);
        await _db.SaveChangesAsync();
    }

    public static TerminDto Map(Termin t) => new(
        t.Id, t.DatumVreme, t.Trajanje, t.MaxKapacitet,
        t.Rezervacije.Count(r => r.Status == "aktivna"),
        t.ListeCekanja.Count,
        t.AktivnostId, t.Aktivnost.Naziv, t.Aktivnost.TipAktivnosti,
        t.TrenerId, $"{t.Trener.Ime} {t.Trener.Prezime}",
        t.SalaId, t.Sala.Naziv
    );
}