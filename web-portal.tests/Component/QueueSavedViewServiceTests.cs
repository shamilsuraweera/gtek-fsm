namespace GTEK.FSM.WebPortal.Tests.Component;

using GTEK.FSM.WebPortal.Models;
using GTEK.FSM.WebPortal.Services;

public sealed class QueueSavedViewServiceTests
{
    [Fact]
    public void GetViews_ReturnsEmpty_WhenNoViewsSaved()
    {
        var service = new QueueSavedViewService();

        var result = service.GetViews("TENANT-01", "Manager");

        Assert.Empty(result);
    }

    [Fact]
    public void SaveView_StoresView_ForTenantRole()
    {
        var service = new QueueSavedViewService();
        var state = new QueueFilterState
        {
            SearchText = "urgent",
            StageFilter = "Dispatch",
            StatusFilter = "Assigned",
            UrgencyFilter = "High",
        };

        service.SaveView("TENANT-01", "Manager", "High Priority Dispatch", state);

        var result = service.GetViews("TENANT-01", "Manager");
        Assert.Single(result);
        Assert.Equal("High Priority Dispatch", result[0].Name);
        Assert.Equal("urgent", result[0].FilterState.SearchText);
    }

    [Fact]
    public void GetViews_DoesNotLeakAcrossTenants()
    {
        var service = new QueueSavedViewService();
        var state = new QueueFilterState { SearchText = "alpha" };

        service.SaveView("TENANT-A", "Manager", "Tenant A View", state);

        var tenantBResult = service.GetViews("TENANT-B", "Manager");
        Assert.Empty(tenantBResult);
    }

    [Fact]
    public void SaveView_ReplacesExistingView_WhenNameMatches()
    {
        var service = new QueueSavedViewService();
        service.SaveView("TENANT-01", "Manager", "Dispatch Focus", new QueueFilterState { SearchText = "first" });

        service.SaveView("TENANT-01", "Manager", "Dispatch Focus", new QueueFilterState { SearchText = "second" });

        var result = service.GetViews("TENANT-01", "Manager");
        Assert.Single(result);
        Assert.Equal("second", result[0].FilterState.SearchText);
    }

    [Fact]
    public void DeleteView_RemovesView()
    {
        var service = new QueueSavedViewService();
        service.SaveView("TENANT-01", "Manager", "Delete Me", new QueueFilterState());

        service.DeleteView("TENANT-01", "Manager", "Delete Me");

        var result = service.GetViews("TENANT-01", "Manager");
        Assert.Empty(result);
    }
}
