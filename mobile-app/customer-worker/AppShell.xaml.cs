namespace GTEK.FSM.MobileApp;

using CustomerHomePage = GTEK.FSM.MobileApp.Pages.Customer.HomePage;
using WorkerHomePage = GTEK.FSM.MobileApp.Pages.Worker.HomePage;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("CustomerHome", typeof(CustomerHomePage));
        Routing.RegisterRoute("WorkerHome", typeof(WorkerHomePage));
    }
}
