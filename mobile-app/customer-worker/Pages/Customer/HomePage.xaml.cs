namespace GTEK.FSM.MobileApp.Pages.Customer;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    private async void OnViewRequestsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//CustomerRequests");
    }

    private async void OnEditProfileClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//CustomerProfile");
    }
}
