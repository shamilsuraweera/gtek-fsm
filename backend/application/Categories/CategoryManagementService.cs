using GTEK.FSM.Backend.Application.Audit;
using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Transactions;
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Audit;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Categories.Requests;

namespace GTEK.FSM.Backend.Application.Categories;

internal sealed class CategoryManagementService : ICategoryManagementService
{
    private readonly ICategoryRepository categoryRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IAuditLogWriter auditLogWriter;

    public CategoryManagementService(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IAuditLogWriter auditLogWriter)
    {
        this.categoryRepository = categoryRepository;
        this.unitOfWork = unitOfWork;
        this.auditLogWriter = auditLogWriter;
    }

    public async Task<CategoryMutationResult> CreateAsync(
        AuthenticatedPrincipal principal,
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsManagementRole(principal))
        {
            return CategoryMutationResult.Failure("Role is not authorized to manage categories.", "AUTH_FORBIDDEN_ROLE", 403);
        }

        var normalizedCode = request.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        var duplicate = await this.categoryRepository.GetByCodeAsync(principal.TenantId, normalizedCode, cancellationToken);
        if (duplicate is not null)
        {
            return CategoryMutationResult.Failure("Category code already exists for tenant.", "CATEGORY_CODE_CONFLICT", 409);
        }

        ServiceCategory category;
        try
        {
            category = new ServiceCategory(Guid.NewGuid(), principal.TenantId, normalizedCode, request.Name ?? string.Empty, request.SortOrder ?? 0);
        }
        catch (ArgumentException ex)
        {
            return CategoryMutationResult.Failure(ex.Message, "VALIDATION_FAILED", 400);
        }

        await this.categoryRepository.AddAsync(category, cancellationToken);
        await this.unitOfWork.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(principal, category.Id, "CATEGORY_CREATED", "Success", cancellationToken);

        return CategoryMutationResult.Success(ToItem(category), "Category created.");
    }

    public async Task<CategoryMutationResult> UpdateAsync(
        AuthenticatedPrincipal principal,
        Guid categoryId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsManagementRole(principal))
        {
            return CategoryMutationResult.Failure("Role is not authorized to manage categories.", "AUTH_FORBIDDEN_ROLE", 403);
        }

        var category = await this.categoryRepository.GetForUpdateAsync(principal.TenantId, categoryId, cancellationToken);
        if (category is null)
        {
            return CategoryMutationResult.Failure("Category was not found.", "CATEGORY_NOT_FOUND", 404);
        }

        var normalizedCode = request.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        var duplicate = await this.categoryRepository.GetByCodeAsync(principal.TenantId, normalizedCode, cancellationToken);
        if (duplicate is not null && duplicate.Id != category.Id)
        {
            return CategoryMutationResult.Failure("Category code already exists for tenant.", "CATEGORY_CODE_CONFLICT", 409);
        }

        try
        {
            category.Update(normalizedCode, request.Name ?? string.Empty, request.SortOrder ?? category.SortOrder);
        }
        catch (ArgumentException ex)
        {
            return CategoryMutationResult.Failure(ex.Message, "VALIDATION_FAILED", 400);
        }

        if (request.IsEnabled.HasValue)
        {
            if (request.IsEnabled.Value)
            {
                category.Enable();
            }
            else
            {
                category.Disable();
            }
        }

        this.categoryRepository.Update(category);
        await this.unitOfWork.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(principal, category.Id, "CATEGORY_UPDATED", "Success", cancellationToken);

        return CategoryMutationResult.Success(ToItem(category), "Category updated.");
    }

    public async Task<CategoryMutationResult> DisableAsync(
        AuthenticatedPrincipal principal,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        if (!IsManagementRole(principal))
        {
            return CategoryMutationResult.Failure("Role is not authorized to manage categories.", "AUTH_FORBIDDEN_ROLE", 403);
        }

        var category = await this.categoryRepository.GetForUpdateAsync(principal.TenantId, categoryId, cancellationToken);
        if (category is null)
        {
            return CategoryMutationResult.Failure("Category was not found.", "CATEGORY_NOT_FOUND", 404);
        }

        category.Disable();
        this.categoryRepository.Update(category);
        await this.unitOfWork.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(principal, category.Id, "CATEGORY_DISABLED", "Success", cancellationToken);

        return CategoryMutationResult.Success(ToItem(category), "Category disabled.");
    }

    public async Task<CategoriesQueryResult> ReorderAsync(
        AuthenticatedPrincipal principal,
        ReorderCategoriesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsManagementRole(principal))
        {
            return CategoriesQueryResult.Failure("Role is not authorized to manage categories.", "AUTH_FORBIDDEN_ROLE", 403);
        }

        var items = request.Items?.ToArray() ?? Array.Empty<ReorderCategoryItemRequest>();
        foreach (var item in items)
        {
            if (!Guid.TryParse(item.CategoryId, out var categoryId) || categoryId == Guid.Empty)
            {
                return CategoriesQueryResult.Failure("categoryId must be a valid GUID.", "VALIDATION_CATEGORY_ID_INVALID", 400);
            }

            var category = await this.categoryRepository.GetForUpdateAsync(principal.TenantId, categoryId, cancellationToken);
            if (category is null)
            {
                return CategoriesQueryResult.Failure("Category was not found.", "CATEGORY_NOT_FOUND", 404);
            }

            try
            {
                category.Reorder(item.SortOrder ?? 0);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return CategoriesQueryResult.Failure(ex.Message, "VALIDATION_SORT_ORDER_INVALID", 400);
            }

            this.categoryRepository.Update(category);
        }

        await this.unitOfWork.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(principal, null, "CATEGORY_REORDERED", "Success", cancellationToken);

        var latest = await this.categoryRepository.ListByTenantAsync(principal.TenantId, includeDisabled: true, cancellationToken);
        var payload = latest.Select(ToItem).ToArray();
        return CategoriesQueryResult.Success(payload, "Categories reordered.");
    }

    private static QueriedCategoryItem ToItem(ServiceCategory category)
    {
        return new QueriedCategoryItem(
            CategoryId: category.Id,
            TenantId: category.TenantId,
            Code: category.Code,
            Name: category.Name,
            SortOrder: category.SortOrder,
            IsEnabled: category.IsEnabled,
            CreatedAtUtc: category.CreatedAtUtc,
            UpdatedAtUtc: category.UpdatedAtUtc);
    }

    private async Task WriteAuditAsync(
        AuthenticatedPrincipal principal,
        Guid? entityId,
        string action,
        string outcome,
        CancellationToken cancellationToken)
    {
        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = principal.UserId,
            TenantId = principal.TenantId,
            EntityType = "ServiceCategory",
            EntityId = entityId ?? Guid.Empty,
            Action = action,
            Outcome = outcome,
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Details = null,
        };

        await this.auditLogWriter.WriteAsync(audit, cancellationToken);
    }

    private static bool IsManagementRole(AuthenticatedPrincipal principal)
    {
        return principal.IsInRole("Manager") || principal.IsInRole("Admin");
    }
}
