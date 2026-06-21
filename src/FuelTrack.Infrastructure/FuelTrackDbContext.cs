using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure
{
    public class FuelTrackDbContext : DbContext
    {
        public FuelTrackDbContext(DbContextOptions<FuelTrackDbContext> options)
            : base(options) { }

        // Master tables
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<Station> Stations => Set<Station>();
        public DbSet<SubStation> SubStations => Set<SubStation>();
        public DbSet<Generator> Generators => Set<Generator>();
        public DbSet<OilTank> OilTanks => Set<OilTank>();
        public DbSet<FuelTank> FuelTanks => Set<FuelTank>();
        public DbSet<Screen> Screens => Set<Screen>();
        public DbSet<Device> Devices => Set<Device>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Lookup> Lookups => Set<Lookup>();

        // Junctions
        public DbSet<UserSubStation> UserSubStations => Set<UserSubStation>();
        public DbSet<GeneratorOil> GeneratorOils => Set<GeneratorOil>();
        public DbSet<GeneratorFuel> GeneratorFuels => Set<GeneratorFuel>();

        // Ledger tables
        public DbSet<DailyReading> DailyReadings => Set<DailyReading>();
        public DbSet<OilTransaction> OilTransactions => Set<OilTransaction>();
        public DbSet<FuelTransaction> FuelTransactions => Set<FuelTransaction>();
        public DbSet<PeriodicInventory> PeriodicInventories => Set<PeriodicInventory>();
        public DbSet<InventoryOilReading> InventoryOilReadings => Set<InventoryOilReading>();
        public DbSet<InventoryFuelReading> InventoryFuelReadings => Set<InventoryFuelReading>();

        // Support
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Master relationships
            modelBuilder.Entity<Branch>()
                .HasMany(b => b.Stations)
                .WithOne(s => s.Branch)
                .HasForeignKey(s => s.BranchId);

            modelBuilder.Entity<Station>()
                .HasMany(s => s.SubStations)
                .WithOne(ss => ss.Station)
                .HasForeignKey(ss => ss.StationId);

            modelBuilder.Entity<SubStation>()
                .HasMany(ss => ss.Generators)
                .WithOne(g => g.SubStation)
                .HasForeignKey(g => g.SubStationId);

            modelBuilder.Entity<SubStation>()
                .HasMany(ss => ss.OilTanks)
                .WithOne(t => t.SubStation)
                .HasForeignKey(t => t.SubStationId);

            modelBuilder.Entity<SubStation>()
                .HasMany(ss => ss.FuelTanks)
                .WithOne(t => t.SubStation)
                .HasForeignKey(t => t.SubStationId);

            // Lookup unique index
            modelBuilder.Entity<Lookup>()
                .HasIndex(l => new { l.Category, l.Code })
                .IsUnique();

            // Generator ↔ Oil many‑to‑many
            modelBuilder.Entity<GeneratorOil>()
                .HasOne(go => go.Generator)
                .WithMany(g => g.AllowedOils)
                .HasForeignKey(go => go.GeneratorId);

            modelBuilder.Entity<GeneratorOil>()
                .HasOne(go => go.OilLookup)
                .WithMany()
                .HasForeignKey(go => go.OilLookupId);

            modelBuilder.Entity<GeneratorOil>()
                .HasIndex(go => new { go.GeneratorId, go.OilLookupId })
                .IsUnique();

            // Generator ↔ Fuel (optional)
            modelBuilder.Entity<GeneratorFuel>()
                .HasOne(gf => gf.Generator)
                .WithMany(g => g.AllowedFuels)
                .HasForeignKey(gf => gf.GeneratorId);

            modelBuilder.Entity<GeneratorFuel>()
                .HasOne(gf => gf.FuelLookup)
                .WithMany()
                .HasForeignKey(gf => gf.FuelLookupId);

            modelBuilder.Entity<GeneratorFuel>()
                .HasIndex(gf => new { gf.GeneratorId, gf.FuelLookupId })
                .IsUnique();

            // OilTank unique per SubStation + Oil
            modelBuilder.Entity<OilTank>()
                .HasIndex(t => new { t.SubStationId, t.OilLookupId })
                .IsUnique();

            // FuelTank unique per SubStation + Fuel
            modelBuilder.Entity<FuelTank>()
                .HasIndex(t => new { t.SubStationId, t.FuelLookupId })
                .IsUnique();

            // User ↔ SubStation junction
            modelBuilder.Entity<UserSubStation>()
                .HasOne(us => us.User)
                .WithMany(u => u.SubStationLinks)
                .HasForeignKey(us => us.UserId);

            modelBuilder.Entity<UserSubStation>()
                .HasOne(us => us.SubStation)
                .WithMany(ss => ss.UserLinks)
                .HasForeignKey(us => us.SubStationId);

            modelBuilder.Entity<UserSubStation>()
                .HasIndex(us => new { us.UserId, us.SubStationId })
                .IsUnique();

            // Notification relationships
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.SubStation)
                .WithMany()
                .HasForeignKey(n => n.SubStationId);

            // Ledger unique constraint for DailyReading
            modelBuilder.Entity<DailyReading>()
                .HasIndex(dr => new { dr.GeneratorId, dr.ReadingDate, dr.Shift })
                .IsUnique();

            // View mapping (read‑only)
            modelBuilder.Entity<OilTank>()
                .HasNoKey()
                .ToView("vw_SubStation_OilBalances");
        }
    }
}
