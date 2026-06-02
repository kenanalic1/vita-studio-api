using System.ComponentModel.DataAnnotations.Schema;

namespace VitaStudio.Api.Models;

[Table("aktivnost")]
public class Aktivnost
{
    [Column("id")] public int Id { get; set; }
    [Column("naziv")] public string Naziv { get; set; } = string.Empty;
    [Column("opis")] public string? Opis { get; set; }
    [Column("tip_aktivnosti")] public string TipAktivnosti { get; set; } = string.Empty;

    public ICollection<Termin> Termini { get; set; } = new List<Termin>();
}