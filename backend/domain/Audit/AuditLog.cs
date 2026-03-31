using System;

namespace GTEK.FSM.Backend.Domain.Audit
{
    /// <summary>
    /// Represents a persisted audit log entry for business actions (status, assignment, subscription, sensitive updates).
    /// </summary>
    public class AuditLog
    {
        public Guid Id { get; set; }
        public Guid? ActorUserId { get; set; }
        public Guid TenantId { get; set; }
        public string EntityType { get; set; } = null!;
        public Guid EntityId { get; set; }
        public string Action { get; set; } = null!;
        public string Outcome { get; set; } = null!;
        public DateTimeOffset OccurredAtUtc { get; set; }
        public string? Details { get; set; }
    }
}
