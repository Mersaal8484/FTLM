using FuelTrack.Domain.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Platform;

/// <summary>Platform-owned metadata; customer operational data belongs in tenant schemas.</summary>
public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public DbSet<PlatformTenant> Tenants => Set<PlatformTenant>();
    public DbSet<SubscriptionPlan> Plans => Set<SubscriptionPlan>();
    public DbSet<PlatformSubscription> Subscriptions => Set<PlatformSubscription>();
    public DbSet<TenantCredential> TenantCredentials => Set<TenantCredential>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("platform");
        modelBuilder.Entity<PlatformTenant>(tenant =>
        {
            tenant.ToTable("Tenants");
            tenant.HasKey(x => x.Id);
            tenant.Property(x => x.Id).ValueGeneratedNever();
            tenant.Property(x => x.Name).HasMaxLength(200).IsRequired();
            tenant.Property(x => x.Host).HasMaxLength(253).IsRequired();
            tenant.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            tenant.HasIndex(x => x.Host).IsUnique();
            tenant.Ignore(x => x.Schema);
        });
        modelBuilder.Entity<SubscriptionPlan>(plan =>
        {
            plan.ToTable("Plans");
            plan.HasKey(x => x.Id);
            plan.Property(x => x.Id).ValueGeneratedNever();
            plan.Property(x => x.Name).HasMaxLength(200).IsRequired();
            plan.Property(x => x.Price).HasPrecision(18, 2);
            plan.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        });
        modelBuilder.Entity<PlatformSubscription>(subscription =>
        {
            subscription.ToTable("Subscriptions");
            subscription.HasKey(x => x.Id);
            subscription.Property(x => x.Id).ValueGeneratedNever();
            subscription.HasOne<PlatformTenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            subscription.HasOne<SubscriptionPlan>().WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
            subscription.HasIndex(x => new { x.TenantId, x.StartsAt });
        });
        modelBuilder.Entity<TenantCredential>(credential =>
        {
            credential.ToTable("TenantCredentials");
            credential.HasKey(x => x.TenantId);
            credential.Property(x => x.TenantId).ValueGeneratedNever();
            credential.Property(x => x.KeyId).HasMaxLength(100).IsRequired();
            credential.HasOne<PlatformTenant>().WithOne().HasForeignKey<TenantCredential>(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
