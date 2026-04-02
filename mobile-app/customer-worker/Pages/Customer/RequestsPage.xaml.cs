namespace GTEK.FSM.MobileApp.Pages.Customer;

using System.Collections.ObjectModel;
using GTEK.FSM.MobileApp.Services.Api;
using GTEK.FSM.MobileApp.Services.Realtime;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Realtime;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;
using Microsoft.Extensions.DependencyInjection;

public partial class RequestsPage : ContentPage, IDisposable, IQueryAttributable
{
    private readonly ObservableCollection<CustomerRequestViewModel> _requests;
    private readonly ObservableCollection<RequestCategoryViewModel> _categories;
    private readonly IRequestQueryService _requestQueryService;
    private readonly ICategoryQueryService _categoryQueryService;
    private readonly IServiceRequestCreationService _requestCreationService;
    private readonly IMobileOperationalRealtimeClient? _realtimeClient;
    private readonly IDisposable? _statusSubscription;
    private CustomerRequestViewModel _selectedRequest;
    private bool _isSubmitting;
    private string _pendingRequestId;

    public RequestsPage()
    {
        InitializeComponent();

        _requests = new ObservableCollection<CustomerRequestViewModel>
        {
            new CustomerRequestViewModel(
                id: "REQ-2304",
                title: "AC Cooling Issue",
                summary: "Unit is not cooling the second floor.",
                etaText: "Technician ETA: Today 2:30 PM",
                statusLabel: "In Progress",
                statusColor: Color.FromArgb("#0F6ABD"),
                currentStage: 2),
            new CustomerRequestViewModel(
                id: "REQ-2301",
                title: "Water Pressure Drop",
                summary: "Low pressure in kitchen and bathroom lines.",
                etaText: "Awaiting schedule confirmation",
                statusLabel: "Scheduled",
                statusColor: Color.FromArgb("#166534"),
                currentStage: 1),
            new CustomerRequestViewModel(
                id: "REQ-2294",
                title: "Generator Inspection",
                summary: "Routine preventive maintenance inspection.",
                etaText: "Completed yesterday",
                statusLabel: "Completed",
                statusColor: Color.FromArgb("#166534"),
                currentStage: 3),
        };

            _categories = new ObservableCollection<RequestCategoryViewModel>();

        RequestsCollectionView.ItemsSource = _requests;
            CategoryPicker.ItemsSource = _categories;
        RequestsCollectionView.SelectedItem = _requests[0];
        RenderRequestDetail(_requests[0]);
            CreateRequestFeedbackLabel.Text = string.Empty;

        _requestQueryService = Application.Current?.Handler?.MauiContext?.Services?.GetService<IRequestQueryService>();
            _categoryQueryService = Application.Current?.Handler?.MauiContext?.Services?.GetService<ICategoryQueryService>();
            _requestCreationService = Application.Current?.Handler?.MauiContext?.Services?.GetService<IServiceRequestCreationService>();
        _realtimeClient = Application.Current?.Handler?.MauiContext?.Services?.GetService<IMobileOperationalRealtimeClient>();
        if (_realtimeClient is not null)
        {
            _statusSubscription = _realtimeClient.SubscribeToStatusUpdates(HandleStatusUpdateAsync);
            _ = _realtimeClient.EnsureConnectedAsync();
        }

        _ = LoadLiveRequestsAsync();
        _ = LoadCategoriesAsync();
    }

    public void Dispose()
    {
        _statusSubscription?.Dispose();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("requestId", out var requestIdValue) && requestIdValue is string requestId)
        {
            _pendingRequestId = Uri.UnescapeDataString(requestId);
            TrySelectPendingRequest();
        }
    }

    private void OnRequestSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is CustomerRequestViewModel request)
        {
            RenderRequestDetail(request);
        }
    }

    private void RenderRequestDetail(CustomerRequestViewModel request)
    {
        _selectedRequest = request;
        SelectedRequestTitleLabel.Text = request.Title;
        SelectedRequestDescriptionLabel.Text = request.Summary;
        SelectedRequestMetaLabel.Text = $"{request.Id} • {request.EtaText}";

        var stageLabels = new[] { "Submitted", "Scheduled", "In Progress", "Completed" };
        StatusTimelineLayout.Children.Clear();

        for (var index = 0; index < stageLabels.Length; index++)
        {
            var isComplete = index <= request.CurrentStage;
            var marker = isComplete ? "●" : "○";
            var color = isComplete
                ? Color.FromArgb("#0F6ABD")
                : Color.FromArgb("#6B7280");

            StatusTimelineLayout.Children.Add(new Label
            {
                Text = $"{marker} {stageLabels[index]}",
                TextColor = color,
                FontAttributes = isComplete ? FontAttributes.Bold : FontAttributes.None,
                FontSize = 14,
            });
        }
    }

    private async void OnEscalateRequestClicked(object sender, EventArgs e)
    {
        if (_selectedRequest is null)
        {
            return;
        }

        await DisplayAlertAsync("Escalation", $"Escalation pathway placeholder triggered for {_selectedRequest.Id}.", "OK");
    }

    private async void OnRefreshEtaClicked(object sender, EventArgs e)
    {
        if (_selectedRequest is null)
        {
            return;
        }

        await DisplayAlertAsync("ETA", $"ETA refresh pathway placeholder triggered for {_selectedRequest.Id}.", "OK");
    }

    private async void OnMessageTechnicianClicked(object sender, EventArgs e)
    {
        if (_selectedRequest is null)
        {
            return;
        }

        await DisplayAlertAsync("Technician", $"Messaging pathway placeholder opened for {_selectedRequest.Id}.", "OK");
    }

    private async Task LoadLiveRequestsAsync()
    {
        if (_requestQueryService is null)
        {
            return;
        }

        var result = await _requestQueryService.QueryRequestsAsync();
        if (!result.IsLive || result.Items.Count == 0)
        {
            return;
        }

        _requests.Clear();
        foreach (var item in result.Items)
        {
            var stage = item.Stage ?? "Submitted";
            var summary = item.Summary ?? "Request details unavailable.";
            var requestId = item.RequestId ?? "REQ-UNKNOWN";
            var title = BuildTitle(summary, requestId);

            _requests.Add(new CustomerRequestViewModel(
                id: requestId,
                title: title,
                summary: summary,
                etaText: $"Updated {item.UpdatedUtc:g}",
                statusLabel: stage,
                statusColor: ResolveStageColor(stage),
                currentStage: ResolveStageIndex(stage)));
        }

        if (_requests.Count > 0)
        {
            RequestsCollectionView.SelectedItem = _requests[0];
            RenderRequestDetail(_requests[0]);
            TrySelectPendingRequest();
        }
    }

    private async Task LoadCategoriesAsync()
    {
        if (_categoryQueryService is null)
        {
            return;
        }

        var result = await _categoryQueryService.QueryActiveCategoriesAsync();
        if (!result.IsLive || result.Items.Count == 0)
        {
            CreateRequestFeedbackLabel.Text = "Unable to load categories right now.";
            return;
        }

        _categories.Clear();
        foreach (var item in result.Items.OrderBy(category => category.SortOrder).ThenBy(category => category.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                continue;
            }

            _categories.Add(new RequestCategoryViewModel(
                CategoryId: item.CategoryId ?? string.Empty,
                Code: item.Code ?? string.Empty,
                Name: item.Name,
                SortOrder: item.SortOrder));
        }

        if (_categories.Count > 0)
        {
            CategoryPicker.SelectedItem = _categories[0];
            CreateRequestFeedbackLabel.Text = string.Empty;
        }
    }

    private async void OnSubmitRequestClicked(object sender, EventArgs e)
    {
        if (_isSubmitting)
        {
            return;
        }

        if (_requestCreationService is null)
        {
            CreateRequestFeedbackLabel.Text = "Request submission is unavailable.";
            return;
        }

        if (CategoryPicker.SelectedItem is not RequestCategoryViewModel selectedCategory)
        {
            CreateRequestFeedbackLabel.Text = "Select a category before submitting.";
            return;
        }

        var details = RequestDetailsEditor.Text?.Trim() ?? string.Empty;
        if (details.Length < 10)
        {
            CreateRequestFeedbackLabel.Text = "Provide at least 10 characters of details.";
            return;
        }

        var title = BuildRequestTitle(selectedCategory.Name, details);
        if (title.Length > 180)
        {
            CreateRequestFeedbackLabel.Text = "Combined category and details must be 180 characters or less.";
            return;
        }

        _isSubmitting = true;
        CreateRequestFeedbackLabel.Text = "Submitting request...";

        try
        {
            var creation = await _requestCreationService.CreateRequestAsync(title);
            if (!creation.IsSuccess)
            {
                CreateRequestFeedbackLabel.Text = "Request submission failed. Please try again.";
                return;
            }

            RequestDetailsEditor.Text = string.Empty;
            CreateRequestFeedbackLabel.Text = $"Request {creation.Request.RequestId} created.";

            await LoadLiveRequestsAsync();
            EnsureCreatedRequestVisible(creation.Request);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void EnsureCreatedRequestVisible(CreateServiceRequestResponse createdRequest)
    {
        var existing = _requests.FirstOrDefault(item => string.Equals(item.Id, createdRequest.RequestId, StringComparison.Ordinal));
        if (existing is null)
        {
            var fallback = new CustomerRequestViewModel(
                id: createdRequest.RequestId,
                title: createdRequest.Title,
                summary: createdRequest.Title,
                etaText: $"Updated {createdRequest.UpdatedAtUtc:g}",
                statusLabel: createdRequest.Status,
                statusColor: ResolveStageColor(createdRequest.Status),
                currentStage: ResolveStageIndex(createdRequest.Status));

            _requests.Insert(0, fallback);
            RequestsCollectionView.SelectedItem = fallback;
            RenderRequestDetail(fallback);
            return;
        }

        RequestsCollectionView.SelectedItem = existing;
        RenderRequestDetail(existing);
    }

    private static string BuildTitle(string summary, string requestId)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return requestId;
        }

        return summary.Length <= 36
            ? summary
            : $"{summary[..33]}...";
    }

    private static string BuildRequestTitle(string categoryName, string details)
    {
        return $"{categoryName}: {details}";
    }

    private void TrySelectPendingRequest()
    {
        if (string.IsNullOrWhiteSpace(_pendingRequestId) || _requests.Count == 0)
        {
            return;
        }

        var target = _requests.FirstOrDefault(request => string.Equals(request.Id, _pendingRequestId, StringComparison.OrdinalIgnoreCase));
        if (target is null)
        {
            return;
        }

        RequestsCollectionView.SelectedItem = target;
        RenderRequestDetail(target);
        _pendingRequestId = string.Empty;
    }

    private static int ResolveStageIndex(string stage)
    {
        return MobileOperationalRealtimeMapper.ResolveRequestStageIndex(stage);
    }

    private static Color ResolveStageColor(string stage)
    {
        return MobileOperationalRealtimeMapper.ResolveRequestStageColor(stage);
    }

    private Task HandleStatusUpdateAsync(ServiceRequestStatusUpdatedEvent payload)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var target = _requests.FirstOrDefault(request => string.Equals(request.Id, payload.RequestId, StringComparison.Ordinal));
            if (target is null)
            {
                return;
            }

            var updatedStatus = MobileOperationalRealtimeMapper.NormalizeStatus(payload.CurrentStatus);
            var updated = target with
            {
                StatusLabel = updatedStatus,
                StatusColor = ResolveStageColor(updatedStatus),
                CurrentStage = ResolveStageIndex(updatedStatus),
                EtaText = $"Updated {payload.UpdatedAtUtc:g}",
            };

            ReplaceRequest(target, updated);
        });

        return Task.CompletedTask;
    }

    private void ReplaceRequest(CustomerRequestViewModel previous, CustomerRequestViewModel updated)
    {
        var index = _requests.IndexOf(previous);
        if (index < 0)
        {
            return;
        }

        _requests[index] = updated;
        if (_selectedRequest is not null && string.Equals(_selectedRequest.Id, updated.Id, StringComparison.Ordinal))
        {
            RenderRequestDetail(updated);
        }
    }
}

internal sealed record RequestCategoryViewModel(
    string CategoryId,
    string Code,
    string Name,
    int SortOrder);

internal sealed record CustomerRequestViewModel
{
    public CustomerRequestViewModel(
        string id,
        string title,
        string summary,
        string etaText,
        string statusLabel,
        Color statusColor,
        int currentStage)
    {
        Id = id;
        Title = title;
        Summary = summary;
        EtaText = etaText;
        StatusLabel = statusLabel;
        StatusColor = statusColor;
        CurrentStage = currentStage;
    }

    public string Id { get; }

    public string Title { get; }

    public string Summary { get; }

    public string EtaText { get; init; }

    public string StatusLabel { get; init; }

    public Color StatusColor { get; init; }

    public int CurrentStage { get; init; }
}
