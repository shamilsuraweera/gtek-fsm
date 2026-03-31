using System;
using System.Threading;
using System.Threading.Tasks;
using GTEK.FSM.Backend.Domain.Audit;
using GTEK.FSM.Backend.Infrastructure.Audit;
using GTEK.FSM.Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Audit
{
    public class AuditLogPersistenceTests
    {
        [Fact]
        public async Task CanPersistAndRetrieveAuditLog()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<GtekFsmDbContext>()
                .UseInMemoryDatabase(databaseName: $"AuditLogTestDb_{Guid.NewGuid()}")
                .Options;
            var dbContext = new GtekFsmDbContext(options);
            var writer = new EfAuditLogWriter(dbContext);
            var log = new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorUserId = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                EntityType = "ServiceRequest",
                EntityId = Guid.NewGuid(),
                Action = "StatusTransition:New->Assigned",
                Outcome = "Success",
                OccurredAtUtc = DateTimeOffset.UtcNow,
                Details = "Test details"
            };

            // Act
            await writer.WriteAsync(log, CancellationToken.None);
            var retrieved = await dbContext.AuditLogs.FirstOrDefaultAsync(x => x.Id == log.Id);

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal(log.ActorUserId, retrieved!.ActorUserId);
            Assert.Equal(log.TenantId, retrieved.TenantId);
            Assert.Equal(log.EntityType, retrieved.EntityType);
            Assert.Equal(log.EntityId, retrieved.EntityId);
            Assert.Equal(log.Action, retrieved.Action);
            Assert.Equal(log.Outcome, retrieved.Outcome);
            Assert.Equal(log.Details, retrieved.Details);
        }
    }
}
