using GTEK.FSM.Backend.Domain.Rules;

namespace GTEK.FSM.Backend.Domain.Aggregates;

public sealed class LocalCredential
{
    public LocalCredential(Guid userId, Guid tenantId, string email, string passwordHash, string role)
    {
        this.UserId = DomainGuards.RequiredId(userId, nameof(userId), "User id cannot be empty.");
        this.TenantId = DomainGuards.RequiredId(tenantId, nameof(tenantId), "Tenant id cannot be empty.");
        this.Email = DomainGuards.RequiredText(email, nameof(email), "Email is required.", 256);
        this.PasswordHash = DomainGuards.RequiredText(passwordHash, nameof(passwordHash), "Password hash is required.", 512);
        this.Role = DomainGuards.RequiredText(role, nameof(role), "Role is required.", 32);
    }

    public Guid UserId { get; }

    public Guid TenantId { get; }

    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public string Role { get; private set; }

    public DateTime CreatedAtUtc { get; internal set; }

    public DateTime UpdatedAtUtc { get; internal set; }

    public void UpdateEmail(string email)
    {
        this.Email = DomainGuards.RequiredText(email, nameof(email), "Email is required.", 256);
    }

    public void UpdatePasswordHash(string passwordHash)
    {
        this.PasswordHash = DomainGuards.RequiredText(passwordHash, nameof(passwordHash), "Password hash is required.", 512);
    }

    public void UpdateRole(string role)
    {
        this.Role = DomainGuards.RequiredText(role, nameof(role), "Role is required.", 32);
    }
}
