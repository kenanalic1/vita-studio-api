using System.ComponentModel.DataAnnotations.Schema;

namespace VitaStudio.Api.Models;

[Table("notifikacija")]
public class Notifikacija
{
    [Column("id")]               public int      Id               { get; set; }
    [Column("korisnik_id")]      public int      KorisnikId       { get; set; }
    [Column("tip_korisnika")]    public string   TipKorisnika     { get; set; } = "clan";
    [Column("tekst")]            public string   Tekst            { get; set; } = "";
    [Column("tip")]              public string   Tip              { get; set; } = "info";
    [Column("procitana")]        public bool     Procitana        { get; set; } = false;
    [Column("datum_kreiranja")]  public DateTime DatumKreiranja   { get; set; } = DateTime.UtcNow;
}
