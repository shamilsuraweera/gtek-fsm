namespace GTEK.FSM.MobileApp.Services.Identity;

public interface IIdentityTokenProvider
{
    string GetAccessToken();

    void SetAccessToken(string token);

    void ClearAccessToken();
}
