namespace GTEK.FSM.MobileApp.Pages.Worker;

using System.Collections.ObjectModel;

public partial class JobsPage : ContentPage
{
    private readonly ObservableCollection<WorkerJobViewModel> _jobs;
    private WorkerJobViewModel _selectedJob;

    public JobsPage()
    {
        InitializeComponent();

        StatusPicker.ItemsSource = new[]
        {
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

        _selectedJob.Accepted = true;
        _selectedJob.StatusLabel = "Accepted";
        _selectedJob.StatusColor = Color.FromArgb("#166534");
        StatusResultLabel.Text = $"Assignment accepted for {_selectedJob.Id}.";
        RefreshJobsList();
        RenderSelectedJob(_selectedJob);
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

        _selectedJob.StatusLabel = newStatus;
        _selectedJob.StatusColor = ResolveStatusColor(newStatus);
        StatusResultLabel.Text = $"Published status '{newStatus}' for {_selectedJob.Id} at {DateTime.Now:t}.";

        if (newStatus == "Accepted")
        {
            _selectedJob.Accepted = true;
        }

        RefreshJobsList();
        RenderSelectedJob(_selectedJob);
    }

    private async void OnOpenMapClicked(object sender, EventArgs e)
    {
        if (_selectedJob is null)
        {
            return;
        }

        await DisplayAlertAsync("Navigation", $"Map launch placeholder for {_selectedJob.Location}.", "OK");
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
        return status switch
        {
            "Completed" => Color.FromArgb("#166534"),
            "In Progress" => Color.FromArgb("#0F6ABD"),
            "On Site" => Color.FromArgb("#0F6ABD"),
            "On Route" => Color.FromArgb("#B45309"),
            "Accepted" => Color.FromArgb("#166534"),
            _ => Color.FromArgb("#6B7280"),
        };
    }
}

internal sealed class WorkerJobViewModel
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

    public string StatusLabel { get; set; }

    public Color StatusColor { get; set; }

    public bool Accepted { get; set; }
}
