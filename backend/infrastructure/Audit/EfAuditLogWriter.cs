using GTEK.FSM.Backend.Domain.Audit;
using GTEK.FSM.Backend.Application.Audit;
using GTEK.FSM.Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace GTEK.FSM.Backend.Infrastructure.Audit
{
    /// <summary>
    /// Persists audit logs to the database.
    /// </summary>
    public class EfAuditLogWriter : IAuditLogWriter
    {
        private readonly GtekFsmDbContext _dbContext;
        public EfAuditLogWriter(GtekFsmDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task WriteAsync(AuditLog log, CancellationToken cancellationToken = default)
        {
            await _dbContext.AuditLogs.AddAsync(log, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
