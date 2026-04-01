namespace GTEK.FSM.MobileApp.Pages.Worker;

using System.Collections.ObjectModel;
using GTEK.FSM.MobileApp.Services.Api;
using GTEK.FSM.MobileApp.Services.Realtime;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Realtime;
using Microsoft.Extensions.DependencyInjection;

public partial class JobsPage : ContentPage, IDisposable
{
    private readonly ObservableCollection<WorkerJobViewModel> _jobs;
    private readonly IJobQueryService _jobQueryService;
    private readonly IMobileOperationalRealtimeClient? _realtimeClient;
    private readonly IDisposable? _assignmentSubscription;
    private WorkerJobViewModel _selectedJob;

    public JobsPage()
    {
        InitializeComponent();

        StatusPicker.ItemsSource = new[]
        {
            "Available",
            "Accepted",
            "On Route",
            "On Site",
            "In Progress",
            "Completed",
        };

        _jobs = new ObservableCollection<WorkerJobViewModel>
        {
            new WorkerJobViewModel(
                id: "JOB-884",
                title: "Rooftop AC Compressor Fault",
                description: "Inspect compressor overload alert and restore cooling output.",
                location: "Lakepoint Towers, Colombo 03",
                priorityLabel: "High",
                priorityColor: Color.FromArgb("#B45309"),
                statusLabel: "Available",
                statusColor: Color.FromArgb("#0F6ABD"),
                accepted: false),
            new WorkerJobViewModel(
                id: "JOB-876",
                title: "Generator Runtime Inspection",
                description: "Run full safety and load handover checklist.",
                location: "Central Arcade, Colombo 07",
                priorityLabel: "Medium",
                priorityColor: Color.FromArgb("#0F6ABD"),
                statusLabel: "Accepted",
                statusColor: Color.FromArgb("#166534"),
                accepted: true),
            new WorkerJobViewModel(
                id: "JOB-861",
                title: "Water Pump Pressure Recalibration",
                description: "Calibrate pressure switches and verify line stability.",
                location: "Palm Residency, Nugegoda",
                priorityLabel: "Low",
                priorityColor: Color.FromArgb("#1D4ED8"),
                statusLabel: "In Progress",
                statusColor: Color.FromArgb("#166534"),
                accepted: true),
        };

        JobsCollectionView.ItemsSource = _jobs;
        JobsCollectionView.SelectedItem = _jobs[0];
        RenderSelectedJob(_jobs[0]);

        _jobQueryService = Application.Current?.Handler?.MauiContext?.Services?.GetService<IJobQueryService>();
        _realtimeClient = Application.Current?.Handler?.MauiContext?.Services?.GetService<IMobileOperationalRealtimeClient>();
        if (_realtimeClient is not null)
        {
            _assignmentSubscription = _realtimeClient.SubscribeToAssignmentUpdates(HandleAssignmentUpdateAsync);
            _ = _realtimeClient.EnsureConnectedAsync();
        }

        _ = LoadLiveJobsAsync();
    }

    public void Dispose()
    {
        _assignmentSubscription?.Dispose();
    }

    private void OnJobSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is WorkerJobViewModel selected)
        {
            RenderSelectedJob(selected);
        }
    }

    private async void OnAcceptAssignmentClicked(object sender, EventArgs e)
    {
        if (_selectedJob is null)
        {
            return;
        }

        if (_selectedJob.Accepted)
        {
            await DisplayAlertAsync("Assignment", "This assignment is already accepted.", "OK");
            return;
        }

        var updated = _selectedJob with
        {
            Accepted = true,
            StatusLabel = "Accepted",
            StatusColor = Color.FromArgb("#166534"),
        };

        ReplaceJob(_selectedJob, updated);
        StatusResultLabel.Text = $"Assignment accepted for {_selectedJob.Id}.";
    }

    private async void OnPublishStatusClicked(object sender, EventArgs e)
    {
        if (_selectedJob is null)
        {
            return;
        }

        if (StatusPicker.SelectedItem is not string newStatus)
        {
            await DisplayAlertAsync("Status", "Please choose a status before publishing.", "OK");
            return;
        }

        var updated = _selectedJob with
        {
            StatusLabel = newStatus,
            StatusColor = ResolveStatusColor(newStatus),
            Accepted = newStatus == "Accepted" || _selectedJob.Accepted,
        };

        ReplaceJob(_selectedJob, updated);
        StatusResultLabel.Text = $"Published status '{newStatus}' for {updated.Id} at {DateTime.Now:t}.";
    }

    private async void OnOpenMapClicked(object sender, EventArgs e)
    {
        if (_selectedJob is null)
        {
            return;
        }

        await DisplayAlertAsync("Navigation", $"Map launch placeholder for {_selectedJob.Location}.", "OK");
    }

    private void OnStartTravelClicked(object sender, EventArgs e)
    {
        ApplyQuickStatus("On Route");
    }

    private void OnMarkOnSiteClicked(object sender, EventArgs e)
    {
        ApplyQuickStatus("On Site");
    }

    private void OnCompleteJobClicked(object sender, EventArgs e)
    {
        ApplyQuickStatus("Completed");
    }

    private void RenderSelectedJob(WorkerJobViewModel selected)
    {
        _selectedJob = selected;
        SelectedJobTitleLabel.Text = selected.Title;
        SelectedJobDescriptionLabel.Text = selected.Description;
        SelectedJobMetaLabel.Text = $"{selected.Id} • {selected.Location} • {selected.StatusLabel}";
        AcceptAssignmentButton.IsEnabled = !selected.Accepted;
        StatusPicker.SelectedItem = selected.StatusLabel;
        StatusResultLabel.Text ??= string.Empty;
    }

    private void RefreshJobsList()
    {
        JobsCollectionView.ItemsSource = null;
        JobsCollectionView.ItemsSource = _jobs;
    }

    private static Color ResolveStatusColor(string status)
    {
        return MobileOperationalRealtimeMapper.ResolveJobStatusColor(status);
    }

    private void ApplyQuickStatus(string quickStatus)
    {
        if (_selectedJob is null)
        {
            return;
        }

        var updated = _selectedJob with
        {
            StatusLabel = quickStatus,
            StatusColor = ResolveStatusColor(quickStatus),
            Accepted = quickStatus == "Accepted" || _selectedJob.Accepted,
        };

        ReplaceJob(_selectedJob, updated);
        StatusPicker.SelectedItem = quickStatus;
        StatusResultLabel.Text = $"Quick action applied: '{quickStatus}' for {updated.Id} at {DateTime.Now:t}.";
    }

    private async Task LoadLiveJobsAsync()
    {
        if (_jobQueryService is null)
        {
            return;
        }

        var result = await _jobQueryService.QueryJobsAsync();
        if (!result.IsLive || result.Items.Count == 0)
        {
            return;
        }

        _jobs.Clear();
        foreach (var item in result.Items)
        {
            var status = NormalizeStatus(item.Status);
            _jobs.Add(new WorkerJobViewModel(
                id: item.JobId ?? "JOB-UNKNOWN",
                title: item.Title ?? "Untitled Job",
                description: BuildDescription(item),
                location: BuildLocation(item),
                priorityLabel: "Live",
                priorityColor: Color.FromArgb("#0F6ABD"),
                statusLabel: status,
                statusColor: ResolveStatusColor(status),
                accepted: IsAcceptedStatus(status)));
        }

        if (_jobs.Count > 0)
        {
            JobsCollectionView.SelectedItem = _jobs[0];
            RenderSelectedJob(_jobs[0]);
        }
    }

    private static string BuildDescription(GTEK.FSM.Shared.Contracts.Api.Contracts.Jobs.Responses.GetJobsResponse item)
    {
        var requestReference = string.IsNullOrWhiteSpace(item.RequestId) ? "N/A" : item.RequestId;
        return $"Linked request: {requestReference}";
    }

    private static string BuildLocation(GTEK.FSM.Shared.Contracts.Api.Contracts.Jobs.Responses.GetJobsResponse item)
    {
        if (!string.IsNullOrWhiteSpace(item.AssignedTo))
        {
            return $"Assigned to {item.AssignedTo}";
        }

        return "Field location unavailable";
    }

    private static bool IsAcceptedStatus(string status)
    {
        return MobileOperationalRealtimeMapper.IsAcceptedStatus(status);
    }

    private static string NormalizeStatus(string status)
    {
        return MobileOperationalRealtimeMapper.NormalizeStatus(status);
    }

    private Task HandleAssignmentUpdateAsync(JobAssignmentUpdatedEvent payload)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var target = _jobs.FirstOrDefault(job => string.Equals(job.Id, payload.JobId, StringComparison.Ordinal));
            if (target is null)
            {
                return;
            }

            var normalizedStatus = NormalizeStatus(payload.AssignmentStatus);
            var updated = target with
            {
                StatusLabel = normalizedStatus,
                StatusColor = ResolveStatusColor(normalizedStatus),
                Accepted = IsAcceptedStatus(normalizedStatus),
            };

            ReplaceJob(target, updated);
            StatusResultLabel.Text = $"Live update: {updated.Id} is now {normalizedStatus} ({payload.UpdatedAtUtc:t}).";
        });

        return Task.CompletedTask;
    }

    private void ReplaceJob(WorkerJobViewModel previous, WorkerJobViewModel updated)
    {
        var index = _jobs.IndexOf(previous);
        if (index < 0)
        {
            return;
        }

        _jobs[index] = updated;
        if (_selectedJob is not null && string.Equals(_selectedJob.Id, updated.Id, StringComparison.Ordinal))
        {
            RenderSelectedJob(updated);
        }
    }
}

internal sealed record WorkerJobViewModel
{
    public WorkerJobViewModel(
        string id,
        string title,
        string description,
        string location,
        string priorityLabel,
        Color priorityColor,
        string statusLabel,
        Color statusColor,
        bool accepted)
    {
        Id = id;
        Title = title;
        Description = description;
        Location = location;
        PriorityLabel = priorityLabel;
        PriorityColor = priorityColor;
        StatusLabel = statusLabel;
        StatusColor = statusColor;
        Accepted = accepted;
    }

    public string Id { get; }

    public string Title { get; }

    public string Description { get; }

    public string Location { get; }

    public string PriorityLabel { get; }

    public Color PriorityColor { get; }

    public string StatusLabel { get; init; }

    public Color StatusColor { get; init; }

    public bool Accepted { get; init; }
}
