using FuelTrack.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure
{
    public class FuelTrackDbContext : IdentityDbContext<User, Role, int>
    {
        public FuelTrackDbContext(DbContextOptions<FuelTrackDbContext> options)
            : base(options) { }

        // Master tables
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<Station> Stations => Set<Station>();
        public DbSet<SubStation> SubStations => Set<SubStation>();
        public DbSet<Generator> Generators => Set<Generator>();
        public DbSet<FuelTank> FuelTanks => Set<FuelTank>();
        public DbSet<SubStationReading> SubStationReadings => Set<SubStationReading>();
        public DbSet<Lookup> Lookups => Set<Lookup>();
        public DbSet<LookupCategory> LookupCategories => Set<LookupCategory>();

        // Junctions
        public DbSet<UserScope> UserScopes => Set<UserScope>();


        // Ledger tables
        public DbSet<DailyReading> DailyReadings => Set<DailyReading>();
        public DbSet<OilTransaction> OilTransactions => Set<OilTransaction>();
        public DbSet<FuelTransaction> FuelTransactions => Set<FuelTransaction>();
        public DbSet<PeriodicInventory> PeriodicInventories => Set<PeriodicInventory>();
        public DbSet<PeriodicInventoryFuel> PeriodicInventoryFuels => Set<PeriodicInventoryFuel>();
        public DbSet<PeriodicInventoryOil> PeriodicInventoryOils => Set<PeriodicInventoryOil>();

        // Oil inventory
        public DbSet<Oil> Oils => Set<Oil>();

        // Support
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<NotificationRecipient> NotificationRecipients => Set<NotificationRecipient>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Controller> Controllers => Set<Controller>();
        public DbSet<Meter> Meters => Set<Meter>();
        public DbSet<AnalyticsInsight> AnalyticsInsights => Set<AnalyticsInsight>();
        public DbSet<InsightFeedback> InsightFeedbacks => Set<InsightFeedback>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
        // Archive tables
        public DbSet<DailyReadingArchive> DailyReadingArchives => Set<DailyReadingArchive>();
        public DbSet<AuditLogArchive> AuditLogArchives => Set<AuditLogArchive>();

        // Warehouse & Maintenance
        public DbSet<SparePart> SpareParts => Set<SparePart>();
        public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
        public DbSet<WarehouseSupply> WarehouseSupplies => Set<WarehouseSupply>();
        public DbSet<WarehouseSupplyItem> WarehouseSupplyItems => Set<WarehouseSupplyItem>();
        public DbSet<DispatchRequest> DispatchRequests => Set<DispatchRequest>();
        public DbSet<DispatchRequestItem> DispatchRequestItems => Set<DispatchRequestItem>();
        public DbSet<WarehouseTransaction> WarehouseTransactions => Set<WarehouseTransaction>();
        public DbSet<WorkPermit> WorkPermits => Set<WorkPermit>();
        public DbSet<WorkTask> WorkTasks => Set<WorkTask>();
        public DbSet<InstalledPart> InstalledParts => Set<InstalledPart>();
        public DbSet<MaintenanceLog> MaintenanceLogs => Set<MaintenanceLog>();

        // Operational foundations
        public DbSet<StationOperationalSettings> StationOperationalSettings => Set<StationOperationalSettings>();
        public DbSet<Attachment> Attachments => Set<Attachment>();
        public DbSet<AttachmentLink> AttachmentLinks => Set<AttachmentLink>();
        public DbSet<CustomerFeatureSetting> CustomerFeatureSettings => Set<CustomerFeatureSetting>();
        public DbSet<IdempotencyRequest> IdempotencyRequests => Set<IdempotencyRequest>();

        // Operational capture (Phase 2). These are raw Pending records only;
        // Phase 3 owns review, approval and official calculations.
        public DbSet<OperationalSubmission> OperationalSubmissions => Set<OperationalSubmission>();
        public DbSet<OperationalGeneratorReading> OperationalGeneratorReadings => Set<OperationalGeneratorReading>();
        public DbSet<TankLevelReading> TankLevelReadings => Set<TankLevelReading>();
        public DbSet<Feeder> Feeders => Set<Feeder>();
        public DbSet<FeederLoadReading> FeederLoadReadings => Set<FeederLoadReading>();
        public DbSet<MeterReading> MeterReadings => Set<MeterReading>();
        public DbSet<GeneratorOperatingLimit> GeneratorOperatingLimits => Set<GeneratorOperatingLimit>();
        public DbSet<OverloadEvent> OverloadEvents => Set<OverloadEvent>();
        public DbSet<OperatingLimitEpisode> OperatingLimitEpisodes => Set<OperatingLimitEpisode>();

        // Permission system (Module Registry)
        public DbSet<ModuleDefinition> ModuleDefinitions => Set<ModuleDefinition>();
        public DbSet<PagePermission> PagePermissions => Set<PagePermission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Filter expressions are provider-specific SQL, not LINQ.
            var activeFilter = Database.IsNpgsql() ? "\"DeletedAt\" IS NULL" : "[DeletedAt] IS NULL";
            var approvedFilter = Database.IsNpgsql() ? "\"DeletedAt\" IS NULL AND \"IsApproved\" = TRUE" : "[DeletedAt] IS NULL AND [IsApproved] = 1";

            // === Identity table names ===
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Role>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
            modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
            modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");
            modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");

            modelBuilder.Entity<User>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Role>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            // === Soft-delete global filter ===
            modelBuilder.Entity<Branch>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<Station>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<SubStation>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<Generator>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<FuelTank>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<Controller>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<Meter>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<User>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<Role>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<DailyReading>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<OilTransaction>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<FuelTransaction>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<SubStationReading>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<PeriodicInventory>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<Notification>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<NotificationRecipient>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<AuditLog>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<AnalyticsInsight>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<InsightFeedback>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<RefreshToken>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<LoginAttempt>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<UserScope>().HasQueryFilter(e => e.DeletedAt == null);

            modelBuilder.Entity<Oil>().HasQueryFilter(e => e.DeletedAt == null);

            // === Warehouse & Maintenance soft-delete filters ===
            modelBuilder.Entity<SparePart>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<WarehouseStock>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<WarehouseSupply>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<WarehouseSupplyItem>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<DispatchRequest>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<DispatchRequestItem>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<WarehouseTransaction>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<WorkPermit>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<WorkTask>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<InstalledPart>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<MaintenanceLog>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<StationOperationalSettings>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<Attachment>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<AttachmentLink>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<CustomerFeatureSetting>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<IdempotencyRequest>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<OperationalSubmission>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<OperationalGeneratorReading>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<TankLevelReading>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<Feeder>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<FeederLoadReading>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<MeterReading>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<GeneratorOperatingLimit>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<OverloadEvent>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<OperatingLimitEpisode>().HasQueryFilter(e => e.DeletedAt == null);

            // === Oil relationships ===
            modelBuilder.Entity<Oil>()
                .HasOne(o => o.OilLookup)
                .WithMany()
                .HasForeignKey(o => o.OilLookupId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Oil>()
                .HasOne(o => o.Station)
                .WithMany()
                .HasForeignKey(o => o.StationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Oil>()
                .HasOne(o => o.SubStation)
                .WithMany()
                .HasForeignKey(o => o.SubStationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Branch>()
                .HasMany(b => b.Stations)
                .WithOne(s => s.Branch)
                .HasForeignKey(s => s.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            // === Station → SubStation ===
            modelBuilder.Entity<Station>()
                .HasMany(s => s.SubStations)
                .WithOne(ss => ss.Station)
                .HasForeignKey(ss => ss.StationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Station>()
                .HasOne(s => s.OperationalSettings)
                .WithOne(s => s.Station)
                .HasForeignKey<StationOperationalSettings>(s => s.StationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AttachmentLink>()
                .HasOne(link => link.Attachment)
                .WithMany(attachment => attachment.Links)
                .HasForeignKey(link => link.AttachmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OperationalSubmission>()
                .HasOne(x => x.SubStation).WithMany(x => x.OperationalSubmissions)
                .HasForeignKey(x => x.SubStationId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<OperationalGeneratorReading>()
                .HasOne(x => x.OperationalSubmission).WithMany(x => x.GeneratorReadings)
                .HasForeignKey(x => x.OperationalSubmissionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<OperationalGeneratorReading>()
                .HasOne(x => x.Generator).WithMany().HasForeignKey(x => x.GeneratorId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TankLevelReading>()
                .HasOne(x => x.OperationalSubmission).WithMany(x => x.TankLevelReadings)
                .HasForeignKey(x => x.OperationalSubmissionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TankLevelReading>()
                .HasOne(x => x.FuelTank).WithMany(x => x.LevelReadings).HasForeignKey(x => x.FuelTankId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TankLevelReading>()
                .HasOne(x => x.LinkedEvent).WithMany().HasForeignKey(x => x.LinkedEventId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Feeder>()
                .HasOne(x => x.SubStation).WithMany(x => x.Feeders).HasForeignKey(x => x.SubStationId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Feeder>()
                .HasOne(x => x.Meter).WithMany().HasForeignKey(x => x.MeterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FeederLoadReading>()
                .HasOne(x => x.OperationalSubmission).WithMany(x => x.FeederLoadReadings)
                .HasForeignKey(x => x.OperationalSubmissionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FeederLoadReading>()
                .HasOne(x => x.Feeder).WithMany(x => x.LoadReadings).HasForeignKey(x => x.FeederId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MeterReading>()
                .HasOne(x => x.OperationalSubmission).WithMany(x => x.MeterReadings)
                .HasForeignKey(x => x.OperationalSubmissionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MeterReading>()
                .HasOne(x => x.Meter).WithMany().HasForeignKey(x => x.MeterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FuelTank>()
                .HasOne(x => x.ParentTank).WithMany(x => x.DailyTanks).HasForeignKey(x => x.ParentTankId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FuelTank>()
                .HasOne(x => x.Generator).WithMany().HasForeignKey(x => x.GeneratorId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<GeneratorOperatingLimit>()
                .HasOne(x => x.Generator).WithMany().HasForeignKey(x => x.GeneratorId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<OverloadEvent>()
                .HasOne(x => x.Generator).WithMany().HasForeignKey(x => x.GeneratorId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<OverloadEvent>()
                .HasOne(x => x.TriggerReading).WithMany().HasForeignKey(x => x.TriggerReadingId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<OperatingLimitEpisode>()
                .HasOne(x => x.GeneratorOperatingLimit).WithMany().HasForeignKey(x => x.GeneratorOperatingLimitId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<OperatingLimitEpisode>()
                .HasOne(x => x.TriggerReading).WithMany().HasForeignKey(x => x.TriggerReadingId)
                .OnDelete(DeleteBehavior.Restrict);

            // === SubStation children ===
            modelBuilder.Entity<SubStation>()
                .HasMany(ss => ss.Generators)
                .WithOne(g => g.SubStation)
                .HasForeignKey(g => g.SubStationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SubStation>()
                .HasMany(ss => ss.FuelTanks)
                .WithOne(t => t.SubStation)
                .HasForeignKey(t => t.SubStationId)
                .OnDelete(DeleteBehavior.Restrict);


            // === Generator navigation ===
            // AllowedOils/AllowedFuels removed (tables dropped)

            modelBuilder.Entity<Generator>()
                .HasMany(g => g.DailyReadings)
                .WithOne(dr => dr.Generator)
                .HasForeignKey(dr => dr.GeneratorId)
                .OnDelete(DeleteBehavior.Restrict);


            // === Generator navigation ===

            // === Controller ===
            modelBuilder.Entity<Controller>()
                .HasOne(c => c.Generator)
                .WithOne(g => g.Controller)
                .HasForeignKey<Controller>(c => c.GeneratorId)
                .OnDelete(DeleteBehavior.SetNull);

            // === Meter ===
            modelBuilder.Entity<Meter>()
                .HasOne(m => m.SubStation)
                .WithMany(ss => ss.Meters)
                .HasForeignKey(m => m.SubStationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Meter>()
                .HasOne(m => m.Generator)
                .WithMany()
                .HasForeignKey(m => m.GeneratorId)
                .OnDelete(DeleteBehavior.SetNull);


            // === User ===
            modelBuilder.Entity<User>()
                .HasMany(u => u.Notifications)
                .WithOne(nr => nr.User)
                .HasForeignKey(nr => nr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // === Lookup ===
            modelBuilder.Entity<Lookup>()
                .HasIndex(l => new { l.CategoryId, l.Code })
                .IsUnique();

            // === Junction: UserScope ===
            modelBuilder.Entity<UserScope>()
                .HasOne(us => us.User)
                .WithMany(u => u.Scopes)
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserScope>()
                .HasOne(us => us.Branch)
                .WithMany()
                .HasForeignKey(us => us.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserScope>()
                .HasOne(us => us.Station)
                .WithMany()
                .HasForeignKey(us => us.StationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserScope>()
                .HasOne(us => us.SubStation)
                .WithMany()
                .HasForeignKey(us => us.SubStationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserScope>()
                .HasIndex(us => new { us.UserId, us.BranchId, us.StationId, us.SubStationId })
                .IsUnique();

            // === FuelTank ===
            modelBuilder.Entity<FuelTank>()
                .HasOne(t => t.FuelLookup)
                .WithMany()
                .HasForeignKey(t => t.FuelLookupId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FuelTank>()
                .HasIndex(t => new { t.SubStationId, t.FuelLookupId })
                .IsUnique();

            // === Ledger: DailyReading ===
            modelBuilder.Entity<DailyReading>()
                .HasIndex(dr => new { dr.GeneratorId, dr.ReadingDate })
                .IsUnique();

            modelBuilder.Entity<DailyReading>()
                .HasOne(dr => dr.FuelLookup)
                .WithMany()
                .HasForeignKey(dr => dr.FuelLookupId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DailyReading>()
                .HasOne(dr => dr.OilLookup)
                .WithMany()
                .HasForeignKey(dr => dr.OilLookupId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DailyReading>()
                .HasOne(dr => dr.ReadingStatusLookup)
                .WithMany()
                .HasForeignKey(dr => dr.ReadingStatusLookupId)
                .OnDelete(DeleteBehavior.Restrict);

            // Each reading records the (active) control panel it was entered against
            modelBuilder.Entity<DailyReading>()
                .HasOne(dr => dr.Controller)
                .WithMany()
                .HasForeignKey(dr => dr.ControllerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DailyReading>()
                .HasIndex(dr => dr.ControllerId)
                .HasDatabaseName("IX_DailyReading_ControllerId");





            // === Ledger: OilTransaction ===
            modelBuilder.Entity<OilTransaction>()
                .HasOne(ot => ot.SubStation)
                .WithMany()
                .HasForeignKey(ot => ot.SubStationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OilTransaction>()
                .HasOne(ot => ot.OilLookup)
                .WithMany()
                .HasForeignKey(ot => ot.OilLookupId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OilTransaction>()
                .HasIndex(ot => ot.TransactionDate);



            modelBuilder.Entity<OilTransaction>()
                .HasOne(ot => ot.TransactionTypeLookup)
                .WithMany()
                .HasForeignKey(ot => ot.TransactionTypeLookupId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OilTransaction>()
                .HasOne(ot => ot.SourceSubStation)
                .WithMany()
                .HasForeignKey(ot => ot.SourceSubStationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OilTransaction>()
                .HasOne(ot => ot.DestinationSubStation)
                .WithMany()
                .HasForeignKey(ot => ot.DestinationSubStationId)
                .OnDelete(DeleteBehavior.Restrict);

            // === Ledger: FuelTransaction ===
            modelBuilder.Entity<FuelTransaction>()
                .HasOne(ft => ft.SubStation)
                .WithMany()
                .HasForeignKey(ft => ft.SubStationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FuelTransaction>()
                .HasOne(ft => ft.FuelLookup)
                .WithMany()
                .HasForeignKey(ft => ft.FuelLookupId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FuelTransaction>()
                .HasIndex(ft => ft.TransactionDate);



            modelBuilder.Entity<FuelTransaction>()
                .HasOne(ft => ft.TransactionTypeLookup)
                .WithMany()
                .HasForeignKey(ft => ft.TransactionTypeLookupId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FuelTransaction>()
                .HasOne(ft => ft.SourceSubStation)
                .WithMany()
                .HasForeignKey(ft => ft.SourceSubStationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FuelTransaction>()
                .HasOne(ft => ft.DestinationSubStation)
                .WithMany()
                .HasForeignKey(ft => ft.DestinationSubStationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FuelTransaction>()
                .HasOne(ft => ft.Meter)
                .WithMany()
                .HasForeignKey(ft => ft.MeterId)
                .OnDelete(DeleteBehavior.SetNull);

            // === Ledger: PeriodicInventory ===
            modelBuilder.Entity<PeriodicInventory>()
                .HasOne(pi => pi.SubStation)
                .WithMany()
                .HasForeignKey(pi => pi.SubStationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PeriodicInventory>()
                .HasOne(pi => pi.ApprovedBy)
                .WithMany()
                .HasForeignKey(pi => pi.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PeriodicInventory>()
                .HasMany(pi => pi.FuelReadings)
                .WithOne(fr => fr.PeriodicInventory)
                .HasForeignKey(fr => fr.PeriodicInventoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PeriodicInventory>()
                .HasMany(pi => pi.OilReadings)
                .WithOne(or => or.PeriodicInventory)
                .HasForeignKey(or => or.PeriodicInventoryId)
                .OnDelete(DeleteBehavior.Restrict);



            // === Ledger: SubStationReading ===
            modelBuilder.Entity<SubStationReading>()
                .HasOne(sr => sr.SubStation)
                .WithMany()
                .HasForeignKey(sr => sr.SubStationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SubStationReading>()
                .HasOne(sr => sr.Meter)
                .WithMany()
                .HasForeignKey(sr => sr.MeterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SubStationReading>()
                .HasOne(sr => sr.ReadingStatusLookup)
                .WithMany()
                .HasForeignKey(sr => sr.ReadingStatusLookupId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SubStationReading>()
                .HasIndex(sr => new { sr.SubStationId, sr.ReadingDate })
                .IsUnique();

            // === Ledger: PeriodicInventoryFuel ===
            modelBuilder.Entity<PeriodicInventoryFuel>()
                .HasOne(fr => fr.PeriodicInventory)
                .WithMany(pi => pi.FuelReadings)
                .HasForeignKey(fr => fr.PeriodicInventoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PeriodicInventoryFuel>()
                .HasOne(fr => fr.FuelLookup)
                .WithMany()
                .HasForeignKey(fr => fr.FuelLookupId)
                .OnDelete(DeleteBehavior.Restrict);

            // === Ledger: PeriodicInventoryOil ===
            modelBuilder.Entity<PeriodicInventoryOil>()
                .HasOne(or => or.PeriodicInventory)
                .WithMany(pi => pi.OilReadings)
                .HasForeignKey(or => or.PeriodicInventoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PeriodicInventoryOil>()
                .HasOne(or => or.OilLookup)
                .WithMany()
                .HasForeignKey(or => or.OilLookupId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PeriodicInventoryOil>()
                .HasOne(or => or.Oil)
                .WithMany()
                .HasForeignKey(or => or.OilId)
                .OnDelete(DeleteBehavior.SetNull);

            // === Support: Notification ===
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.SubStation)
                .WithMany()
                .HasForeignKey(n => n.SubStationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Notification>()
                .HasMany(n => n.Recipients)
                .WithOne(nr => nr.Notification)
                .HasForeignKey(nr => nr.NotificationId)
                .OnDelete(DeleteBehavior.Restrict);

            // === Support: NotificationRecipient ===
            modelBuilder.Entity<NotificationRecipient>()
                .HasIndex(nr => new { nr.NotificationId, nr.UserId })
                .IsUnique();

            // === Support: AuditLog ===
            modelBuilder.Entity<AuditLog>()
                .HasIndex(al => al.Timestamp);

            // === Support: AnalyticsInsight ===
            modelBuilder.Entity<AnalyticsInsight>()
                .HasMany(ai => ai.Feedback)
                .WithOne(f => f.AnalyticsInsight)
                .HasForeignKey(f => f.AnalyticsInsightId)
                .OnDelete(DeleteBehavior.Restrict);

            // === Support: InsightFeedback ===
            modelBuilder.Entity<InsightFeedback>()
                .HasIndex(f => new { f.AnalyticsInsightId, f.UserId })
                .IsUnique();

            // === Support: RefreshToken ===
            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => rt.ExpiresAt);

            // === Support: LoginAttempt ===
            modelBuilder.Entity<LoginAttempt>()
                .HasIndex(la => la.AttemptedAt);

            // === Performance: Composite indexes for common query patterns ===
            modelBuilder.Entity<DailyReading>()
                .HasIndex(dr => new { dr.ReadingDate, dr.GeneratorId })
                .HasDatabaseName("IX_DailyReading_Date_Generator");

            modelBuilder.Entity<DailyReading>()
                .HasIndex(dr => dr.ReadingStatusLookupId)
                .HasDatabaseName("IX_DailyReading_Status");

            modelBuilder.Entity<FuelTransaction>()
                .HasIndex(ft => new { ft.SubStationId, ft.TransactionDate })
                .HasDatabaseName("IX_FuelTransaction_SubStation_Date");

            modelBuilder.Entity<OilTransaction>()
                .HasIndex(ot => new { ot.SubStationId, ot.TransactionDate })
                .HasDatabaseName("IX_OilTransaction_SubStation_Date");

            modelBuilder.Entity<PeriodicInventory>()
                .HasIndex(pi => new { pi.SubStationId, pi.PeriodTypeLookupId, pi.Year, pi.Month })
                .HasDatabaseName("IX_PeriodicInventory_SubStation_Period");

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.SubStationId, n.CreatedAt })
                .HasDatabaseName("IX_Notification_SubStation_Created");
            modelBuilder.Entity<Notification>()
                .Property(n => n.RelatedEntityType)
                .HasMaxLength(64);
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.NotificationType, n.RelatedEntityType, n.RelatedEntityId, n.CreatedAt })
                .HasDatabaseName("IX_Notifications_Alert_Deduplication");

            // === Critical missing indexes for frequent filters ===
            modelBuilder.Entity<Generator>()
                .HasIndex(g => g.SubStationId)
                .HasDatabaseName("IX_Generator_SubStationId");

            modelBuilder.Entity<SubStation>()
                .HasIndex(ss => ss.StationId)
                .HasDatabaseName("IX_SubStation_StationId");

            modelBuilder.Entity<Station>()
                .HasIndex(s => s.BranchId)
                .HasDatabaseName("IX_Station_BranchId");

            modelBuilder.Entity<Oil>()
                .HasIndex(o => o.SubStationId)
                .HasDatabaseName("IX_Oil_SubStationId");

            modelBuilder.Entity<Oil>()
                .HasIndex(o => o.StationId)
                .HasDatabaseName("IX_Oil_StationId");

            modelBuilder.Entity<Controller>()
                .HasIndex(c => c.GeneratorId)
                .HasDatabaseName("IX_Controller_GeneratorId");

            modelBuilder.Entity<Meter>()
                .HasIndex(m => m.SubStationId)
                .HasDatabaseName("IX_Meter_SubStationId");

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => rt.UserId)
                .HasDatabaseName("IX_RefreshToken_UserId");

            modelBuilder.Entity<AuditLog>()
                .HasIndex(al => al.EntityName)
                .HasDatabaseName("IX_AuditLog_EntityName");

            // === Stock Summary: Composite indexes for summary queries ===
            modelBuilder.Entity<FuelTransaction>()
                .HasIndex(ft => new { ft.SubStationId, ft.FuelLookupId, ft.TransactionDate, ft.IsApproved })
                .HasDatabaseName("IX_FuelTransactions_StockSummary")
                .HasFilter(activeFilter);

            modelBuilder.Entity<DailyReading>()
                .HasIndex(dr => new { dr.GeneratorId, dr.ReadingDate, dr.FuelLookupId })
                .HasDatabaseName("IX_DailyReadings_FuelConsumption")
                .HasFilter(activeFilter);

            modelBuilder.Entity<PeriodicInventory>()
                .HasIndex(pi => new { pi.SubStationId, pi.InventoryDate, pi.IsApproved })
                .HasDatabaseName("IX_PeriodicInventory_LastApproved")
                .HasFilter(approvedFilter);

            modelBuilder.Entity<PeriodicInventoryFuel>()
                .HasIndex(pif => new { pif.PeriodicInventoryId, pif.FuelLookupId })
                .HasDatabaseName("IX_PeriodicInventoryFuel_Lookup");

            modelBuilder.Entity<OilTransaction>()
                .HasIndex(ot => new { ot.SubStationId, ot.OilLookupId, ot.TransactionDate, ot.IsApproved })
                .HasDatabaseName("IX_OilTransactions_StockSummary")
                .HasFilter(activeFilter);

            // === Phase 3 Performance: Composite & Filtered Indexes for High-Frequency Queries ===
            modelBuilder.Entity<FuelTransaction>()
                .HasIndex(ft => new { ft.TransactionDate, ft.IsApproved })
                .HasDatabaseName("IX_FuelTransactions_Date_Approved")
                .HasFilter(activeFilter);

            modelBuilder.Entity<OilTransaction>()
                .HasIndex(ot => new { ot.TransactionDate, ot.IsApproved })
                .HasDatabaseName("IX_OilTransactions_Date_Approved")
                .HasFilter(activeFilter);

            modelBuilder.Entity<DailyReading>()
                .HasIndex(dr => new { dr.GeneratorId, dr.ReadingDate, dr.ReadingStatusLookupId })
                .HasDatabaseName("IX_DailyReadings_Gen_Date_Status")
                .HasFilter(activeFilter);

            modelBuilder.Entity<SubStationReading>()
                .HasIndex(sr => new { sr.ReadingDate, sr.ReadingStatusLookupId })
                .HasDatabaseName("IX_SubStationReadings_Date_Status")
                .HasFilter(activeFilter);

            modelBuilder.Entity<SubStationReading>()
                .HasIndex(sr => new { sr.SubStationId, sr.ReadingDate, sr.ReadingStatusLookupId })
                .HasDatabaseName("IX_SubStationReadings_SubStation_Date_Status")
                .HasFilter(activeFilter);

            modelBuilder.Entity<PeriodicInventory>()
                .HasIndex(pi => new { pi.SubStationId, pi.InventoryDate })
                .HasDatabaseName("IX_PeriodicInventories_SubStation_Date")
                .HasFilter(activeFilter);

            // === Archive: DailyReadingArchive ===
            modelBuilder.Entity<DailyReadingArchive>()
                .HasIndex(a => a.ArchivedAt)
                .HasDatabaseName("IX_DailyReadingArchive_ArchivedAt");

            modelBuilder.Entity<DailyReadingArchive>()
                .HasIndex(a => a.GeneratorId)
                .HasDatabaseName("IX_DailyReadingArchive_GeneratorId");

            // === Archive: AuditLogArchive ===
            modelBuilder.Entity<AuditLogArchive>()
                .HasIndex(a => a.ArchivedAt)
                .HasDatabaseName("IX_AuditLogArchive_ArchivedAt");

            modelBuilder.Entity<AuditLogArchive>()
                .HasIndex(a => a.EntityName)
                .HasDatabaseName("IX_AuditLogArchive_EntityName");

            modelBuilder.Entity<AuditLogArchive>()
                .HasIndex(a => a.Timestamp)
                .HasDatabaseName("IX_AuditLogArchive_Timestamp");

            modelBuilder.Entity<AuditLogArchive>()
                .HasIndex(a => a.Timestamp)
                .HasDatabaseName("IX_AuditLogArchive_Timestamp");

            // === Warehouse: SparePart ===
            modelBuilder.Entity<SparePart>()
                .HasOne(sp => sp.PartCategoryLookup)
                .WithMany()
                .HasForeignKey(sp => sp.PartCategoryLookupId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SparePart>()
                .HasOne(sp => sp.UnitLookup)
                .WithMany()
                .HasForeignKey(sp => sp.UnitLookupId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SparePart>()
                .HasOne(sp => sp.ReplacementPriorityLookup)
                .WithMany()
                .HasForeignKey(sp => sp.ReplacementPriorityLookupId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SparePart>()
                .HasIndex(sp => sp.PartNumber)
                .IsUnique()
                .HasFilter(activeFilter);

            // === Warehouse: WarehouseStock ===
            modelBuilder.Entity<WarehouseStock>()
                .HasOne(ws => ws.SparePart)
                .WithMany(sp => sp.Stocks)
                .HasForeignKey(ws => ws.SparePartId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WarehouseStock>()
                .HasOne(ws => ws.Station)
                .WithMany()
                .HasForeignKey(ws => ws.StationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WarehouseStock>()
                .HasIndex(ws => new { ws.SparePartId, ws.StationId })
                .IsUnique()
                .HasFilter(activeFilter);

            // === Warehouse: WarehouseSupply ===
            modelBuilder.Entity<WarehouseSupply>()
                .HasOne(ws => ws.Station)
                .WithMany()
                .HasForeignKey(ws => ws.StationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WarehouseSupply>()
                .HasOne(ws => ws.StatusLookup)
                .WithMany()
                .HasForeignKey(ws => ws.StatusLookupId)
                .OnDelete(DeleteBehavior.Restrict);

            // === Warehouse: WarehouseSupplyItem ===
            modelBuilder.Entity<WarehouseSupplyItem>()
                .HasOne(wsi => wsi.WarehouseSupply)
                .WithMany(ws => ws.Items)
                .HasForeignKey(wsi => wsi.WarehouseSupplyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WarehouseSupplyItem>()
                .HasOne(wsi => wsi.SparePart)
                .WithMany()
                .HasForeignKey(wsi => wsi.SparePartId)
                .OnDelete(DeleteBehavior.Restrict);

            // === Warehouse: DispatchRequest ===
            modelBuilder.Entity<DispatchRequest>()
                .HasOne(dr => dr.SubStation)
                .WithMany()
                .HasForeignKey(dr => dr.SubStationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DispatchRequest>()
                .HasOne(dr => dr.Station)
                .WithMany()
                .HasForeignKey(dr => dr.StationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DispatchRequest>()
                .HasOne(dr => dr.RequestedBy)
                .WithMany()
                .HasForeignKey(dr => dr.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DispatchRequest>()
                .HasOne(dr => dr.ApprovedBy)
                .WithMany()
                .HasForeignKey(dr => dr.ApprovedById)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DispatchRequest>()
                .HasOne(dr => dr.WorkPermit)
                .WithMany(wp => wp.DispatchRequests)
                .HasForeignKey(dr => dr.WorkPermitId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DispatchRequest>()
                .HasOne(dr => dr.StatusLookup)
                .WithMany()
                .HasForeignKey(dr => dr.StatusLookupId)
                .OnDelete(DeleteBehavior.Restrict);

            // === Warehouse: DispatchRequestItem ===
            modelBuilder.Entity<DispatchRequestItem>()
                .HasOne(dri => dri.DispatchRequest)
                .WithMany(dr => dr.Items)
                .HasForeignKey(dri => dri.DispatchRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DispatchRequestItem>()
                .HasOne(dri => dri.SparePart)
                .WithMany()
                .HasForeignKey(dri => dri.SparePartId)
                .OnDelete(DeleteBehavior.Restrict);

            // === Warehouse: WarehouseTransaction ===
            modelBuilder.Entity<WarehouseTransaction>()
                .HasOne(wt => wt.SparePart)
                .WithMany()
                .HasForeignKey(wt => wt.SparePartId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WarehouseTransaction>()
                .HasOne(wt => wt.Station)
                .WithMany()
                .HasForeignKey(wt => wt.StationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WarehouseTransaction>()
                .HasOne(wt => wt.TransactionTypeLookup)
                .WithMany()
                .HasForeignKey(wt => wt.TransactionTypeLookupId)
                .OnDelete(DeleteBehavior.Restrict);

            // === Maintenance: WorkPermit ===
            modelBuilder.Entity<WorkPermit>()
                .HasOne(wp => wp.SubStation)
                .WithMany()
                .HasForeignKey(wp => wp.SubStationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkPermit>()
                .HasOne(wp => wp.Generator)
                .WithMany()
                .HasForeignKey(wp => wp.GeneratorId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<WorkPermit>()
                .HasOne(wp => wp.MaintenanceTypeLookup)
                .WithMany()
                .HasForeignKey(wp => wp.MaintenanceTypeLookupId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkPermit>()
                .HasOne(wp => wp.DepartmentLookup)
                .WithMany()
                .HasForeignKey(wp => wp.DepartmentLookupId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkPermit>()
                .HasOne(wp => wp.PriorityLookup)
                .WithMany()
                .HasForeignKey(wp => wp.PriorityLookupId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkPermit>()
                .HasOne(wp => wp.StatusLookup)
                .WithMany()
                .HasForeignKey(wp => wp.StatusLookupId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkPermit>()
                .HasOne(wp => wp.RequestedBy)
                .WithMany()
                .HasForeignKey(wp => wp.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkPermit>()
                .HasOne(wp => wp.AssignedTo)
                .WithMany()
                .HasForeignKey(wp => wp.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkPermit>()
                .HasOne(wp => wp.ApprovedBy)
                .WithMany()
                .HasForeignKey(wp => wp.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            // === Maintenance: WorkTask ===
            modelBuilder.Entity<WorkTask>()
                .HasOne(wt => wt.WorkPermit)
                .WithMany(wp => wp.Tasks)
                .HasForeignKey(wt => wt.WorkPermitId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkTask>()
                .HasOne(wt => wt.CompletedBy)
                .WithMany()
                .HasForeignKey(wt => wt.CompletedById)
                .OnDelete(DeleteBehavior.SetNull);

            // === Maintenance: InstalledPart ===
            modelBuilder.Entity<InstalledPart>()
                .HasOne(ip => ip.Generator)
                .WithMany()
                .HasForeignKey(ip => ip.GeneratorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InstalledPart>()
                .HasOne(ip => ip.SparePart)
                .WithMany()
                .HasForeignKey(ip => ip.SparePartId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InstalledPart>()
                .HasOne(ip => ip.WorkPermit)
                .WithMany(wp => wp.InstalledParts)
                .HasForeignKey(ip => ip.WorkPermitId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<InstalledPart>()
                .HasOne(ip => ip.DispatchRequestItem)
                .WithMany(dri => dri.InstalledParts)
                .HasForeignKey(ip => ip.DispatchRequestItemId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<InstalledPart>()
                .HasIndex(ip => new { ip.GeneratorId, ip.IsActive })
                .HasDatabaseName("IX_InstalledPart_Generator_Active")
                .HasFilter(activeFilter);

            // === Maintenance: MaintenanceLog ===
            modelBuilder.Entity<MaintenanceLog>()
                .HasOne(ml => ml.WorkPermit)
                .WithMany(wp => wp.Logs)
                .HasForeignKey(ml => ml.WorkPermitId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MaintenanceLog>()
                .HasOne(ml => ml.Generator)
                .WithMany()
                .HasForeignKey(ml => ml.GeneratorId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<MaintenanceLog>()
                .HasOne(ml => ml.LoggedBy)
                .WithMany()
                .HasForeignKey(ml => ml.LoggedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MaintenanceLog>()
                .HasIndex(ml => new { ml.WorkPermitId, ml.LogDate })
                .HasDatabaseName("IX_MaintenanceLog_Permit_Date");

            // === Decimal precision ===
            modelBuilder.Entity<SparePart>()
                .Property(sp => sp.UnitPrice)
                .HasColumnType("decimal(18,4)");

            modelBuilder.Entity<WarehouseSupplyItem>()
                .Property(wsi => wsi.UnitPrice)
                .HasColumnType("decimal(18,4)");

            modelBuilder.Entity<WarehouseSupplyItem>()
                .Property(wsi => wsi.TotalPrice)
                .HasColumnType("decimal(18,4)");

            modelBuilder.Entity<WarehouseStock>()
                .Property(ws => ws.CurrentQuantity)
                .HasColumnType("decimal(18,3)");

            modelBuilder.Entity<WarehouseStock>()
                .Property(ws => ws.ReservedQuantity)
                .HasColumnType("decimal(18,3)");

            modelBuilder.Entity<WarehouseTransaction>()
                .Property(wt => wt.Quantity)
                .HasColumnType("decimal(18,3)");

            modelBuilder.Entity<WarehouseTransaction>()
                .Property(wt => wt.BalanceBefore)
                .HasColumnType("decimal(18,3)");

            modelBuilder.Entity<WarehouseTransaction>()
                .Property(wt => wt.BalanceAfter)
                .HasColumnType("decimal(18,3)");

            // === Performance: Composite indexes for warehouse queries ===
            modelBuilder.Entity<WarehouseSupply>()
                .HasIndex(ws => new { ws.StationId, ws.SupplyDate })
                .HasDatabaseName("IX_WarehouseSupply_Station_Date");

            modelBuilder.Entity<DispatchRequest>()
                .HasIndex(dr => new { dr.SubStationId, dr.RequestDate })
                .HasDatabaseName("IX_DispatchRequest_SubStation_Date");

            modelBuilder.Entity<DispatchRequest>()
                .HasIndex(dr => new { dr.StationId, dr.StatusLookupId })
                .HasDatabaseName("IX_DispatchRequest_Station_Status");

            modelBuilder.Entity<WorkPermit>()
                .HasIndex(wp => new { wp.SubStationId, wp.PermitDate })
                .HasDatabaseName("IX_WorkPermit_SubStation_Date");

            modelBuilder.Entity<WorkPermit>()
                .HasIndex(wp => new { wp.StatusLookupId, wp.PriorityLookupId })
                .HasDatabaseName("IX_WorkPermit_Status_Priority");

            // === Seed: ActiveStatus ===
            var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 1,
                Name = "ActiveStatus",
                DisplayName = "Active Status",
                Description = "Active/Inactive status for soft-delete entities",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup
                {
                    Id = 1,
                    CategoryId = 1,
                    Name = "فعال",
                    Code = "Active",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 2,
                    CategoryId = 1,
                    Name = "غير فعال",
                    Code = "Inactive",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                });

            // === Seed: Voltage (الفولتية) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 2,
                Name = "Voltage",
                DisplayName = "الفولتية",
                Description = "Voltage ratings: 400V, 11KV, 33KV",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup
                {
                    Id = 3,
                    CategoryId = 2,
                    Name = "400V",
                    Code = "400V",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 4,
                    CategoryId = 2,
                    Name = "11KV",
                    Code = "11KV",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 5,
                    CategoryId = 2,
                    Name = "33KV",
                    Code = "33KV",
                    SortOrder = 3,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                });

            // === Seed: Frequency (التردد) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 3,
                Name = "Frequency",
                DisplayName = "التردد",
                Description = "Frequency: 50Hz, 60Hz",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup
                {
                    Id = 6,
                    CategoryId = 3,
                    Name = "50Hz",
                    Code = "50Hz",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 7,
                    CategoryId = 3,
                    Name = "60Hz",
                    Code = "60Hz",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                });

            // === Seed: GeneratorType (انواع المولدات) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 4,
                Name = "GeneratorType",
                DisplayName = "انواع المولدات",
                Description = "Generator operating modes: PRIME, CONTINUOUS, STANDBY",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup
                {
                    Id = 8,
                    CategoryId = 4,
                    Name = "PRIME",
                    Code = "PRIME",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 9,
                    CategoryId = 4,
                    Name = "CONTINUOUS",
                    Code = "CONTINUOUS",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 10,
                    CategoryId = 4,
                    Name = "STANDBY",
                    Code = "STANDBY",
                    SortOrder = 3,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                });

            // === Seed: OwnershipType (نوع الملكية) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 5,
                Name = "OwnershipType",
                DisplayName = "نوع الملكية",
                Description = "Ownership: Owner (ملك), Tenant (مستأجر)",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup
                {
                    Id = 11,
                    CategoryId = 5,
                    Name = "ملك",
                    Code = "Owner",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 12,
                    CategoryId = 5,
                    Name = "مستأجر",
                    Code = "Tenant",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                });

            // === Seed: GeneratorStatus (حالة المولد) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 6,
                Name = "GeneratorStatus",
                DisplayName = "حالة المولد",
                Description = "Generator status: Running, Stopped, Under Maintenance",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup
                {
                    Id = 13,
                    CategoryId = 6,
                    Name = "شغال",
                    Code = "Running",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 14,
                    CategoryId = 6,
                    Name = "واقف",
                    Code = "Stopped",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 15,
                    CategoryId = 6,
                    Name = "تحت الصيانة",
                    Code = "UnderMaintenance",
                    SortOrder = 3,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                });

            // === Seed: MeterType (انواع العدادات) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 7,
                Name = "MeterType",
                DisplayName = "انواع العدادات",
                Description = "Meter types: Fuel Drain, Export Energy, Generator",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup
                {
                    Id = 16,
                    CategoryId = 7,
                    Name = "عداد وقود",
                    Code = "FuelDrain",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 17,
                    CategoryId = 7,
                    Name = "عداد طاقة",
                    Code = "ExportEnergy",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 18,
                    CategoryId = 7,
                    Name = "عداد مولدات",
                    Code = "GeneratorMeter",
                    SortOrder = 3,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                });

            // === Seed: FuelType (انواع الوقود) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 8,
                Name = "FuelType",
                DisplayName = "انواع الوقود",
                Description = "Fuel types: Diesel, Mazout",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup
                {
                    Id = 19,
                    CategoryId = 8,
                    Name = "ديزل",
                    Code = "Diesel",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 20,
                    CategoryId = 8,
                    Name = "مازوت",
                    Code = "Mazout",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 54,
                    CategoryId = 8,
                    Name = "بنزين",
                    Code = "Petrol",
                    SortOrder = 3,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                });

            // === Seed: OilType (انواع الزيوت) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 9,
                Name = "OilType",
                DisplayName = "انواع الزيوت",
                Description = "Oil types: LOCK OIL, Engine Oil SAE 15W-40, GOLF, MOBIL, TOTAL",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup
                {
                    Id = 21,
                    CategoryId = 9,
                    Name = "LOCK OIL",
                    Code = "LockOil",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 22,
                    CategoryId = 9,
                    Name = "Engine Oil SAE 15W-40",
                    Code = "EngineOil15W40",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 23,
                    CategoryId = 9,
                    Name = "GOLF",
                    Code = "Golf",
                    SortOrder = 3,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 24,
                    CategoryId = 9,
                    Name = "MOBIL",
                    Code = "Mobil",
                    SortOrder = 4,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 25,
                    CategoryId = 9,
                    Name = "TOTAL",
                    Code = "Total",
                    SortOrder = 5,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                });

            // === Seed: SystemRole (الادوار الوظيفية) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 10,
                Name = "SystemRole",
                DisplayName = "الأدوار الوظيفية",
                Description = "System roles: SystemAdmin, Manager, Supervisor, FuelOfficer, Operator, Visitor",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup
                {
                    Id = 26,
                    CategoryId = 10,
                    Name = "مسئول النظام",
                    Code = "SystemAdmin",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 27,
                    CategoryId = 10,
                    Name = "مدير",
                    Code = "Manager",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 28,
                    CategoryId = 10,
                    Name = "مشرف",
                    Code = "Supervisor",
                    SortOrder = 3,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 29,
                    CategoryId = 10,
                    Name = "مسئول الوقود",
                    Code = "FuelOfficer",
                    SortOrder = 4,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 30,
                    CategoryId = 10,
                    Name = "مشغل",
                    Code = "Operator",
                    SortOrder = 5,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 31,
                    CategoryId = 10,
                    Name = "زائر",
                    Code = "Visitor",
                    SortOrder = 6,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                });

            // === Seed: PeriodType (نوع الفترة) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 11,
                Name = "PeriodType",
                DisplayName = "نوع الفترة",
                Description = "Inventory period type: First Half, Second Half",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup
                {
                    Id = 32,
                    CategoryId = 11,
                    Name = "النصف الأول",
                    Code = "FirstHalf",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 33,
                    CategoryId = 11,
                    Name = "النصف الثاني",
                    Code = "SecondHalf",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                });

            // === Seed: ShiftDuration (مدة الوردية) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 12,
                Name = "ShiftDuration",
                DisplayName = "مدة الوردية",
                Description = "Shift duration in hours: 8, 12, 24",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup
                {
                    Id = 34,
                    CategoryId = 12,
                    Name = "8 ساعات",
                    Code = "8",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 35,
                    CategoryId = 12,
                    Name = "12 ساعة",
                    Code = "12",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 36,
                    CategoryId = 12,
                    Name = "24 ساعة",
                    Code = "24",
                    SortOrder = 3,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                });

            // === Seed: ShiftType (نوع الوردية) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 13,
                Name = "ShiftType",
                DisplayName = "نوع الوردية",
                Description = "Shift type: Morning, Night",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup
                {
                    Id = 37,
                    CategoryId = 13,
                    Name = "صباحي",
                    Code = "Morning",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 38,
                    CategoryId = 13,
                    Name = "ليلي",
                    Code = "Night",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                });

            // === Seed: ControllerModel (موديل الميتر) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 14,
                Name = "ControllerModel",
                DisplayName = "موديل الميتر",
                Description = "Controller/Display model brands",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup
                {
                    Id = 39,
                    CategoryId = 14,
                    Name = "SCT",
                    Code = "SCT",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 40,
                    CategoryId = 14,
                    Name = "ComAp",
                    Code = "ComAp",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 41,
                    CategoryId = 14,
                    Name = "Deep Sea",
                    Code = "DeepSea",
                    SortOrder = 3,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 42,
                    CategoryId = 14,
                    Name = "Densei",
                    Code = "Densei",
                    SortOrder = 4,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 43,
                    CategoryId = 14,
                    Name = "Lovato",
                    Code = "Lovato",
                    SortOrder = 5,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                });

            // === Seed: RegisterType (نوع العداد) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 15,
                Name = "RegisterType",
                DisplayName = "نوع العداد",
                Description = "Register type: KWh, KVarh, KVAh",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup
                {
                    Id = 44,
                    CategoryId = 15,
                    Name = "kWh",
                    Code = "KWh",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 45,
                    CategoryId = 15,
                    Name = "kVarh",
                    Code = "KVarh",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 46,
                    CategoryId = 15,
                    Name = "kVAh",
                    Code = "KVAh",
                    SortOrder = 3,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                });

            // === Seed: EnergyUnit (وحدة الطاقة) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 16,
                Name = "EnergyUnit",
                DisplayName = "وحدة الطاقة",
                Description = "Energy unit: Watt-hour, Joule",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup
                {
                    Id = 47,
                    CategoryId = 16,
                    Name = "Wh",
                    Code = "WattHour",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 48,
                    CategoryId = 16,
                    Name = "J",
                    Code = "Joule",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                });

            // === Seed: SubStationStatus (حالة المحطة الفرعية) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 17,
                Name = "SubStationStatus",
                DisplayName = "حالة المحطة الفرعية",
                Description = "Sub-station status: Active, Under Maintenance, Decommissioned",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup
                {
                    Id = 49,
                    CategoryId = 17,
                    Name = "نشطة",
                    Code = "Active",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 50,
                    CategoryId = 17,
                    Name = "تحت الصيانة",
                    Code = "UnderMaintenance",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 51,
                    CategoryId = 17,
                    Name = "متوقفة",
                    Code = "Decommissioned",
                    SortOrder = 3,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                });

            // === Seed: ItemType (نوع الصنف) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 18,
                Name = "ItemType",
                DisplayName = "نوع الصنف",
                Description = "Item type for reports: Fuel, Oil",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup
                {
                    Id = 52,
                    CategoryId = 18,
                    Name = "وقود",
                    Code = "Fuel",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 53,
                    CategoryId = 18,
                    Name = "زيت",
                    Code = "Oil",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                });

             // === Seed: ReadingStatus (حالة الموقع) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 19,
                Name = "ReadingStatus",
                DisplayName = "حالة الموقع",
                Description = "Reading status: Draft, Pending, Submitted, Approved, Rejected",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup
                {
                    Id = 55,
                    CategoryId = 19,
                    Name = "مسودة",
                    Code = "Draft",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 56,
                    CategoryId = 19,
                    Name = "معلق",
                    Code = "Pending",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 57,
                    CategoryId = 19,
                    Name = "مرسل",
                    Code = "Submitted",
                    SortOrder = 3,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 58,
                    CategoryId = 19,
                    Name = "معتمد",
                    Code = "Approved",
                    SortOrder = 4,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 59,
                    CategoryId = 19,
                    Name = "مرفوض",
                    Code = "Rejected",
                    SortOrder = 5,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                });

            // === Seed: TransactionType (انواع الحركات) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 100,
                Name = "TransactionType",
                DisplayName = "انواع الحركات",
                Description = "Transaction types for fuel and oil movements",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup
                {
                    Id = 200,
                    CategoryId = 100,
                    Name = "توريد",
                    Code = "Purchase",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 201,
                    CategoryId = 100,
                    Name = "صادر",
                    Code = "Sale",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 202,
                    CategoryId = 100,
                    Name = "نقل",
                    Code = "Transfer",
                    SortOrder = 3,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 203,
                    CategoryId = 100,
                    Name = "نقل صادر",
                    Code = "TransferOut",
                    SortOrder = 4,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 204,
                    CategoryId = 100,
                    Name = "نقل وارد",
                    Code = "TransferIn",
                    SortOrder = 5,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 205,
                    CategoryId = 100,
                    Name = "فاقد",
                    Code = "Loss",
                    SortOrder = 6,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 206,
                    CategoryId = 100,
                    Name = "استهلاك",
                    Code = "Consumption",
                    SortOrder = 7,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                },
                new Lookup
                {
                    Id = 207,
                    CategoryId = 100,
                    Name = "تسوية",
                    Code = "Adjustment",
                    SortOrder = 8,
                    IsActive = true,
                    CreatedAt = seedDate,
                    CreatedBy = 0,
                    UpdatedAt = seedDate,
                    UpdatedBy = 0,
                });

            // === Seed: PartCategory (فئة القطعة) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 20,
                Name = "PartCategory",
                DisplayName = "فئة القطعة",
                Description = "Spare part categories: Mechanical, Electrical, Electronic, Instrumentation, Workshop, Other",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup { Id = 60, CategoryId = 20, Name = "ميكانيكي", Code = "Mechanical", SortOrder = 1, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 61, CategoryId = 20, Name = "كهربائي", Code = "Electrical", SortOrder = 2, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 62, CategoryId = 20, Name = "إلكتروني", Code = "Electronic", SortOrder = 3, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 63, CategoryId = 20, Name = "أجهزة قياس", Code = "Instrumentation", SortOrder = 4, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 64, CategoryId = 20, Name = "ورشة", Code = "Workshop", SortOrder = 5, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 65, CategoryId = 20, Name = "أخرى", Code = "Other", SortOrder = 6, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 });

            // === Seed: PartUnit (وحدة القطعة) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 21,
                Name = "PartUnit",
                DisplayName = "وحدة القطعة",
                Description = "Spare part units: Piece, Liter, Kilogram, Meter, Set, Pack",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup { Id = 66, CategoryId = 21, Name = "قطعة", Code = "Piece", SortOrder = 1, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 67, CategoryId = 21, Name = "لتر", Code = "Liter", SortOrder = 2, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 68, CategoryId = 21, Name = "كيلوغرام", Code = "Kilogram", SortOrder = 3, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 69, CategoryId = 21, Name = "متر", Code = "Meter", SortOrder = 4, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 70, CategoryId = 21, Name = "طقم", Code = "Set", SortOrder = 5, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 71, CategoryId = 21, Name = "علبة", Code = "Pack", SortOrder = 6, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 });

            // === Seed: MaintenanceType (نوع الصيانة) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 22,
                Name = "MaintenanceType",
                DisplayName = "نوع الصيانة",
                Description = "Maintenance types: Routine, Preventive, Scheduled, HalfLife, FullLife, Inspection, Emergency",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup { Id = 72, CategoryId = 22, Name = "روتيني", Code = "Routine", SortOrder = 1, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 73, CategoryId = 22, Name = "وقائي", Code = "Preventive", SortOrder = 2, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 74, CategoryId = 22, Name = "مبرمج", Code = "Scheduled", SortOrder = 3, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 75, CategoryId = 22, Name = "نصف عمرية", Code = "HalfLife", SortOrder = 4, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 76, CategoryId = 22, Name = "عمرية", Code = "FullLife", SortOrder = 5, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 77, CategoryId = 22, Name = "تفقدية", Code = "Inspection", SortOrder = 6, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 78, CategoryId = 22, Name = "طارئة", Code = "Emergency", SortOrder = 7, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 });

            // === Seed: MaintenanceDepartment (القسم المكلف) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 23,
                Name = "MaintenanceDepartment",
                DisplayName = "القسم المكلف",
                Description = "Maintenance departments: All, Mechanical, Electrical, Instrumentation, Workshop",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup { Id = 79, CategoryId = 23, Name = "الكل", Code = "All", SortOrder = 1, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 80, CategoryId = 23, Name = "ميكانيكي", Code = "Mechanical", SortOrder = 2, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 81, CategoryId = 23, Name = "كهربائي", Code = "Electrical", SortOrder = 3, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 82, CategoryId = 23, Name = "أجهزة", Code = "Instrumentation", SortOrder = 4, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 83, CategoryId = 23, Name = "ورشة", Code = "Workshop", SortOrder = 5, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 });

            // === Seed: MaintenancePriority (أولوية الصيانة) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 24,
                Name = "MaintenancePriority",
                DisplayName = "أولوية الصيانة",
                Description = "Maintenance priority: Critical, Emergency, Normal, Low",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup { Id = 84, CategoryId = 24, Name = "حرجة", Code = "Critical", SortOrder = 1, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 85, CategoryId = 24, Name = "اضطراري", Code = "Emergency", SortOrder = 2, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 86, CategoryId = 24, Name = "عادية", Code = "Normal", SortOrder = 3, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 87, CategoryId = 24, Name = "منخفضة", Code = "Low", SortOrder = 4, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 });

            // === Seed: MaintenanceStatus (حالة التصريح) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 25,
                Name = "MaintenanceStatus",
                DisplayName = "حالة التصريح",
                Description = "Work permit status: Draft, Submitted, Active, Completed, Approved, Rejected, Cancelled",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup { Id = 88, CategoryId = 25, Name = "مسودة", Code = "Draft", SortOrder = 1, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 89, CategoryId = 25, Name = "مقدم", Code = "Submitted", SortOrder = 2, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 90, CategoryId = 25, Name = "نشط", Code = "Active", SortOrder = 3, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 91, CategoryId = 25, Name = "مكتمل", Code = "Completed", SortOrder = 4, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 92, CategoryId = 25, Name = "معتمد", Code = "Approved", SortOrder = 5, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 93, CategoryId = 25, Name = "مرفوض", Code = "Rejected", SortOrder = 6, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 94, CategoryId = 25, Name = "ملغى", Code = "Cancelled", SortOrder = 7, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 });

            // === Seed: DispatchStatus (حالة طلب الصرف) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 26,
                Name = "DispatchStatus",
                DisplayName = "حالة طلب الصرف",
                Description = "Dispatch request status: Pending, Approved, PartialApproved, Rejected, Issued, Completed",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup { Id = 95, CategoryId = 26, Name = "قيد الانتظار", Code = "Pending", SortOrder = 1, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 96, CategoryId = 26, Name = "معتمد", Code = "Approved", SortOrder = 2, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 97, CategoryId = 26, Name = "موافقة جزئية", Code = "PartialApproved", SortOrder = 3, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 98, CategoryId = 26, Name = "مرفوض", Code = "Rejected", SortOrder = 4, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 99, CategoryId = 26, Name = "تم الصرف", Code = "Issued", SortOrder = 5, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 100, CategoryId = 26, Name = "مكتمل", Code = "Completed", SortOrder = 6, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 });

            // === Seed: WarehouseSupplyStatus (حالة سند التوريد) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 27,
                Name = "WarehouseSupplyStatus",
                DisplayName = "حالة سند التوريد",
                Description = "Warehouse supply voucher status: Draft, Completed",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup { Id = 101, CategoryId = 27, Name = "مسودة", Code = "Draft", SortOrder = 1, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 102, CategoryId = 27, Name = "مكتمل", Code = "Completed", SortOrder = 2, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 });

            // === Seed: WarehouseTransactionType (نوع حركة المخزون) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 28,
                Name = "WarehouseTransactionType",
                DisplayName = "نوع حركة المخزون",
                Description = "Warehouse transaction types: Supply, Dispatch, Return, Adjustment, Transfer",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup { Id = 103, CategoryId = 28, Name = "توريد", Code = "Supply", SortOrder = 1, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 104, CategoryId = 28, Name = "صرف", Code = "Dispatch", SortOrder = 2, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 105, CategoryId = 28, Name = "إرجاع", Code = "Return", SortOrder = 3, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 106, CategoryId = 28, Name = "تسوية", Code = "Adjustment", SortOrder = 4, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 107, CategoryId = 28, Name = "نقل", Code = "Transfer", SortOrder = 5, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 });

            // === Seed: ReplacementPriority (أهمية الاستبدال) ===
            modelBuilder.Entity<LookupCategory>().HasData(new LookupCategory
            {
                Id = 29,
                Name = "ReplacementPriority",
                DisplayName = "أهمية الاستبدال",
                Description = "Replacement priority: Critical, High, Medium, Low",
                IsActive = true,
                CreatedAt = seedDate,
                CreatedBy = 0,
                UpdatedAt = seedDate,
                UpdatedBy = 0,
            });

            modelBuilder.Entity<Lookup>().HasData(
                new Lookup { Id = 108, CategoryId = 29, Name = "حرجة", Code = "Critical", SortOrder = 1, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 109, CategoryId = 29, Name = "عالية", Code = "High", SortOrder = 2, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 110, CategoryId = 29, Name = "متوسطة", Code = "Medium", SortOrder = 3, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 },
                new Lookup { Id = 111, CategoryId = 29, Name = "منخفضة", Code = "Low", SortOrder = 4, IsActive = true, CreatedAt = seedDate, CreatedBy = 0, UpdatedAt = seedDate, UpdatedBy = 0 });

            // === Operational foundations ===
            modelBuilder.Entity<StationOperationalSettings>(entity =>
            {
                entity.HasIndex(e => e.StationId)
                    .IsUnique()
                    .HasDatabaseName("UX_StationOperationalSettings_Station")
                    .HasFilter(activeFilter);
                entity.Property(e => e.TimeZone).HasMaxLength(80);
                entity.Property(e => e.OilPressureUnit).HasMaxLength(16);
                entity.Property(e => e.ProducedVsSentWarningThresholdPercent).HasPrecision(5, 2);
            });

            modelBuilder.Entity<Attachment>(entity =>
            {
                entity.HasIndex(e => e.StorageKey)
                    .IsUnique()
                    .HasDatabaseName("UX_Attachments_StorageKey")
                    .HasFilter(activeFilter);
                entity.Property(e => e.StorageKey).HasMaxLength(500);
                entity.Property(e => e.OriginalFileName).HasMaxLength(260);
                entity.Property(e => e.ContentType).HasMaxLength(100);
                entity.Property(e => e.Sha256).HasMaxLength(64);
            });

            modelBuilder.Entity<AttachmentLink>(entity =>
            {
                entity.HasIndex(e => new { e.EntityType, e.EntityId, e.SortOrder })
                    .HasDatabaseName("IX_AttachmentLinks_Entity_Sort")
                    .HasFilter(activeFilter);
                entity.Property(e => e.EntityType).HasMaxLength(100);
            });

            modelBuilder.Entity<CustomerFeatureSetting>(entity =>
            {
                entity.HasIndex(e => e.Key)
                    .IsUnique()
                    .HasDatabaseName("UX_CustomerFeatureSettings_Key")
                    .HasFilter(activeFilter);
                entity.Property(e => e.Key).HasMaxLength(100);
                entity.Property(e => e.Value).HasMaxLength(500);
            });

            modelBuilder.Entity<IdempotencyRequest>(entity =>
            {
                entity.HasIndex(e => new { e.TenantId, e.UserId, e.ClientRequestId })
                    .IsUnique()
                    .HasDatabaseName("UX_IdempotencyRequests_Tenant_User_ClientRequestId")
                    .HasFilter(activeFilter);
                entity.HasIndex(e => new { e.Operation, e.CreatedAt })
                    .HasDatabaseName("IX_IdempotencyRequests_Operation_Created");
                entity.Property(e => e.TenantId).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.Operation).HasMaxLength(100);
                entity.Property(e => e.RequestHash).HasMaxLength(64);
                entity.Property(e => e.Status).HasMaxLength(32);
                entity.Property(e => e.ResponsePayload).HasMaxLength(8000);
            });

            // === Operational capture ===
            // Rejected records are excluded deliberately: Phase 3 may permit a
            // corrected recapture at the same instant without mutating raw history.
            var activeNonRejectedOperationalFilter = Database.IsNpgsql()
                ? "\"DeletedAt\" IS NULL AND \"Status\" NOT IN (2, 4)"
                : "[DeletedAt] IS NULL AND [Status] NOT IN (2, 4)";

            modelBuilder.Entity<OperationalSubmission>(entity =>
            {
                entity.HasIndex(x => new { x.SubStationId, x.CreatedBy, x.ClientRequestId }).IsUnique()
                    .HasDatabaseName("UX_OperationalSubmissions_Site_User_Request")
                    .HasFilter(activeFilter);
                entity.HasIndex(x => new { x.SubStationId, x.ReceivedAtUtc })
                    .HasDatabaseName("IX_OperationalSubmissions_Site_Received");
            });

            modelBuilder.Entity<OperationalGeneratorReading>(entity =>
            {
                entity.HasIndex(x => new { x.GeneratorId, x.CapturedAtUtc }).IsUnique()
                    .HasDatabaseName("UX_OperationalGeneratorReadings_Generator_Captured")
                    .HasFilter(activeNonRejectedOperationalFilter);
                entity.HasIndex(x => new { x.GeneratorId, x.BusinessDate, x.Status })
                    .HasDatabaseName("IX_OperationalGeneratorReadings_Generator_Date_Status");
                entity.Property(x => x.WarningsJson).HasMaxLength(2000);
                entity.Property(x => x.LineVoltageVolts).HasPrecision(18, 4);
                entity.Property(x => x.CurrentAmps).HasPrecision(18, 4);
                entity.Property(x => x.WaterTemperatureCelsius).HasPrecision(18, 4);
                entity.Property(x => x.OilPressure).HasPrecision(18, 4);
                entity.Property(x => x.OilTemperatureCelsius).HasPrecision(18, 4);
                entity.Property(x => x.ActiveLoadKw).HasPrecision(18, 4);
                entity.Property(x => x.ApparentLoadKva).HasPrecision(18, 4);
                entity.Property(x => x.ReactiveLoadKvar).HasPrecision(18, 4);
                entity.Property(x => x.FrequencyHz).HasPrecision(18, 4);
                entity.Property(x => x.OilAddedLitres).HasPrecision(18, 4);
                entity.Property(x => x.PowerFactor).HasPrecision(9, 6);
                entity.Property(x => x.PeakLoadKw).HasPrecision(18, 4);
                entity.Property(x => x.AirPressureBar).HasPrecision(18, 4);
                entity.HasIndex(x => x.ClientLineId).IsUnique().HasDatabaseName("UX_OperationalGeneratorReadings_ClientLine")
                    .HasFilter(activeFilter);
            });

            modelBuilder.Entity<TankLevelReading>(entity =>
            {
                entity.HasIndex(x => new { x.FuelTankId, x.CapturedAtUtc }).IsUnique()
                    .HasDatabaseName("UX_TankLevelReadings_Tank_Captured")
                    .HasFilter(activeNonRejectedOperationalFilter);
                entity.HasIndex(x => new { x.FuelTankId, x.BusinessDate, x.Status })
                    .HasDatabaseName("IX_TankLevelReadings_Tank_Date_Status");
                entity.Property(x => x.WarningsJson).HasMaxLength(2000);
                entity.Property(x => x.LiquidLevelCm).HasPrecision(18, 4);
                entity.Property(x => x.ComputedLitres).HasPrecision(18, 4);
                entity.Property(x => x.DiameterCmSnapshot).HasPrecision(18, 4);
                entity.Property(x => x.CylinderHeightCmSnapshot).HasPrecision(18, 4);
                entity.Property(x => x.ConeHeightCmSnapshot).HasPrecision(18, 4);
                entity.Property(x => x.MultiplicationFactorSnapshot).HasPrecision(18, 6);
                entity.Property(x => x.BeforeLiquidLevelCm).HasPrecision(18, 4);
                entity.Property(x => x.BeforeComputedLitres).HasPrecision(18, 4);
                entity.Property(x => x.DeliveryNoteLitres).HasPrecision(18, 4);
                entity.Property(x => x.DeadStockLitresSnapshot).HasPrecision(18, 4);
                entity.HasIndex(x => x.ClientLineId).IsUnique().HasDatabaseName("UX_TankLevelReadings_ClientLine")
                    .HasFilter(activeFilter);
            });

            modelBuilder.Entity<Feeder>(entity =>
            {
                entity.HasIndex(x => new { x.SubStationId, x.Code }).IsUnique()
                    .HasDatabaseName("UX_Feeders_Site_Code").HasFilter(activeFilter);
                entity.Property(x => x.Code).HasMaxLength(80);
                entity.Property(x => x.Name).HasMaxLength(200);
                entity.Property(x => x.MeterMultiplier).HasPrecision(18, 6);
            });

            modelBuilder.Entity<FeederLoadReading>(entity =>
            {
                entity.HasIndex(x => new { x.FeederId, x.CapturedAtUtc }).IsUnique()
                    .HasDatabaseName("UX_FeederLoadReadings_Feeder_Captured")
                    .HasFilter(activeNonRejectedOperationalFilter);
                entity.HasIndex(x => new { x.FeederId, x.BusinessDate, x.Status })
                    .HasDatabaseName("IX_FeederLoadReadings_Feeder_Date_Status");
                entity.Property(x => x.WarningsJson).HasMaxLength(2000);
                entity.Property(x => x.ActiveLoadKw).HasPrecision(18, 4);
                entity.Property(x => x.ApparentLoadKva).HasPrecision(18, 4);
                entity.Property(x => x.MeterMultiplierSnapshot).HasPrecision(18, 6);
                entity.HasIndex(x => x.ClientLineId).IsUnique().HasDatabaseName("UX_FeederLoadReadings_ClientLine")
                    .HasFilter(activeFilter);
            });

            modelBuilder.Entity<MeterReading>(entity =>
            {
                entity.HasIndex(x => new { x.SubjectType, x.SubjectId, x.CapturedAtUtc }).IsUnique()
                    .HasDatabaseName("UX_MeterReadings_Subject_Captured")
                    .HasFilter(activeNonRejectedOperationalFilter);
                entity.HasIndex(x => new { x.SubjectType, x.SubjectId, x.BusinessDate, x.Kind }).IsUnique()
                    .HasDatabaseName("UX_MeterReadings_DailyClosing")
                    .HasFilter(Database.IsNpgsql()
                        ? "\"DeletedAt\" IS NULL AND \"Status\" <> 2 AND \"Kind\" = 1"
                        : "[DeletedAt] IS NULL AND [Status] <> 2 AND [Kind] = 1");
                entity.Property(x => x.ResetNote).HasMaxLength(1000);
                entity.Property(x => x.WarningsJson).HasMaxLength(2000);
                entity.Property(x => x.RawValue).HasPrecision(18, 4);
                entity.Property(x => x.MultiplierSnapshot).HasPrecision(18, 6);
                entity.HasIndex(x => x.ClientLineId).IsUnique().HasDatabaseName("UX_MeterReadings_ClientLine")
                    .HasFilter(activeFilter);
            });

            modelBuilder.Entity<FuelTank>(entity =>
            {
                entity.Property(x => x.DiameterCm).HasPrecision(18, 4);
                entity.Property(x => x.CylinderHeightCm).HasPrecision(18, 4);
                entity.Property(x => x.ConeHeightCm).HasPrecision(18, 4);
                entity.Property(x => x.MultiplicationFactor).HasPrecision(18, 6);
                entity.Property(x => x.ComputedCapacityLitres).HasPrecision(18, 4);
                entity.HasIndex(x => x.GeneratorId).IsUnique().HasDatabaseName("UX_FuelTanks_ActiveDailyGenerator")
                    .HasFilter(Database.IsNpgsql() ? "\"DeletedAt\" IS NULL AND \"GeneratorId\" IS NOT NULL" : "[DeletedAt] IS NULL AND [GeneratorId] IS NOT NULL");
            });

            modelBuilder.Entity<GeneratorOperatingLimit>(entity =>
            {
                entity.HasIndex(x => new { x.GeneratorId, x.Parameter }).IsUnique()
                    .HasDatabaseName("UX_GeneratorOperatingLimits_Generator_Parameter").HasFilter(activeFilter);
                entity.Property(x => x.Minimum).HasPrecision(18, 4);
                entity.Property(x => x.Maximum).HasPrecision(18, 4);
            });
            modelBuilder.Entity<OverloadEvent>(entity =>
            {
                entity.HasIndex(x => new { x.GeneratorId, x.ResolvedAtUtc })
                    .HasDatabaseName("IX_OverloadEvents_Generator_Open");
                entity.Property(x => x.PeakPercent).HasPrecision(18, 4);
            });
            modelBuilder.Entity<OperatingLimitEpisode>(entity =>
            {
                entity.HasIndex(x => new { x.GeneratorOperatingLimitId, x.ResolvedAtUtc })
                    .HasDatabaseName("IX_OperatingLimitEpisodes_Limit_Open");
                entity.Property(x => x.ObservedValue).HasPrecision(18, 4);
            });

            // === Module Registry System ===

            modelBuilder.Entity<ModuleDefinition>(entity =>
            {
                entity.HasIndex(e => e.Key).IsUnique();
                entity.HasIndex(e => e.ParentKey);
                entity.HasIndex(e => e.SortOrder);
                entity.Property(e => e.Key).HasMaxLength(100);
                entity.Property(e => e.NameEn).HasMaxLength(200);
                entity.Property(e => e.NameAr).HasMaxLength(200);
                entity.Property(e => e.Route).HasMaxLength(500);
                entity.Property(e => e.Icon).HasMaxLength(100);
                entity.Property(e => e.ParentKey).HasMaxLength(100);
                entity.Property(e => e.CustomActions).HasMaxLength(2000);
                entity.HasQueryFilter(e => e.IsActive);

                entity.HasOne(e => e.Parent)
                    .WithMany(e => e.Children)
                    .HasForeignKey("ParentKey")
                    .HasPrincipalKey(e => e.Key)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PagePermission>(entity =>
            {
                entity.HasIndex(e => new { e.ModuleDefinitionId, e.ActionKey }).IsUnique();
                entity.Property(e => e.ActionKey).HasMaxLength(50);
                entity.Property(e => e.NameEn).HasMaxLength(200);
                entity.Property(e => e.NameAr).HasMaxLength(200);
                entity.HasQueryFilter(e => e.IsActive);

                entity.HasOne(e => e.ModuleDefinition)
                    .WithMany(e => e.PagePermissions)
                    .HasForeignKey(e => e.ModuleDefinitionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasIndex(e => new { e.RoleId, e.PagePermissionId }).IsUnique();

                entity.HasOne(e => e.Role)
                    .WithMany()
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.PagePermission)
                    .WithMany()
                    .HasForeignKey(e => e.PagePermissionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        }
    }
}
