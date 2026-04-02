namespace GTEK.FSM.MobileApp.State;

using GTEK.FSM.MobileApp.Services.Notifications;

public sealed class MobileNotificationInboxState
{
    private const int MaxNotifications = 100;
    private readonly Queue<MobileNotificationPayload> _notifications = new();

    public IReadOnlyCollection<MobileNotificationPayload> Notifications => _notifications.ToArray();

    public void Add(MobileNotificationPayload payload)
    {
        _notifications.Enqueue(payload);

        while (_notifications.Count > MaxNotifications)
        {
            _notifications.Dequeue();
        }
    }

    public MobileNotificationPayload PullLatest()
    {
        if (_notifications.Count == 0)
        {
            return new MobileNotificationPayload(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, DateTime.UtcNow);
        }

        var latest = _notifications.Last();
        _notifications.Clear();
        return latest;
    }

    public void Clear()
    {
        _notifications.Clear();
    }
}