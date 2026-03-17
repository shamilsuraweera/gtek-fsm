namespace GTEK.FSM.MobileApp;

using CustomerHomePage = GTEK.FSM.MobileApp.Pages.Customer.HomePage;
using CustomerRequestsPage = GTEK.FSM.MobileApp.Pages.Customer.RequestsPage;
using CustomerJobsPage = GTEK.FSM.MobileApp.Pages.Customer.JobsPage;
using CustomerProfilePage = GTEK.FSM.MobileApp.Pages.Customer.ProfilePage;
using CustomerSettingsPage = GTEK.FSM.MobileApp.Pages.Customer.SettingsPage;
using WorkerHomePage = GTEK.FSM.MobileApp.Pages.Worker.HomePage;
using WorkerRequestsPage = GTEK.FSM.MobileApp.Pages.Worker.RequestsPage;
using WorkerJobsPage = GTEK.FSM.MobileApp.Pages.Worker.JobsPage;
using WorkerProfilePage = GTEK.FSM.MobileApp.Pages.Worker.ProfilePage;
using WorkerSettingsPage = GTEK.FSM.MobileApp.Pages.Worker.SettingsPage;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Customer routes
        Routing.RegisterRoute("CustomerHome", typeof(CustomerHomePage));
        Routing.RegisterRoute("CustomerRequests", typeof(CustomerRequestsPage));
        Routing.RegisterRoute("CustomerJobs", typeof(CustomerJobsPage));
        Routing.RegisterRoute("CustomerProfile", typeof(CustomerProfilePage));
        Routing.RegisterRoute("CustomerSettings", typeof(CustomerSettingsPage));

        // Worker routes
        Routing.RegisterRoute("WorkerHome", typeof(WorkerHomePage));
        Routing.RegisterRoute("WorkerRequests", typeof(WorkerRequestsPage));
        Routing.RegisterRoute("WorkerJobs", typeof(WorkerJobsPage));
        Routing.RegisterRoute("WorkerProfile", typeof(WorkerProfilePage));
        Routing.RegisterRoute("WorkerSettings", typeof(WorkerSettingsPage));
    }
}
