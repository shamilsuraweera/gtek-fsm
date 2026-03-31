namespace GTEK.FSM.MobileApp.Pages.Customer;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();

        PreferredContactPicker.ItemsSource = new[]
        {
            "Phone",
            "Email",
            "SMS",
        };

        FullNameEntry.Text = "Alex Customer";
        PhoneEntry.Text = "+94 77 555 2310";
        EmailEntry.Text = "alex.customer@example.com";
        AddressEditor.Text = "No 14, Lake View Avenue, Colombo 05";
        PreferredContactPicker.SelectedIndex = 0;
        SaveStatusLabel.Text = string.Empty;
    }

    private async void OnSaveProfileClicked(object sender, EventArgs e)
    {
        SaveStatusLabel.Text = $"Profile updated at {DateTime.Now:t}";

        await DisplayAlertAsync(
            "Profile Saved",
            "Your profile changes are saved locally for this phase shell.",
            "OK");
    }
}
