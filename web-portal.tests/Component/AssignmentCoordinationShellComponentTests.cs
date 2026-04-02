namespace GTEK.FSM.WebPortal.Tests.Component;

using Bunit;
using GTEK.FSM.Shared.Contracts.Vocabulary;
using GTEK.FSM.WebPortal.Components;
using GTEK.FSM.WebPortal.Models;
using GTEK.FSM.WebPortal.Services.Security;
using Microsoft.Extensions.DependencyInjection;

public sealed class AssignmentCoordinationShellComponentTests : TestContext
{
    [Fact]
    public void AssignBestMatch_TriggersDecisionCallback_ForAccessibleTenant()
    {
        // Arrange
        this.Services.AddScoped<UiSecurityContext>();

        (string requestReference, string workerId, bool isReassignment)? captured = null;
        var requests = new List<OperationalQueueItem>
        {
            new()
            {
                Reference = "ASN-500",
                Customer = "Acme Hospital",
                TenantId = "TENANT-01",
                Stage = "Dispatch",
                Priority = "Critical",
                Summary = "Needs immediate assignment",
                UpdatedAtUtc = DateTime.UtcNow,
                Status = RequestStage.Assigned,
                UrgencyLevel = UrgencyLevel.Critical,
                IsEscalated = true,
                IsSLABreach = true,
            },
        };

        var workers = new List<AssignmentWorkerOption>
        {
            new() { Id = "W-1", Name = "Alex", SkillTag = "Dispatch", ActiveAssignments = 1, IsAvailable = true, HasConflict = false },
            new() { Id = "W-2", Name = "Jordan", SkillTag = "Dispatch", ActiveAssignments = 3, IsAvailable = true, HasConflict = false },
        };

        var cut = this.RenderComponent<AssignmentCoordinationShell>(parameters => parameters
            .Add(x => x.Requests, requests)
            .Add(x => x.Workers, workers)
            .Add(x => x.OnDecisionTriggered, EventCallback.Factory.Create<(string requestReference, string workerId, bool isReassignment)>(
                this,
                payload => captured = payload)));

        // Act
        cut.FindAll("button").First(x => x.TextContent.Contains("Assign Best Match", StringComparison.Ordinal)).Click();

        // Assert
        Assert.NotNull(captured);
        Assert.Equal("ASN-500", captured.Value.requestReference);
        Assert.Equal("W-1", captured.Value.workerId);
        Assert.False(captured.Value.isReassignment);
    }

    [Fact]
    public void TenantGuard_DisablesAssignmentActions_ForInaccessibleTenant()
    {
        // Arrange
        this.Services.AddScoped<UiSecurityContext>();

        var requests = new List<OperationalQueueItem>
        {
            new()
            {
                Reference = "ASN-501",
                Customer = "Blue Tower",
                TenantId = "TENANT-03",
                Stage = "Dispatch",
                Priority = "High",
                Summary = "Blocked by tenant guard",
                UpdatedAtUtc = DateTime.UtcNow,
                Status = RequestStage.Assigned,
                UrgencyLevel = UrgencyLevel.High,
            },
        };

        var workers = new List<AssignmentWorkerOption>
        {
            new() { Id = "W-9", Name = "Sam", SkillTag = "Dispatch", ActiveAssignments = 0, IsAvailable = true, HasConflict = false },
        };

        var cut = this.RenderComponent<AssignmentCoordinationShell>(parameters => parameters
            .Add(x => x.Requests, requests)
            .Add(x => x.Workers, workers));

        // Act + Assert
        var assignBestMatch = cut.FindAll("button").First(x => x.TextContent.Contains("Assign Best Match", StringComparison.Ordinal));
        Assert.True(assignBestMatch.HasAttribute("disabled"));

        var assign = cut.FindAll("button").First(x => x.TextContent.Equals("Assign", StringComparison.Ordinal));
        Assert.True(assign.HasAttribute("disabled"));
    }
}
