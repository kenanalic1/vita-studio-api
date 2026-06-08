using System.ComponentModel.DataAnnotations.Schema;

namespace VitaStudio.Api.Models;

[Table("rezervacija")]
public class Rezervacija
{
    [Column("id")] public int Id { get; set; }
    [Column("datum_rezervacije")] public DateTime DatumRezervacije { get; set; } = DateTime.UtcNow;
    [Column("status")] public string Status { get; set; } = "aktivna";
    [Column("clan_id")] public int ClanId { get; set; }
    [Column("termin_id")] public int TerminId { get; set; }
    [Column("promovisan_sa_liste")] public bool PromovisanSaListe { get; set; } = false;

    public Clan Clan { get; set; } = null!;
    public Termin Termin { get; set; } = null!;
}