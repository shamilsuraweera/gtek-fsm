namespace GTEK.FSM.MobileApp.Services.Realtime;

public enum MobileOperationalRealtimeConnectionState
{
    Disabled = 0,
    Disconnected = 1,
    Connecting = 2,
    Connected = 3,
    Reconnecting = 4,
    AuthenticationRequired = 5,
    Faulted = 6,
}