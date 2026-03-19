using GTEK.FSM.Backend.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace GTEK.FSM.Backend.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for GTEK FSM persistence.
/// </summary>
public class GtekFsmDbContext : DbContext
{
    public GtekFsmDbContext(DbContextOptions<GtekFsmDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => this.Set<Tenant>();

    public DbSet<User> Users => this.Set<User>();

    public DbSet<ServiceRequest> ServiceRequests => this.Set<ServiceRequest>();

    public DbSet<Job> Jobs => this.Set<Job>();

    public DbSet<Subscription> Subscriptions => this.Set<Subscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GtekFsmDbContext).Assembly);

        // Apply global soft-delete query filters for all tenant-owned aggregates.
        // This ensures that IsDeleted = true records are automatically excluded from all queries.
        modelBuilder.Entity<Tenant>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<User>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<ServiceRequest>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<Job>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<Subscription>()
            .HasQueryFilter(x => !x.IsDeleted);
    }
}
