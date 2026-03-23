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
}
