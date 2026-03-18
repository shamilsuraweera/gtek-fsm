-- Phase 0.7.3 placeholder seed file
-- No business schema is created in this phase.
-- This script is intentionally no-op and safe to run repeatedly.

SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    PRINT 'Seed placeholder: baseline reference data is deferred until Phase 1 schema is available.';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
