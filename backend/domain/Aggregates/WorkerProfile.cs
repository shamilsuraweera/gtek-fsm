using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Backend.Domain.Rules;

namespace GTEK.FSM.Backend.Domain.Aggregates;

/// <summary>
/// Worker profile aggregate for management-controlled worker operations.
/// </summary>
public sealed class WorkerProfile
{
    private const int MaxSkillCount = 20;
    private const int MaxSkillLength = 40;

    public WorkerProfile(
        Guid id,
        Guid tenantId,
        string workerCode,
        string displayName,
        decimal internalRating,
        IEnumerable<string>? skills = null,
        decimal? baseLatitude = null,
        decimal? baseLongitude = null)
    {
        this.Id = DomainGuards.RequiredId(id, nameof(id), "Worker id cannot be empty.");
        this.TenantId = DomainGuards.RequiredId(tenantId, nameof(tenantId), "Worker must belong to a tenant.");
        this.WorkerCode = NormalizeWorkerCode(workerCode);
        this.DisplayName = DomainGuards.RequiredText(displayName, nameof(displayName), "Worker display name is required.", 120);
        this.InternalRating = NormalizeRating(internalRating);
        this.AvailabilityStatus = WorkerAvailabilityStatus.Available;
        this.IsActive = true;
        this.SkillTagsSerialized = string.Empty;
        this.BaseLatitude = null;
        this.BaseLongitude = null;

        this.ReplaceSkills(skills ?? Array.Empty<string>());
        this.SetBaseLocation(baseLatitude, baseLongitude);
    }

    public Guid Id { get; }

    public Guid TenantId { get; }

    public string WorkerCode { get; private set; }

    public string DisplayName { get; private set; }

    public decimal InternalRating { get; private set; }

    public string SkillTagsSerialized { get; private set; }

    public decimal? BaseLatitude { get; private set; }

    public decimal? BaseLongitude { get; private set; }

    public WorkerAvailabilityStatus AvailabilityStatus { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; internal set; }

    public DateTime UpdatedAtUtc { get; internal set; }

    public bool IsDeleted { get; internal set; }

    public IReadOnlyList<string> GetSkills()
    {
        if (string.IsNullOrWhiteSpace(this.SkillTagsSerialized))
        {
            return Array.Empty<string>();
        }

        return this.SkillTagsSerialized
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }

    public void UpdateProfile(string workerCode, string displayName, decimal internalRating)
    {
        this.WorkerCode = NormalizeWorkerCode(workerCode);
        this.DisplayName = DomainGuards.RequiredText(displayName, nameof(displayName), "Worker display name is required.", 120);
        this.InternalRating = NormalizeRating(internalRating);
    }

    public void ReplaceSkills(IEnumerable<string> skills)
    {
        var normalized = (skills ?? Array.Empty<string>())
            .Select(NormalizeSkill)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        if (normalized.Length > MaxSkillCount)
        {
            throw new ArgumentOutOfRangeException(nameof(skills), $"Worker skill count must be {MaxSkillCount} or fewer.");
        }

        this.SkillTagsSerialized = string.Join(';', normalized);
    }

    public void SetBaseLocation(decimal? baseLatitude, decimal? baseLongitude)
    {
        if (baseLatitude.HasValue != baseLongitude.HasValue)
        {
            throw new ArgumentException("baseLatitude and baseLongitude must be supplied together.");
        }

        if (!baseLatitude.HasValue)
        {
            this.BaseLatitude = null;
            this.BaseLongitude = null;
            return;
        }

        if (baseLatitude.Value < -90m || baseLatitude.Value > 90m)
        {
            throw new ArgumentOutOfRangeException(nameof(baseLatitude), "baseLatitude must be between -90 and 90.");
        }

        if (baseLongitude.Value < -180m || baseLongitude.Value > 180m)
        {
            throw new ArgumentOutOfRangeException(nameof(baseLongitude), "baseLongitude must be between -180 and 180.");
        }

        this.BaseLatitude = Math.Round(baseLatitude.Value, 6, MidpointRounding.AwayFromZero);
        this.BaseLongitude = Math.Round(baseLongitude.Value, 6, MidpointRounding.AwayFromZero);
    }

    public void SetAvailability(WorkerAvailabilityStatus availabilityStatus)
    {
        this.AvailabilityStatus = availabilityStatus;
    }

    public void Activate()
    {
        this.IsActive = true;
    }

    public void Deactivate()
    {
        this.IsActive = false;
    }

    private static string NormalizeWorkerCode(string workerCode)
    {
        return DomainGuards.RequiredText(workerCode, nameof(workerCode), "workerCode is required.", 32)
            .Trim()
            .ToUpperInvariant();
    }

    private static decimal NormalizeRating(decimal internalRating)
    {
        if (internalRating < 1.0m || internalRating > 5.0m)
        {
            throw new ArgumentOutOfRangeException(nameof(internalRating), "internalRating must be between 1.0 and 5.0.");
        }

        var rounded = Math.Round(internalRating, 1, MidpointRounding.AwayFromZero);
        if (rounded != internalRating)
        {
            throw new ArgumentOutOfRangeException(nameof(internalRating), "internalRating must use one decimal place at most.");
        }

        return rounded;
    }

    private static string NormalizeSkill(string skill)
    {
        return DomainGuards.RequiredText(skill, nameof(skill), "skill entry is required.", MaxSkillLength)
            .Trim()
            .ToUpperInvariant();
    }
}
