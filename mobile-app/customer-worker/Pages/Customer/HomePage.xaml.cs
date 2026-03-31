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

    private async void OnTrackActiveJobClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//CustomerRequests");
    }

    private async void OnApproveQuoteClicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Quote", "Approval pathway placeholder: quote approved and queued for dispatch.", "OK");
    }

    private async void OnContactSupportClicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Support", "Support contact pathway placeholder opened.", "OK");
    }
}
