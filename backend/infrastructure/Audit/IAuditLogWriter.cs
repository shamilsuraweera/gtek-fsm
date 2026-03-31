using GTEK.FSM.Backend.Domain.Audit;
using System.Threading;
using System.Threading.Tasks;

namespace GTEK.FSM.Backend.Infrastructure.Audit
{
    /// <summary>
    /// Interface for writing audit logs to persistent storage.
    /// </summary>
    public interface IAuditLogWriter
    {
        Task WriteAsync(AuditLog log, CancellationToken cancellationToken = default);
    }
}
