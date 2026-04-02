namespace GTEK.FSM.MobileApp.Pages.Worker;

using System.Collections.ObjectModel;
using GTEK.FSM.MobileApp.Services.Api;
using GTEK.FSM.MobileApp.Services.Realtime;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Jobs.Responses;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Realtime;
using Microsoft.Extensions.DependencyInjection;

public partial class JobsPage : ContentPage, IDisposable
{
    private static readonly string[] RequestLifecycleStatuses =
    {
        "Assigned",
        "InProgress",
        "OnHold",
        "Completed",
    };

    private readonly ObservableCollection<WorkerJobViewModel> _jobs;
    private readonly IJobQueryService _jobQueryService;
    private readonly IWorkerExecutionService _workerExecutionService;
    private readonly IMobileOperationalRealtimeClient? _realtimeClient;
    private readonly IDisposable? _assignmentSubscription;
    private WorkerJobViewModel _selectedJob;
    private bool _isSubmitting;

    public JobsPage()
    {
        InitializeComponent();

        StatusPicker.ItemsSource = RequestLifecycleStatuses;

        _jobs = new ObservableCollection<WorkerJobViewModel>
        {
            new WorkerJobViewModel(
                id: "JOB-884",
                requestId: "REQ-2304",
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
                requestId: "REQ-2301",
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
                requestId: "REQ-2294",
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
        _workerExecutionService = Application.Current?.Handler?.MauiContext?.Services?.GetService<IWorkerExecutionService>();
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
            _ = LoadSelectedJobExecutionContextAsync(selected);
        }
    }

    private async void OnAcceptAssignmentClicked(object sender, EventArgs e)
    {
        await ApplyTransitionAsync("Assigned", "Assignment accepted");
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

        await ApplyTransitionAsync(newStatus, $"Published status '{newStatus}'");
    }

    private async void OnRefreshDetailClicked(object sender, EventArgs e)
    {
        if (_selectedJob is null)
        {
            return;
        }

        await LoadSelectedJobExecutionContextAsync(_selectedJob);
    }

    private async void OnStartWorkClicked(object sender, EventArgs e)
    {
        await ApplyTransitionAsync("InProgress", "Work started");
    }

    private async void OnPlaceOnHoldClicked(object sender, EventArgs e)
    {
        await ApplyTransitionAsync("OnHold", "Request placed on hold");
    }

    private async void OnCompleteRequestClicked(object sender, EventArgs e)
    {
        await ApplyTransitionAsync("Completed", "Request completed");
    }

    private async void OnResumeWorkClicked(object sender, EventArgs e)
    {
        await ApplyTransitionAsync("InProgress", "Work resumed");
    }

    private void RenderSelectedJob(WorkerJobViewModel selected)
    {
        _selectedJob = selected;
        SelectedJobTitleLabel.Text = selected.Title;
        SelectedJobDescriptionLabel.Text = selected.Description;
        SelectedJobMetaLabel.Text = $"{selected.Id} • {selected.RequestId} • {selected.Location} • {selected.StatusLabel}";
        AcceptAssignmentButton.IsEnabled = !selected.Accepted;
        StatusPicker.SelectedItem = ToApiStatus(selected.StatusLabel);
        StatusResultLabel.Text ??= string.Empty;
    }

    private static Color ResolveStatusColor(string status)
    {
        return MobileOperationalRealtimeMapper.ResolveJobStatusColor(status);
    }

    private async Task LoadSelectedJobExecutionContextAsync(WorkerJobViewModel selected)
    {
        if (_workerExecutionService is null)
        {
            return;
        }

        var detail = await _workerExecutionService.GetJobDetailAsync(selected.Id);
        if (!detail.IsSuccess)
        {
            StatusResultLabel.Text = $"Unable to load job detail: {detail.Message}";
            return;
        }

        var requestId = detail.Detail.RequestId ?? selected.RequestId;
        var requestStatus = detail.Detail.RequestStatus ?? selected.StatusLabel;
        var requestRowVersion = selected.RequestRowVersion;

        if (!string.IsNullOrWhiteSpace(requestId))
        {
            var requestDetail = await _workerExecutionService.GetRequestDetailAsync(requestId);
            if (requestDetail.IsSuccess)
            {
                requestStatus = requestDetail.Detail.Status ?? requestStatus;
                requestRowVersion = requestDetail.Detail.RowVersion ?? requestRowVersion;
            }
            else
            {
                StatusResultLabel.Text = $"Loaded job detail, but request detail failed: {requestDetail.Message}";
            }
        }

        var normalizedStatus = NormalizeStatus(requestStatus);
        var updated = selected with
        {
            RequestId = requestId,
            RequestRowVersion = requestRowVersion,
            Title = detail.Detail.RequestTitle ?? selected.Title,
            Description = BuildDescription(detail.Detail, selected.Description),
            StatusLabel = normalizedStatus,
            StatusColor = ResolveStatusColor(normalizedStatus),
            Accepted = IsAcceptedStatus(normalizedStatus),
        };

        ReplaceJob(selected, updated);
        StatusPicker.SelectedItem = ToApiStatus(normalizedStatus);
    }

    private async Task ApplyTransitionAsync(string nextStatus, string successMessage)
    {
        if (_selectedJob is null || _workerExecutionService is null || _isSubmitting)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedJob.RequestId))
        {
            StatusResultLabel.Text = "Missing request linkage. Refresh detail and retry.";
            return;
        }

        _isSubmitting = true;

        try
        {
            var workingSelection = _selectedJob;
            if (string.IsNullOrWhiteSpace(workingSelection.RequestRowVersion))
            {
                await LoadSelectedJobExecutionContextAsync(workingSelection);
                workingSelection = _selectedJob;
            }

            var transition = await _workerExecutionService.TransitionRequestStatusAsync(
                requestId: workingSelection.RequestId,
                nextStatus: nextStatus,
                rowVersion: workingSelection.RequestRowVersion);

            if (!transition.IsSuccess)
            {
                StatusResultLabel.Text = transition.IsConflict
                    ? $"Conflict detected: {transition.Message}. Refreshing latest state."
                    : $"Status update failed: {transition.Message}";

                await LoadSelectedJobExecutionContextAsync(workingSelection);
                return;
            }

            var normalizedStatus = NormalizeStatus(transition.Transition.CurrentStatus);
            var updated = workingSelection with
            {
                StatusLabel = normalizedStatus,
                StatusColor = ResolveStatusColor(normalizedStatus),
                RequestRowVersion = transition.Transition.RowVersion ?? workingSelection.RequestRowVersion,
                Accepted = IsAcceptedStatus(normalizedStatus),
            };

            ReplaceJob(workingSelection, updated);
            StatusPicker.SelectedItem = ToApiStatus(transition.Transition.CurrentStatus);
            StatusResultLabel.Text = $"{successMessage} for {updated.Id} at {DateTime.Now:t}.";

            await LoadLiveJobsAsync();
        }
        finally
        {
            _isSubmitting = false;
        }
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
                requestId: item.RequestId ?? string.Empty,
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
            await LoadSelectedJobExecutionContextAsync(_jobs[0]);
        }
    }

    private static string BuildDescription(GetJobsResponse item)
    {
        var requestReference = string.IsNullOrWhiteSpace(item.RequestId) ? "N/A" : item.RequestId;
        return $"Linked request: {requestReference}";
    }

    private static string BuildDescription(GetJobDetailResponse detail, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(detail.RequestTitle))
        {
            return detail.RequestTitle;
        }

        return fallback;
    }

    private static string BuildLocation(GetJobsResponse item)
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

    private static string ToApiStatus(string status)
    {
        var normalized = NormalizeStatus(status).ToLowerInvariant();
        return normalized switch
        {
            "assigned" => "Assigned",
            "in progress" => "InProgress",
            "on hold" => "OnHold",
            "completed" => "Completed",
            _ => "Assigned",
        };
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
        string requestId,
        string title,
        string description,
        string location,
        string priorityLabel,
        Color priorityColor,
        string statusLabel,
        Color statusColor,
        bool accepted,
        string requestRowVersion = "")
    {
        Id = id;
        RequestId = requestId;
        Title = title;
        Description = description;
        Location = location;
        PriorityLabel = priorityLabel;
        PriorityColor = priorityColor;
        StatusLabel = statusLabel;
        StatusColor = statusColor;
        Accepted = accepted;
        RequestRowVersion = requestRowVersion;
    }

    public string Id { get; }

    public string RequestId { get; init; }

    public string Title { get; init; }

    public string Description { get; init; }

    public string Location { get; }

    public string PriorityLabel { get; }

    public Color PriorityColor { get; }

    public string StatusLabel { get; init; }

    public Color StatusColor { get; init; }

    public bool Accepted { get; init; }

    public string RequestRowVersion { get; init; }
}
