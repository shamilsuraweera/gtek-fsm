namespace GTEK.FSM.MobileApp.Configuration;

public sealed class ApiEndpointConfiguration
{
    public string ApiBaseUrl { get; }

    public ApiEndpointConfiguration()
    {
#if DEBUG
        // Local development environment
        ApiBaseUrl = "http://localhost:5000";
#else
        // Production/Release environment placeholder
        ApiBaseUrl = "https://api.gtek-fsm.example.com";
#endif
    }
}
