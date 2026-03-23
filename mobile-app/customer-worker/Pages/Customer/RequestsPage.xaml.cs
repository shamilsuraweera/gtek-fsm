namespace GTEK.FSM.MobileApp.Pages.Customer;

using System.Collections.ObjectModel;

public partial class RequestsPage : ContentPage
{
    private readonly ObservableCollection<CustomerRequestViewModel> _requests;

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
