using System;
using System.Windows;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;

namespace Patient_Information_System_CS.Views.Admin
{
    public partial class AdminProvisioningWindow : Window
    {
        private readonly HospitalDataService _dataService;

        public AdminProvisioningWindow(HospitalDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            InitializeComponent();
            Loaded += OnLoaded;
        }

        public UserAccount? CreatedAccount { get; private set; }
        public string PlainPassword { get; private set; } = string.Empty;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            BirthDatePicker.SelectedDate ??= DateTime.Today.AddYears(-30);
            AddressTextBox.Text = string.IsNullOrWhiteSpace(AddressTextBox.Text)
                ? "Hospital Campus"
                : AddressTextBox.Text;
        }

        private void CreateAdministrator_Click(object sender, RoutedEventArgs e)
        {
            HideFeedback();

            var password = PasswordBox.Password?.Trim() ?? string.Empty;
            var confirmPassword = ConfirmPasswordBox.Password?.Trim() ?? string.Empty;
            var givenName = GivenNameTextBox.Text?.Trim() ?? string.Empty;
            var lastName = LastNameTextBox.Text?.Trim() ?? string.Empty;

            if (givenName.Length == 0 || lastName.Length == 0)
            {
                ShowFeedback("Please provide the administrator's given and last name.");
                return;
            }

            if (password.Length < 6)
            {
                ShowFeedback("Password must be at least 6 characters long.");
                return;
            }

            if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                ShowFeedback("Passwords do not match. Please confirm and try again.");
                return;
            }

            if (BirthDatePicker.SelectedDate is null)
            {
                ShowFeedback("Please select a valid birth date.");
                return;
            }

            var request = new AdminProvisioningRequest
            {
                Username = UsernameTextBox.Text?.Trim() ?? string.Empty,
                Password = password,
                GivenName = givenName,
                LastName = lastName,
                MiddleName = MiddleNameTextBox.Text?.Trim(),
                Email = EmailTextBox.Text?.Trim() ?? string.Empty,
                ContactNumber = ContactNumberTextBox.Text?.Trim() ?? string.Empty,
                Address = AddressTextBox.Text?.Trim() ?? "Hospital Campus",
                EmergencyContact = "Primary Contact",
                RelationshipToEmergencyContact = "Self",
                Sex = "Unspecified",
                Nationality = "PH",
                BirthDate = BirthDatePicker.SelectedDate.Value.Date
            };

            try
            {
                var account = _dataService.ProvisionFirstAdmin(request);
                CreatedAccount = account;
                PlainPassword = password;

                MessageBox.Show(
                    "Administrator account created successfully. You can now sign in using the credentials provided.",
                    "Administrator Provisioned",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowFeedback(ex.Message);
            }
        }

        private void HideFeedback()
        {
            FeedbackTextBlock.Visibility = Visibility.Collapsed;
            FeedbackTextBlock.Text = string.Empty;
        }

        private void ShowFeedback(string message)
        {
            FeedbackTextBlock.Text = message;
            FeedbackTextBlock.Visibility = string.IsNullOrWhiteSpace(message)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }
}
