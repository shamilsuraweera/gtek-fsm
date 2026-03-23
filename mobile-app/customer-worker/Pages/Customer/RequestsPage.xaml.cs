namespace GTEK.FSM.MobileApp.Pages.Customer;

using System.Collections.ObjectModel;
using GTEK.FSM.MobileApp.Services.Api;
using Microsoft.Extensions.DependencyInjection;

public partial class RequestsPage : ContentPage
{
    private readonly ObservableCollection<CustomerRequestViewModel> _requests;
    private readonly IRequestQueryService _requestQueryService;
    private CustomerRequestViewModel _selectedRequest;

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

        RequestsCollectionView.ItemsSource = _requests;
        RequestsCollectionView.SelectedItem = _requests[0];
        RenderRequestDetail(_requests[0]);

        _requestQueryService = Application.Current?.Handler?.MauiContext?.Services?.GetService<IRequestQueryService>();
        _ = LoadLiveRequestsAsync();
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
        }
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

    private static int ResolveStageIndex(string stage)
    {
        var normalized = stage.Trim().ToLowerInvariant();
        return normalized switch
        {
            "new" => 0,
            "submitted" => 0,
            "scheduled" => 1,
            "assigned" => 1,
            "onroute" => 2,
            "inprogress" => 2,
            "in_progress" => 2,
            "completed" => 3,
            _ => 0,
        };
    }

    private static Color ResolveStageColor(string stage)
    {
        var normalized = stage.Trim().ToLowerInvariant();
        return normalized switch
        {
            "completed" => Color.FromArgb("#166534"),
            "scheduled" => Color.FromArgb("#166534"),
            "assigned" => Color.FromArgb("#0F6ABD"),
            "onroute" => Color.FromArgb("#B45309"),
            "inprogress" => Color.FromArgb("#0F6ABD"),
            "in_progress" => Color.FromArgb("#0F6ABD"),
            _ => Color.FromArgb("#6B7280"),
        };
    }
}

internal sealed class CustomerRequestViewModel
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

    public string EtaText { get; }

    public string StatusLabel { get; }

    public Color StatusColor { get; }

    public int CurrentStage { get; }
}
