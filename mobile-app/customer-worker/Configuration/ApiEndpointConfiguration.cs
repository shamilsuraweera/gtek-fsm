namespace GTEK.FSM.MobileApp.Configuration;

using Microsoft.Maui.Devices;

public sealed class ApiEndpointConfiguration
{
    public string ApiBaseUrl { get; }

    public ApiEndpointConfiguration()
    {
    var configuredBaseUrl = Environment.GetEnvironmentVariable("GTEK_FSM_API_BASE_URL");
    if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
    {
        ApiBaseUrl = configuredBaseUrl.TrimEnd('/');
        return;
    }

#if DEBUG
    var configuredPort = Environment.GetEnvironmentVariable("GTEK_FSM_API_PORT");
    var port = string.IsNullOrWhiteSpace(configuredPort) ? "5000" : configuredPort;

    // Android emulators cannot resolve host machine via localhost; 10.0.2.2 is the emulator loopback alias.
    var localHost = DeviceInfo.Platform == DevicePlatform.Android ? "10.0.2.2" : "localhost";
    ApiBaseUrl = $"http://{localHost}:{port}";
#else
    ApiBaseUrl = "https://api.gtek-fsm.example.com";
#endif
    }
}
