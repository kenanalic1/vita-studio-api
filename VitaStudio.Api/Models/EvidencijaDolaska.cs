using System.ComponentModel.DataAnnotations.Schema;

namespace VitaStudio.Api.Models;

[Table("evidencija_dolaska")]
public class EvidencijaDolaska
{
    [Column("id")] public int Id { get; set; }
    [Column("datum_dolaska")] public DateTime DatumDolaska { get; set; } = DateTime.UtcNow;
    [Column("clan_id")] public int ClanId { get; set; }
    [Column("termin_id")] public int TerminId { get; set; }

    public Clan Clan { get; set; } = null!;
    public Termin Termin { get; set; } = null!;
}