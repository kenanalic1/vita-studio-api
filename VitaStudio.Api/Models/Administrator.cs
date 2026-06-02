using System.ComponentModel.DataAnnotations.Schema;

namespace VitaStudio.Api.Models;

[Table("administrator")]
public class Administrator
{
    [Column("id")] public int Id { get; set; }
    [Column("ime")] public string Ime { get; set; } = string.Empty;
    [Column("prezime")] public string Prezime { get; set; } = string.Empty;
    [Column("email")] public string Email { get; set; } = string.Empty;
    [Column("lozinka")] public string Lozinka { get; set; } = string.Empty;
}