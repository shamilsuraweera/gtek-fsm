namespace GTEK.FSM.MobileApp;

using CustomerHomePage = GTEK.FSM.MobileApp.Pages.Customer.HomePage;
using CustomerRequestsPage = GTEK.FSM.MobileApp.Pages.Customer.RequestsPage;
using CustomerJobsPage = GTEK.FSM.MobileApp.Pages.Customer.JobsPage;
using CustomerProfilePage = GTEK.FSM.MobileApp.Pages.Customer.ProfilePage;
using CustomerSettingsPage = GTEK.FSM.MobileApp.Pages.Customer.SettingsPage;
using GTEK.FSM.MobileApp.Navigation;
using GTEK.FSM.MobileApp.State;
using WorkerHomePage = GTEK.FSM.MobileApp.Pages.Worker.HomePage;
using WorkerRequestsPage = GTEK.FSM.MobileApp.Pages.Worker.RequestsPage;
using WorkerJobsPage = GTEK.FSM.MobileApp.Pages.Worker.JobsPage;
using WorkerProfilePage = GTEK.FSM.MobileApp.Pages.Worker.ProfilePage;
using WorkerSettingsPage = GTEK.FSM.MobileApp.Pages.Worker.SettingsPage;

public partial class AppShell : Shell
{
    public AppShell()
        : this(new SessionContextState())
    {
    }

    public AppShell(SessionContextState sessionContextState)
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

        ApplyRoleGate(sessionContextState);
    }

    private void ApplyRoleGate(SessionContextState sessionContextState)
    {
        var role = (sessionContextState.Role ?? string.Empty).Trim();
        var visibility = RoleGateResolver.Resolve(role);

        if (visibility == MobileSectionVisibility.WorkerOnly)
        {
            CustomerTab.IsVisible = false;
            WorkerTab.IsVisible = true;
            CurrentItem = WorkerTab;
            return;
        }

        if (visibility == MobileSectionVisibility.CustomerOnly)
        {
            WorkerTab.IsVisible = false;
            CustomerTab.IsVisible = true;
            CurrentItem = CustomerTab;
            return;
        }

        CustomerTab.IsVisible = true;
        WorkerTab.IsVisible = true;
    }
}
