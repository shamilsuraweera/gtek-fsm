namespace GTEK.FSM.MobileApp.State;

public sealed class SessionContextState
{
    public bool IsSessionActive { get; private set; }

    public string UserId { get; private set; } = string.Empty;

    public string Role { get; private set; } = string.Empty;

    public DateTimeOffset? LastUpdatedUtc { get; private set; }

    public void Update(string userId, string role, bool isSessionActive)
    {
        UserId = userId;
        Role = role;
        IsSessionActive = isSessionActive;
        LastUpdatedUtc = DateTimeOffset.UtcNow;
    }

    public void Clear()
    {
        UserId = string.Empty;
        Role = string.Empty;
        IsSessionActive = false;
        LastUpdatedUtc = DateTimeOffset.UtcNow;
    }
}
