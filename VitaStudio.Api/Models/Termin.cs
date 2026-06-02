using System.ComponentModel.DataAnnotations.Schema;

namespace VitaStudio.Api.Models;

[Table("termin")]
public class Termin
{
    [Column("id")] public int Id { get; set; }
    [Column("datum_vreme")] public DateTime DatumVreme { get; set; }
    [Column("trajanje")] public int Trajanje { get; set; }
    [Column("max_kapacitet")] public int MaxKapacitet { get; set; }
    [Column("aktivnost_id")] public int AktivnostId { get; set; }
    [Column("trener_id")] public int TrenerId { get; set; }
    [Column("sala_id")] public int SalaId { get; set; }

    public Aktivnost Aktivnost { get; set; } = null!;
    public Trener Trener { get; set; } = null!;
    public Sala Sala { get; set; } = null!;

    public ICollection<Rezervacija> Rezervacije { get; set; } = new List<Rezervacija>();
    public ICollection<ListaCekanja> ListeCekanja { get; set; } = new List<ListaCekanja>();
    public ICollection<EvidencijaDolaska> EvidencijeDolazaka { get; set; } = new List<EvidencijaDolaska>();
}