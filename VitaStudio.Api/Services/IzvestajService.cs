using Microsoft.EntityFrameworkCore;
using VitaStudio.Api.Data;
using VitaStudio.Api.Dtos;

namespace VitaStudio.Api.Services;

public interface IIzvestajService
{
    Task<AdminPregledDto>  GetAdminPregledAsync();
    Task<TrenerPregledDto> GetTrenerPregledAsync(int trenerId);
    Task<ClanPocetnaDto>   GetClanPocetnaAsync(int clanId);
    Task<IzvestajDto>      GetIzvestajAsync(int meseci);
}

public class IzvestajService : IIzvestajService
{
    private readonly VitaStudioDbContext _db;
    public IzvestajService(VitaStudioDbContext db) => _db = db;

    public async Task<AdminPregledDto> GetAdminPregledAsync()
    {
        var sad  = DateTime.UtcNow;
        var mes1 = new DateTime(sad.Year, sad.Month, 1);
        var mes2 = mes1.AddMonths(1);

        var prihod = await _db.Clanarine
            .Where(c => c.DatumPocetka >= mes1 && c.DatumPocetka < mes2)
            .SumAsync(c => (decimal?)c.Cena) ?? 0;

        var aktivneClanice = await _db.Clanarine.CountAsync(c => c.Status == "aktivna");

        var za7dana = sad.AddDays(7);
        var istekleZa7 = await _db.Clanarine
            .CountAsync(c => c.Status == "aktivna" && c.DatumIsteka <= za7dana);

        var terminDanasIds = await _db.Termini
            .Where(t => t.DatumVreme >= DateTime.Today && t.DatumVreme < DateTime.Today.AddDays(1))
            .Select(t => new { t.Id, t.MaxKapacitet })
            .ToListAsync();

        double popunjenost = 0;
        if (terminDanasIds.Any())
        {
            var ukupno = 0.0;
            foreach (var td in terminDanasIds)
            {
                var br = await _db.Rezervacije.CountAsync(r => r.TerminId == td.Id && r.Status == "aktivna");
                ukupno += td.MaxKapacitet > 0 ? (double)br / td.MaxKapacitet * 100 : 0;
            }
            popunjenost = ukupno / terminDanasIds.Count;
        }

        var rasporedDanas = await _db.Termini
            .Include(t => t.Aktivnost).Include(t => t.Trener).Include(t => t.Sala)
            .Include(t => t.Rezervacije).Include(t => t.ListeCekanja)
            .Where(t => t.DatumVreme >= DateTime.Today && t.DatumVreme < DateTime.Today.AddDays(1))
            .OrderBy(t => t.DatumVreme)
            .Select(t => TerminService.Map(t))
            .ToListAsync();

        var ponedeljak = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
        var nedeljnaPop = new List<DanPopunjenostDto>();
        string[] dani = { "Pon", "Uto", "Sre", "Čet", "Pet", "Sub", "Ned" };
        for (int i = 0; i < 7; i++)
        {
            var dan = ponedeljak.AddDays(i);
            var tIds = await _db.Termini
                .Where(t => t.DatumVreme >= dan && t.DatumVreme < dan.AddDays(1))
                .Select(t => new { t.Id, t.MaxKapacitet }).ToListAsync();

            double p = 0;
            if (tIds.Any())
            {
                double sum = 0;
                foreach (var td in tIds)
                {
                    var br = await _db.Rezervacije.CountAsync(r => r.TerminId == td.Id && r.Status == "aktivna");
                    sum += td.MaxKapacitet > 0 ? (double)br / td.MaxKapacitet * 100 : 0;
                }
                p = sum / tIds.Count;
            }
            nedeljnaPop.Add(new DanPopunjenostDto(dani[i], Math.Round(p, 1)));
        }

        var listeCekanja = await _db.ListeCekanja
            .Include(l => l.Termin).ThenInclude(t => t.Aktivnost)
            .GroupBy(l => l.TerminId)
            .Select(g => new TerminListaCekanjaSummary(
                g.Key,
                g.First().Termin.Aktivnost.Naziv,
                g.First().Termin.DatumVreme,
                g.Count()))
            .ToListAsync();

        var nedavneRez = await _db.Rezervacije
            .Include(r => r.Clan).Include(r => r.Termin).ThenInclude(t => t.Aktivnost)
            .OrderByDescending(r => r.DatumRezervacije)
            .Take(5)
            .Select(r => new NedavnaAktivnostDto(
                $"{r.Clan.Ime} {r.Clan.Prezime} rezervisala je termin — {r.Termin.Aktivnost.Naziv}",
                r.DatumRezervacije, "rezervacija"))
            .ToListAsync();

        var nedavneIstekle = await _db.Clanarine
            .Include(c => c.Clan)
            .Where(c => c.Status == "istekla" && c.DatumIsteka >= DateTime.Today.AddDays(-3))
            .Take(3)
            .Select(c => new NedavnaAktivnostDto(
                $"{c.Clan.Ime} {c.Clan.Prezime} — clanarina je istekla",
                c.DatumIsteka, "istek"))
            .ToListAsync();

        var nedavne = nedavneRez.Concat(nedavneIstekle)
            .OrderByDescending(n => n.Vreme).Take(6).ToList();

        return new AdminPregledDto(
            prihod, aktivneClanice, istekleZa7,
            Math.Round(popunjenost, 1), istekleZa7,
            rasporedDanas, nedeljnaPop, listeCekanja, nedavne
        );
    }

    public async Task<TrenerPregledDto> GetTrenerPregledAsync(int trenerId)
    {
        var ponedeljak = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
        var krajNedelje = ponedeljak.AddDays(7);
        var proslaPocetak = ponedeljak.AddDays(-7);

        var terminiNedelje = await _db.Termini
            .Include(t => t.Aktivnost).Include(t => t.Trener).Include(t => t.Sala)
            .Include(t => t.Rezervacije).Include(t => t.ListeCekanja)
            .Where(t => t.TrenerId == trenerId && t.DatumVreme >= ponedeljak && t.DatumVreme < krajNedelje)
            .OrderBy(t => t.DatumVreme)
            .ToListAsync();

        var terminiDanas = terminiNedelje
            .Where(t => t.DatumVreme.Date == DateTime.Today).ToList();

        var ucesniceDanas = 0;
        foreach (var t in terminiDanas)
            ucesniceDanas += t.Rezervacije.Count(r => r.Status == "aktivna");

        var terminiProsle = await _db.Termini
            .Include(t => t.Rezervacije)
            .Where(t => t.TrenerId == trenerId && t.DatumVreme >= proslaPocetak && t.DatumVreme < ponedeljak)
            .ToListAsync();
        var ucesniceProslaNed = terminiProsle.Sum(t => t.Rezervacije.Count(r => r.Status == "aktivna"));
        var rast = ucesniceDanas - ucesniceProslaNed;

        double prosecna = 0;
        if (terminiNedelje.Any())
        {
            prosecna = terminiNedelje.Average(t =>
                t.MaxKapacitet > 0
                    ? (double)t.Rezervacije.Count(r => r.Status == "aktivna") / t.MaxKapacitet * 100
                    : 0);
        }

        var popunjeni = terminiNedelje.Count(t =>
            t.Rezervacije.Count(r => r.Status == "aktivna") >= t.MaxKapacitet);

        var ukupnoLista = terminiNedelje.Sum(t => t.ListeCekanja.Count);
        var terminiSaListom = terminiNedelje.Count(t => t.ListeCekanja.Any());

        var trener = await _db.Treneri.FindAsync(trenerId);
        var spec = trener?.Specijalizacija.Split('/').Select(s => s.Trim()).ToList() ?? new();

        return new TrenerPregledDto(
            terminiNedelje.Count, popunjeni, ucesniceDanas, rast,
            Math.Round(prosecna, 1), ukupnoLista, terminiSaListom,
            terminiDanas.Select(t => TerminService.Map(t)).ToList(),
            terminiNedelje.Select(t => TerminService.Map(t)).ToList(),
            spec
        );
    }

    public async Task<ClanPocetnaDto> GetClanPocetnaAsync(int clanId)
    {
        var mesecPocetak = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        var aktivnaClanarina = await _db.Clanarine
            .Where(c => c.ClanId == clanId && c.Status == "aktivna")
            .OrderByDescending(c => c.DatumPocetka)
            .Select(c => new ClanarinaDto(
                c.Id, c.TipPaketa, c.NazivPaketa,
                c.DatumPocetka, c.DatumIsteka, c.Cena, c.Status,
                c.ClanId, null))
            .FirstOrDefaultAsync();

        var dolazakaOvogMeseca = await _db.EvidencijeDolazaka
            .CountAsync(e => e.ClanId == clanId && e.DatumDolaska >= mesecPocetak);

        var najdraza = await _db.EvidencijeDolazaka
            .Where(e => e.ClanId == clanId && e.DatumDolaska >= DateTime.Today.AddDays(-30))
            .Include(e => e.Termin).ThenInclude(t => t.Aktivnost)
            .GroupBy(e => e.Termin.Aktivnost.Naziv)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefaultAsync();

        var predstojeciTermini = await _db.Rezervacije
            .Where(r => r.ClanId == clanId && r.Status == "aktivna" && r.Termin.DatumVreme >= DateTime.UtcNow)
            .Include(r => r.Termin).ThenInclude(t => t.Aktivnost)
            .Include(r => r.Termin).ThenInclude(t => t.Trener)
            .Include(r => r.Termin).ThenInclude(t => t.Sala)
            .Include(r => r.Clan)
            .OrderBy(r => r.Termin.DatumVreme)
            .Take(5)
            .Select(r => new RezervacijaDto(
                r.Id, r.DatumRezervacije, r.Status,
                r.ClanId, $"{r.Clan.Ime} {r.Clan.Prezime}",
                r.TerminId, r.Termin.DatumVreme, r.Termin.Aktivnost.Naziv,
                r.Termin.Sala.Naziv, $"{r.Termin.Trener.Ime} {r.Termin.Trener.Prezime}"
            ))
            .ToListAsync();

        var rezervisaniIds = await _db.Rezervacije
            .Where(r => r.ClanId == clanId && r.Status == "aktivna")
            .Select(r => r.TerminId).ToListAsync();

        var predlozeni = await _db.Termini
            .Include(t => t.Aktivnost).Include(t => t.Trener).Include(t => t.Sala)
            .Include(t => t.Rezervacije).Include(t => t.ListeCekanja)
            .Where(t => !rezervisaniIds.Contains(t.Id) && t.DatumVreme >= DateTime.UtcNow)
            .OrderBy(t => t.DatumVreme)
            .Take(3)
            .Select(t => TerminService.Map(t))
            .ToListAsync();

        var clan = await _db.Clanovi.FindAsync(clanId);

        return new ClanPocetnaDto(
            aktivnaClanarina, dolazakaOvogMeseca, najdraza, 0,
            clan!.DatumKreiranja, predstojeciTermini, predlozeni
        );
    }

    public async Task<IzvestajDto> GetIzvestajAsync(int meseci)
    {
        var odDatum = DateTime.Today.AddMonths(-meseci);

        var ukupanPrihod = await _db.Clanarine
            .Where(c => c.DatumPocetka >= odDatum)
            .SumAsync(c => (decimal?)c.Cena) ?? 0;

        var clanarine = await _db.Clanarine
            .Where(c => c.DatumPocetka >= odDatum)
            .ToListAsync();

        var prihodPoMesecima = clanarine
            .GroupBy(c => new { c.DatumPocetka.Year, c.DatumPocetka.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MesecPrihodDto(
                $"{g.Key.Year}-{g.Key.Month:D2}",
                g.Sum(c => c.Cena)))
            .ToList();

        var termini = await _db.Termini
            .Include(t => t.Rezervacije)
            .Where(t => t.DatumVreme >= odDatum)
            .ToListAsync();

        double prosecnaPop = termini.Any()
            ? termini.Average(t =>
                t.MaxKapacitet > 0
                    ? (double)t.Rezervacije.Count(r => r.Status != "otkazana") / t.MaxKapacitet * 100
                    : 0)
            : 0;

        var topAktivnosti = termini
            .GroupBy(t => t.AktivnostId)
            .Select(g => new
            {
                Id   = g.Key,
                Pop  = g.Average(t => t.MaxKapacitet > 0 ? (double)t.Rezervacije.Count(r => r.Status != "otkazana") / t.MaxKapacitet * 100 : 0),
                Broj = g.Count()
            })
            .OrderByDescending(x => x.Pop)
            .Take(5)
            .ToList();

        var aktivnostiDict = await _db.Aktivnosti.ToDictionaryAsync(a => a.Id, a => a.Naziv);
        var topList = topAktivnosti.Select(x => new TopAktivnostDto(
            aktivnostiDict.GetValueOrDefault(x.Id, "?"),
            Math.Round(x.Pop, 1), x.Broj)).ToList();

        var ukupno  = await _db.Clanovi.CountAsync();
        var aktivni = await _db.Clanarine.CountAsync(c => c.Status == "aktivna");
        double retencija = ukupno > 0 ? (double)aktivni / ukupno * 100 : 0;

        var distribucija = await _db.Clanarine
            .Where(c => c.Status == "aktivna")
            .GroupBy(c => c.TipPaketa)
            .Select(g => new DistribucijaPaketaDto(g.Key, g.Count()))
            .ToListAsync();

        return new IzvestajDto(
            ukupanPrihod, Math.Round(prosecnaPop, 1), Math.Round(retencija, 1),
            prihodPoMesecima, topList, distribucija
        );
    }
}
