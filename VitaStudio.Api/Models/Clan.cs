using System.ComponentModel.DataAnnotations.Schema;

namespace VitaStudio.Api.Models;

[Table("clan")]
public class Clan
{
    [Column("id")] public int Id { get; set; }
    [Column("ime")] public string Ime { get; set; } = string.Empty;
    [Column("prezime")] public string Prezime { get; set; } = string.Empty;
    [Column("email")] public string Email { get; set; } = string.Empty;
    [Column("lozinka")] public string Lozinka { get; set; } = string.Empty;
    [Column("telefon")] public string Telefon { get; set; } = string.Empty;
    [Column("datum_rodjenja")] public DateTime DatumRodjenja { get; set; }
    [Column("datum_kreiranja")] public DateTime DatumKreiranja { get; set; } = DateTime.UtcNow;
    [Column("status")] public string Status { get; set; } = "aktivan";

    public ICollection<Clanarina> Clanarine { get; set; } = new List<Clanarina>();
    public ICollection<Rezervacija> Rezervacije { get; set; } = new List<Rezervacija>();
    public ICollection<ListaCekanja> ListeCekanja { get; set; } = new List<ListaCekanja>();
    public ICollection<EvidencijaDolaska> EvidencijeDolazaka { get; set; } = new List<EvidencijaDolaska>();
}