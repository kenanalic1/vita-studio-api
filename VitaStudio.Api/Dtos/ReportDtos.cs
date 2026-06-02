namespace VitaStudio.Api.Dtos;

public record AdminPregledDto(
    decimal PrihodOvogMeseca,
    int AktivneClanice,
    int IstekloOvogMeseca,
    double PopunjenostTermina,
    int IstekleZa7Dana,
    List<TerminDto> DanasnjiRaspored,
    List<DanPopunjenostDto> PopunjenostNedelje,
    List<TerminListaCekanjaSummary> ListeCekanja,
    List<NedavnaAktivnostDto> NedavnaAktivnost
);

public record DanPopunjenostDto(string Dan, double Procenat);

public record TerminListaCekanjaSummary(
    int TerminId, string AktivnostNaziv, DateTime DatumVreme, int BrojNaListi
);

public record NedavnaAktivnostDto(
    string Tekst, DateTime Vreme, string Tip
);

public record TrenerPregledDto(
    int TerminiOveNedelje,
    int PopunjenihTermina,
    int UcesniceDanas,
    int RastVsProslaNed,
    double ProsecnaPopunjenost,
    int UkupnoNaListiCekanja,
    int TerminaSaListom,
    List<TerminDto> RasporedDanas,
    List<TerminDto> RasporedNedelja,
    List<string> Specijalizacije
);

public record ClanPocetnaDto(
    ClanarinaDto? AktivnaClanarina,
    int DolazakaOvogMeseca,
    string? NajdrazaAktivnost,
    int NajduzaSerija,
    DateTime ClanOd,
    List<RezervacijaDto> PredstojeciTermini,
    List<TerminDto> PredlozenoZaTebe
);

public record IzvestajDto(
    decimal UkupanPrihod,
    double ProsecnaPopunjenost,
    double RetencijaClanica,
    List<MesecPrihodDto> PrihodPoMesecima,
    List<TopAktivnostDto> TopAktivnosti,
    List<DistribucijaPaketaDto> DistribucijaPaketa
);
public record MesecPrihodDto(string Mesec, decimal Prihod);
public record TopAktivnostDto(string Naziv, double Popunjenost, int BrojTermina);
public record DistribucijaPaketaDto(string TipPaketa, int BrojAktivnih);