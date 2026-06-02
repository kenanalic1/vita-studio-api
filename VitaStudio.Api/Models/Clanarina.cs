using System.ComponentModel.DataAnnotations.Schema;

namespace VitaStudio.Api.Models;

[Table("clanarina")]
public class Clanarina
{
    [Column("id")] public int Id { get; set; }
    [Column("tip_paketa")] public string TipPaketa { get; set; } = string.Empty;
    [Column("naziv_paketa")] public string NazivPaketa { get; set; } = string.Empty;
    [Column("datum_pocetka")] public DateTime DatumPocetka { get; set; }
    [Column("datum_isteka")] public DateTime DatumIsteka { get; set; }
    [Column("cena")] public decimal Cena { get; set; }
    [Column("status")] public string Status { get; set; } = string.Empty;
    [Column("clan_id")] public int ClanId { get; set; }

    public Clan Clan { get; set; } = null!;
}