namespace VitaStudio.Api.Dtos;

public record ClanDto(
    int Id, string Ime, string Prezime, string Email,
    string Telefon, DateTime DatumRodjenja, DateTime DatumKreiranja,
    string Status
);
public record ClanCreateDto(
    string Ime, string Prezime, string Email, string Lozinka,
    string Telefon, DateTime DatumRodjenja
);
public record ClanUpdateDto(
    string Ime, string Prezime, string Email, string Telefon, DateTime DatumRodjenja
);
public record ClanProfilDto(
    int Id, string Ime, string Prezime, string Email,
    string Telefon, DateTime DatumRodjenja, DateTime DatumKreiranja,
    ClanarinaDto? AktivnaClanarina,
    int UkupnoTermina,
    int Otkazivanja,
    int NaListiCekanja,
    int Dolasci30d,
    List<EvidencijaDolaskaDto> IstorijaDolazaka,
    List<RezervacijaDto> PredstojeciTermini
);

public record TrenerDto(
    int Id, string Ime, string Prezime, string Email,
    string Telefon, string Specijalizacija
);
public record TrenerCreateDto(
    string Ime, string Prezime, string Email, string Lozinka,
    string Telefon, string Specijalizacija
);

public record AktivnostDto(int Id, string Naziv, string? Opis, string TipAktivnosti);
public record AktivnostCreateDto(string Naziv, string? Opis, string TipAktivnosti);

public record SalaDto(int Id, string Naziv, int Kapacitet, string Tip);
public record SalaCreateDto(string Naziv, int Kapacitet, string Tip);

public record TerminDto(
    int Id,
    DateTime DatumVreme,
    int Trajanje,
    int MaxKapacitet,
    int BrojRezervacija,
    int BrojNaListiCekanja,
    int AktivnostId, string AktivnostNaziv, string TipAktivnosti,
    int TrenerId, string TrenerImePrezime,
    int SalaId, string SalaNaziv
);
public record TerminCreateDto(
    DateTime DatumVreme, int Trajanje, int MaxKapacitet,
    int AktivnostId, int TrenerId, int SalaId
);

public record ClanarinaDto(
    int Id, string TipPaketa, string NazivPaketa,
    DateTime DatumPocetka, DateTime DatumIsteka,
    decimal Cena, string Status,
    int ClanId, string? ClanImePrezime
);
public record ClanarinaCreateDto(
    string TipPaketa, string NazivPaketa,
    DateTime DatumPocetka, DateTime DatumIsteka,
    decimal Cena, int ClanId
);
public record SamoodabirDto(string TipPaketa, decimal Cena);

public record RezervacijaDto(
    int Id, DateTime DatumRezervacije, string Status,
    int ClanId, string ClanImePrezime,
    int TerminId, DateTime TerminDatumVreme, string AktivnostNaziv,
    string SalaNaziv, string TrenerImePrezime
);
public record RezervacijaCreateDto(int ClanId, int TerminId);
public record LozinkaDto(string StaraLozinka, string NovaLozinka);

public record ListaCekanjaDto(
    int Id, DateTime DatumPrijave, int Pozicija,
    int ClanId, string ClanImePrezime,
    int TerminId, DateTime TerminDatumVreme, string AktivnostNaziv
);
public record ListaCekanjaAdminDto(
    int Id, DateTime DatumPrijave, int Pozicija,
    int ClanId, string ClanImePrezime,
    int TerminId, DateTime TerminDatumVreme, string AktivnostNaziv,
    string SalaNaziv, string TrenerImePrezime
);

public record EvidencijaDolaskaDto(
    int Id, DateTime DatumDolaska,
    int ClanId, string? ClanImePrezime,
    int TerminId, string AktivnostNaziv,
    string TrenerImePrezime, string Status
);
public record EvidencijaCreateDto(int ClanId, int TerminId);
public record PridruziListiDto(int TerminId);
public record NotifikacijaDto(int Id, string Tekst, string Tip, bool Procitana, DateTime DatumKreiranja);