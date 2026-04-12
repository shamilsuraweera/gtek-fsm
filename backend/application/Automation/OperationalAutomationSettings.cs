namespace GTEK.FSM.Backend.Application.Automation;

public sealed class OperationalAutomationSettings
{
    public const string SectionName = "Automation";

    public bool Enabled { get; set; }

    public int IntervalSeconds { get; set; } = 300;

    public int ReminderCooldownHours { get; set; } = 24;

    public int SubscriptionExpiryReminderDays { get; set; } = 14;

    public int MaxActionsPerTenantPerRun { get; set; } = 10;

    public bool EnableSlaReminderWorkflow { get; set; } = true;

    public bool EnableSubscriptionExpiryReminderWorkflow { get; set; } = true;

    public TimeSpan GetInterval() => TimeSpan.FromSeconds(Math.Max(30, this.IntervalSeconds));

    public TimeSpan GetReminderCooldown() => TimeSpan.FromHours(Math.Max(1, this.ReminderCooldownHours));

    public int GetSubscriptionExpiryReminderDays() => Math.Max(1, this.SubscriptionExpiryReminderDays);

    public int GetMaxActionsPerTenantPerRun() => Math.Max(1, this.MaxActionsPerTenantPerRun);

    public bool TryValidate(out string validationError)
    {
        if (this.IntervalSeconds <= 0)
        {
            validationError = "Automation interval must be greater than zero seconds.";
            return false;
        }

        if (this.ReminderCooldownHours <= 0)
        {
            validationError = "Automation reminder cooldown must be greater than zero hours.";
            return false;
        }

        if (this.SubscriptionExpiryReminderDays <= 0)
        {
            validationError = "Subscription expiry reminder days must be greater than zero.";
            return false;
        }

        if (this.MaxActionsPerTenantPerRun <= 0)
        {
            validationError = "Automation max actions per tenant per run must be greater than zero.";
            return false;
        }

        validationError = string.Empty;
        return true;
    }
}