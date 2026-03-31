using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Integration;

/// <summary>
/// Phase 3.8 readiness suite consolidating happy-path and access-denied coverage
/// for customer, worker, customer-care, and management core workflows.
/// </summary>
public sealed class Phase3CoreWorkflowReadinessIntegrationTests
{
    [Fact]
    public async Task CustomerWorkflow_HappyPath_CreateAndTransition()
    {
        var createTests = new CreateServiceRequestIntegrationTests();
        await createTests.CreateRequest_CustomerWithValidPayload_ReturnsCreatedAndPersistsTenantScopedRequest();

        var lifecycleTests = new ServiceRequestLifecycleIntegrationTests();
        await lifecycleTests.TransitionStatus_LegalTransition_ReturnsOkAndUpdatesStatus();
    }

    [Fact]
    public async Task WorkerWorkflow_HappyPath_QueryAssignedJobs()
    {
        var queryTests = new QueryEndpointsIntegrationTests();
        await queryTests.GetJobs_WorkerScope_ReturnsOnlyAssignedItems();
    }

    [Fact]
    public async Task CustomerCareWorkflow_HappyPath_AssignAndReassign()
    {
        var assignmentTests = new ServiceRequestAssignmentIntegrationTests();
        await assignmentTests.AssignRequest_SupportRole_SucceedsAndCreatesJobLink();
        await assignmentTests.ReassignRequest_ManagerRole_SucceedsAndPreservesPreviousWorkerContext();
    }

    [Fact]
    public async Task ManagementWorkflow_HappyPath_SubscriptionReadAndUpdate()
    {
        var subscriptionTests = new SubscriptionOperationsIntegrationTests();
        await subscriptionTests.GetOrganizationSubscription_ManagerRole_ReturnsTenantSubscription();
        await subscriptionTests.PatchOrganizationSubscription_ManagerRole_UpdatesPlanAndLimit();
    }

    [Fact]
    public async Task CustomerCareWorkflow_AccessDenied_CustomerCannotAssign()
    {
        var assignmentTests = new ServiceRequestAssignmentIntegrationTests();
        await assignmentTests.AssignRequest_CustomerRole_ReturnsForbidden();
    }

    [Fact]
    public async Task WorkerWorkflow_AccessDenied_CustomerCannotQueryWorkerJobs()
    {
        var queryTests = new QueryEndpointsIntegrationTests();
        await queryTests.GetJobs_CustomerRole_ReturnsForbidden();
    }

    [Fact]
    public async Task ManagementWorkflow_AccessDenied_CustomerCannotAccessOrganizationSubscription()
    {
        var subscriptionTests = new SubscriptionOperationsIntegrationTests();
        await subscriptionTests.GetOrganizationSubscription_CustomerRole_ReturnsForbidden();
    }

    [Fact]
    public async Task CustomerWorkflow_AccessDenied_CrossTenantTransitionBlocked()
    {
        var lifecycleTests = new ServiceRequestLifecycleIntegrationTests();
        await lifecycleTests.TransitionStatus_CrossTenantLookup_ReturnsNotFound();
    }
}
