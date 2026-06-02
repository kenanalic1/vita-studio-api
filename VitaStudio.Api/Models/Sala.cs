using System.ComponentModel.DataAnnotations.Schema;

namespace VitaStudio.Api.Models;

[Table("sala")]
public class Sala
{
    [Column("id")] public int Id { get; set; }
    [Column("naziv")] public string Naziv { get; set; } = string.Empty;
    [Column("kapacitet")] public int Kapacitet { get; set; }
    [Column("tip")] public string Tip { get; set; } = string.Empty;

    public ICollection<Termin> Termini { get; set; } = new List<Termin>();
}