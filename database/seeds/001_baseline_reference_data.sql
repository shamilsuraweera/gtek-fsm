-- Phase 1.3.2 baseline seed data
-- Purpose:
--   Seed production-safe reference placeholders for:
--   1) User roles
--   2) Request/assignment status examples
--   3) Subscription tier placeholders
--
-- Notes:
--   - Uses deterministic GUIDs for repeatable runs.
--   - Uses IF NOT EXISTS guards to avoid duplicate inserts.
--   - Operates in a dedicated reference tenant (REF-BASELINE).

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @TenantId UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000001';

DECLARE @SubscriptionFreeId UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000001';
DECLARE @SubscriptionProfessionalId UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000002';
DECLARE @SubscriptionEnterpriseId UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000003';

DECLARE @UserGuestId UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000001';
DECLARE @UserCustomerId UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000002';
DECLARE @UserWorkerId UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000003';
DECLARE @UserSupportId UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000004';
DECLARE @UserManagerId UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000005';
DECLARE @UserAdminId UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000006';

BEGIN TRY
    BEGIN TRANSACTION;

    /* 1) Reference tenant baseline */
    IF NOT EXISTS (SELECT 1 FROM dbo.Tenants WHERE Id = @TenantId)
    BEGIN
        INSERT INTO dbo.Tenants (Id, Code, Name, ActiveSubscriptionId)
        VALUES (@TenantId, N'REF-BASELINE', N'Reference Baseline Tenant', NULL);
    END;

    /* 2) Subscription tier placeholders */
    IF NOT EXISTS (SELECT 1 FROM dbo.Subscriptions WHERE Id = @SubscriptionFreeId)
    BEGIN
        INSERT INTO dbo.Subscriptions (Id, TenantId, PlanCode, StartsOnUtc, EndsOnUtc)
        VALUES (@SubscriptionFreeId, @TenantId, N'FREE', SYSUTCDATETIME(), NULL);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Subscriptions WHERE Id = @SubscriptionProfessionalId)
    BEGIN
        INSERT INTO dbo.Subscriptions (Id, TenantId, PlanCode, StartsOnUtc, EndsOnUtc)
        VALUES (@SubscriptionProfessionalId, @TenantId, N'PROFESSIONAL', SYSUTCDATETIME(), NULL);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Subscriptions WHERE Id = @SubscriptionEnterpriseId)
    BEGIN
        INSERT INTO dbo.Subscriptions (Id, TenantId, PlanCode, StartsOnUtc, EndsOnUtc)
        VALUES (@SubscriptionEnterpriseId, @TenantId, N'ENTERPRISE', SYSUTCDATETIME(), NULL);
    END;

    -- Keep PROFESSIONAL as active tier placeholder for baseline tenant.
    UPDATE dbo.Tenants
    SET ActiveSubscriptionId = @SubscriptionProfessionalId
    WHERE Id = @TenantId
      AND (ActiveSubscriptionId IS NULL OR ActiveSubscriptionId <> @SubscriptionProfessionalId);

    /* 3) User role placeholders (Guest, Customer, Worker, Support, Manager, Admin) */
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @UserGuestId)
    BEGIN
        INSERT INTO dbo.Users (Id, TenantId, ExternalIdentity, DisplayName)
        VALUES (@UserGuestId, @TenantId, N'role:Guest', N'Reference Role - Guest');
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @UserCustomerId)
    BEGIN
        INSERT INTO dbo.Users (Id, TenantId, ExternalIdentity, DisplayName)
        VALUES (@UserCustomerId, @TenantId, N'role:Customer', N'Reference Role - Customer');
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @UserWorkerId)
    BEGIN
        INSERT INTO dbo.Users (Id, TenantId, ExternalIdentity, DisplayName)
        VALUES (@UserWorkerId, @TenantId, N'role:Worker', N'Reference Role - Worker');
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @UserSupportId)
    BEGIN
        INSERT INTO dbo.Users (Id, TenantId, ExternalIdentity, DisplayName)
        VALUES (@UserSupportId, @TenantId, N'role:Support', N'Reference Role - Support');
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @UserManagerId)
    BEGIN
        INSERT INTO dbo.Users (Id, TenantId, ExternalIdentity, DisplayName)
        VALUES (@UserManagerId, @TenantId, N'role:Manager', N'Reference Role - Manager');
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @UserAdminId)
    BEGIN
        INSERT INTO dbo.Users (Id, TenantId, ExternalIdentity, DisplayName)
        VALUES (@UserAdminId, @TenantId, N'role:Admin', N'Reference Role - Admin');
    END;

    /* 4) Request status placeholders (RequestStage vocabulary) */
    IF NOT EXISTS (SELECT 1 FROM dbo.ServiceRequests WHERE Id = '40000000-0000-0000-0000-000000000001')
    BEGIN
        INSERT INTO dbo.ServiceRequests (Id, TenantId, CustomerUserId, Title, Status, ActiveJobId)
        VALUES ('40000000-0000-0000-0000-000000000001', @TenantId, @UserCustomerId, N'STATUS_PLACEHOLDER_NEW', 0, NULL);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.ServiceRequests WHERE Id = '40000000-0000-0000-0000-000000000002')
    BEGIN
        INSERT INTO dbo.ServiceRequests (Id, TenantId, CustomerUserId, Title, Status, ActiveJobId)
        VALUES ('40000000-0000-0000-0000-000000000002', @TenantId, @UserCustomerId, N'STATUS_PLACEHOLDER_ASSIGNED', 1, NULL);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.ServiceRequests WHERE Id = '40000000-0000-0000-0000-000000000003')
    BEGIN
        INSERT INTO dbo.ServiceRequests (Id, TenantId, CustomerUserId, Title, Status, ActiveJobId)
        VALUES ('40000000-0000-0000-0000-000000000003', @TenantId, @UserCustomerId, N'STATUS_PLACEHOLDER_IN_PROGRESS', 2, NULL);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.ServiceRequests WHERE Id = '40000000-0000-0000-0000-000000000004')
    BEGIN
        INSERT INTO dbo.ServiceRequests (Id, TenantId, CustomerUserId, Title, Status, ActiveJobId)
        VALUES ('40000000-0000-0000-0000-000000000004', @TenantId, @UserCustomerId, N'STATUS_PLACEHOLDER_ON_HOLD', 3, NULL);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.ServiceRequests WHERE Id = '40000000-0000-0000-0000-000000000005')
    BEGIN
        INSERT INTO dbo.ServiceRequests (Id, TenantId, CustomerUserId, Title, Status, ActiveJobId)
        VALUES ('40000000-0000-0000-0000-000000000005', @TenantId, @UserCustomerId, N'STATUS_PLACEHOLDER_COMPLETED', 4, NULL);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.ServiceRequests WHERE Id = '40000000-0000-0000-0000-000000000006')
    BEGIN
        INSERT INTO dbo.ServiceRequests (Id, TenantId, CustomerUserId, Title, Status, ActiveJobId)
        VALUES ('40000000-0000-0000-0000-000000000006', @TenantId, @UserCustomerId, N'STATUS_PLACEHOLDER_CANCELLED', 5, NULL);
    END;

    /* 5) Assignment status placeholders */
    IF NOT EXISTS (SELECT 1 FROM dbo.Jobs WHERE Id = '50000000-0000-0000-0000-000000000001')
    BEGIN
        INSERT INTO dbo.Jobs (Id, TenantId, ServiceRequestId, AssignmentStatus, AssignedWorkerUserId)
        VALUES ('50000000-0000-0000-0000-000000000001', @TenantId, '40000000-0000-0000-0000-000000000001', 0, NULL);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Jobs WHERE Id = '50000000-0000-0000-0000-000000000002')
    BEGIN
        INSERT INTO dbo.Jobs (Id, TenantId, ServiceRequestId, AssignmentStatus, AssignedWorkerUserId)
        VALUES ('50000000-0000-0000-0000-000000000002', @TenantId, '40000000-0000-0000-0000-000000000002', 1, @UserWorkerId);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Jobs WHERE Id = '50000000-0000-0000-0000-000000000003')
    BEGIN
        INSERT INTO dbo.Jobs (Id, TenantId, ServiceRequestId, AssignmentStatus, AssignedWorkerUserId)
        VALUES ('50000000-0000-0000-0000-000000000003', @TenantId, '40000000-0000-0000-0000-000000000003', 2, @UserWorkerId);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Jobs WHERE Id = '50000000-0000-0000-0000-000000000004')
    BEGIN
        INSERT INTO dbo.Jobs (Id, TenantId, ServiceRequestId, AssignmentStatus, AssignedWorkerUserId)
        VALUES ('50000000-0000-0000-0000-000000000004', @TenantId, '40000000-0000-0000-0000-000000000004', 3, @UserWorkerId);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Jobs WHERE Id = '50000000-0000-0000-0000-000000000005')
    BEGIN
        INSERT INTO dbo.Jobs (Id, TenantId, ServiceRequestId, AssignmentStatus, AssignedWorkerUserId)
        VALUES ('50000000-0000-0000-0000-000000000005', @TenantId, '40000000-0000-0000-0000-000000000005', 4, @UserWorkerId);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Jobs WHERE Id = '50000000-0000-0000-0000-000000000006')
    BEGIN
        INSERT INTO dbo.Jobs (Id, TenantId, ServiceRequestId, AssignmentStatus, AssignedWorkerUserId)
        VALUES ('50000000-0000-0000-0000-000000000006', @TenantId, '40000000-0000-0000-0000-000000000006', 5, @UserWorkerId);
    END;

    PRINT 'Seed complete: baseline reference placeholders for roles, tiers, and statuses.';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
