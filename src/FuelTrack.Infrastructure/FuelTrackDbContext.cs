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

        // Permission system (Module Registry)
        public DbSet<ModuleDefinition> ModuleDefinitions => Set<ModuleDefinition>();
        public DbSet<PagePermission> PagePermissions => Set<PagePermission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
            modelBuilder.Entity<PeriodicInventoryFuel>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<PeriodicInventoryOil>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<Notification>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<NotificationRecipient>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<AuditLog>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<AnalyticsInsight>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<InsightFeedback>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<RefreshToken>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<LoginAttempt>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<UserScope>().HasQueryFilter(e => e.DeletedAt == null);

            modelBuilder.Entity<Oil>().HasQueryFilter(e => e.DeletedAt == null);

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

            modelBuilder.Entity<Meter>()
                .HasOne(m => m.FuelTank)
                .WithMany()
                .HasForeignKey(m => m.FuelTankId)
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
                .HasOne(ot => ot.Oil)
                .WithMany()
                .HasForeignKey(ot => ot.OilId)
                .OnDelete(DeleteBehavior.SetNull);

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
