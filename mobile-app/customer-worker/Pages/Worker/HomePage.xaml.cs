namespace GTEK.FSM.MobileApp.Pages.Worker;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    private async void OnOpenJobsWorkspaceClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//WorkerJobs");
    }

    private async void OnAcceptTopJobClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//WorkerJobs");
        await DisplayAlertAsync("Dispatch", "Top available job pathway opened.", "OK");
    }

    private async void OnUpdateStatusClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//WorkerJobs");
        await DisplayAlertAsync("Status", "Status update pathway opened in jobs workspace.", "OK");
    }

    private async void OnCallDispatchClicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Dispatch", "Dispatch contact pathway placeholder opened.", "OK");
    }
}
