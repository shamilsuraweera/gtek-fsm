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
    }
}
