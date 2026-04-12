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

    // Emulator should use 10.0.2.2; physical Android devices use the host LAN IP.
    var localHost = "192.168.8.197";
        if (DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.DeviceType == DeviceType.Virtual)
        {
            localHost = "10.0.2.2";
        }

        ApiBaseUrl = $"http://{localHost}:{port}";
#else
        ApiBaseUrl = "https://api.gtek-fsm.example.com";
#endif
    }
}
