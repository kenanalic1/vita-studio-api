using System.ComponentModel.DataAnnotations.Schema;

namespace VitaStudio.Api.Models;

[Table("lista_cekanja")]
public class ListaCekanja
{
    [Column("id")] public int Id { get; set; }
    [Column("datum_prijave")] public DateTime DatumPrijave { get; set; } = DateTime.UtcNow;
    [Column("pozicija")] public int Pozicija { get; set; }
    [Column("clan_id")] public int ClanId { get; set; }
    [Column("termin_id")] public int TerminId { get; set; }

    public Clan Clan { get; set; } = null!;
    public Termin Termin { get; set; } = null!;
}