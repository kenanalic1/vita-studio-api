using System.ComponentModel.DataAnnotations.Schema;

namespace VitaStudio.Api.Models;

[Table("trener")]
public class Trener
{
    [Column("id")] public int Id { get; set; }
    [Column("ime")] public string Ime { get; set; } = string.Empty;
    [Column("prezime")] public string Prezime { get; set; } = string.Empty;
    [Column("email")] public string Email { get; set; } = string.Empty;
    [Column("lozinka")] public string Lozinka { get; set; } = string.Empty;
    [Column("telefon")] public string Telefon { get; set; } = string.Empty;
    [Column("specijalizacija")] public string Specijalizacija { get; set; } = string.Empty;

    public ICollection<Termin> Termini { get; set; } = new List<Termin>();
}