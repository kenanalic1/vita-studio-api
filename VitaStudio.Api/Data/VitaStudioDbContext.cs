using Microsoft.EntityFrameworkCore;
using VitaStudio.Api.Models;

namespace VitaStudio.Api.Data;

public class VitaStudioDbContext : DbContext
{
    public VitaStudioDbContext(DbContextOptions<VitaStudioDbContext> options) : base(options) { }

    public DbSet<Clan>              Clanovi             => Set<Clan>();
    public DbSet<Trener>            Treneri             => Set<Trener>();
    public DbSet<Administrator>     Administratori      => Set<Administrator>();
    public DbSet<Aktivnost>         Aktivnosti          => Set<Aktivnost>();
    public DbSet<Sala>              Sale                => Set<Sala>();
    public DbSet<Termin>            Termini             => Set<Termin>();
    public DbSet<Clanarina>         Clanarine           => Set<Clanarina>();
    public DbSet<Rezervacija>       Rezervacije         => Set<Rezervacija>();
    public DbSet<ListaCekanja>      ListeCekanja        => Set<ListaCekanja>();
    public DbSet<EvidencijaDolaska> EvidencijeDolazaka  => Set<EvidencijaDolaska>();
    public DbSet<Notifikacija>      Notifikacije        => Set<Notifikacija>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Clanarina>().Property(c => c.Cena).HasPrecision(10, 2);

        // Unique constraints (vec u SQL skripti, ali da EF zna)
        b.Entity<Clan>().HasIndex(c => c.Email).IsUnique();
        b.Entity<Trener>().HasIndex(t => t.Email).IsUnique();
        b.Entity<Administrator>().HasIndex(a => a.Email).IsUnique();

        b.Entity<Rezervacija>().HasIndex(r => new { r.ClanId, r.TerminId }).IsUnique();
        b.Entity<ListaCekanja>().HasIndex(l => new { l.ClanId, l.TerminId }).IsUnique();
        b.Entity<EvidencijaDolaska>().HasIndex(e => new { e.ClanId, e.TerminId }).IsUnique();

        base.OnModelCreating(b);
    }
}
