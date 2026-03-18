using Microsoft.EntityFrameworkCore;

namespace GTEK.FSM.Backend.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext baseline for migration infrastructure.
/// Domain DbSet mappings will be introduced in Phase 1.
/// </summary>
public class GtekFsmDbContext : DbContext
{
    public GtekFsmDbContext(DbContextOptions<GtekFsmDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Intentionally empty in Phase 0.7.2.
        // Entity mappings and constraints are added when domain models are introduced.
    }
}
