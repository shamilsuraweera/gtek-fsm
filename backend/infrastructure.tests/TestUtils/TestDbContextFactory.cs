using GTEK.FSM.Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTEK.FSM.Backend.Infrastructure.Tests.TestUtils;

internal static class TestDbContextFactory
{
    public static GtekFsmDbContext CreateInMemory()
    {
        var options = new DbContextOptionsBuilder<GtekFsmDbContext>()
            .UseInMemoryDatabase(databaseName: $"gtek-fsm-tests-{Guid.NewGuid()}")
            .Options;

        return new GtekFsmDbContext(options);
    }
}
