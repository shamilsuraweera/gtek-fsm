using GTEK.FSM.WebPortal.Models;

namespace GTEK.FSM.WebPortal.Services;

public sealed record QueueSavedView(string Name, QueueFilterState FilterState, DateTime SavedAtUtc);

public interface IQueueSavedViewService
{
    IReadOnlyList<QueueSavedView> GetViews(string tenantId, string role);

    void SaveView(string tenantId, string role, string name, QueueFilterState state);

    void DeleteView(string tenantId, string role, string name);
}

public sealed class QueueSavedViewService : IQueueSavedViewService
{
    private readonly Dictionary<string, List<QueueSavedView>> viewsByScope = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<QueueSavedView> GetViews(string tenantId, string role)
    {
        var key = BuildScopeKey(tenantId, role);
        if (!this.viewsByScope.TryGetValue(key, out var scopedViews))
        {
            return [];
        }

        return scopedViews
            .OrderByDescending(x => x.SavedAtUtc)
            .ToList();
    }

    public void SaveView(string tenantId, string role, string name, QueueFilterState state)
    {
        var normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return;
        }

        var key = BuildScopeKey(tenantId, role);
        if (!this.viewsByScope.TryGetValue(key, out var scopedViews))
        {
            scopedViews = [];
            this.viewsByScope[key] = scopedViews;
        }

        var existingIndex = scopedViews.FindIndex(x => string.Equals(x.Name, normalizedName, StringComparison.OrdinalIgnoreCase));
        var savedView = new QueueSavedView(normalizedName, state.Clone(), DateTime.UtcNow);

        if (existingIndex >= 0)
        {
            scopedViews[existingIndex] = savedView;
            return;
        }

        scopedViews.Add(savedView);
    }

    public void DeleteView(string tenantId, string role, string name)
    {
        var normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return;
        }

        var key = BuildScopeKey(tenantId, role);
        if (!this.viewsByScope.TryGetValue(key, out var scopedViews))
        {
            return;
        }

        scopedViews.RemoveAll(x => string.Equals(x.Name, normalizedName, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildScopeKey(string tenantId, string role)
    {
        return $"{tenantId.Trim()}:{role.Trim()}";
    }
}
