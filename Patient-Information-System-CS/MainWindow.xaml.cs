using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;
using Patient_Information_System_CS.Views.Admin;

namespace Patient_Information_System_CS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly AuthenticationService _authenticationService;
        private readonly HospitalDataService _dataService;
        private UserRole _selectedRole = UserRole.Admin;

        public MainWindow()
        {
            InitializeComponent();
            _dataService = HospitalDataService.Instance;
            _authenticationService = new AuthenticationService(_dataService);
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!EnsureAdministratorProvisioned())
            {
                return;
            }

            UsernameTextBox.Focus();
        }

        private bool EnsureAdministratorProvisioned()
        {
            if (!_dataService.RequiresAdminProvisioning())
            {
                return true;
            }

            var provisioningWindow = new AdminProvisioningWindow(_dataService)
            {
                Owner = this
            };

            var result = provisioningWindow.ShowDialog();
            if (result != true || provisioningWindow.CreatedAccount is null)
            {
                Close();
                return false;
            }

            UsernameTextBox.Text = provisioningWindow.CreatedAccount.Username;
            PasswordBox.Password = provisioningWindow.PlainPassword;
            ShowFeedback("Administrator account created. Please sign in with the credentials provided.", isError: false);
            return true;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            AttemptLogin();
        }

        private void AttemptLogin()
        {
            var username = UsernameTextBox.Text?.Trim();
            var password = PasswordBox.Password;

            var result = _authenticationService.Authenticate(username, password);

            if (!result.IsAuthenticated)
            {
                ShowFeedback(result.Message, isError: true);
                return;
            }

            var account = result.Account!;

            if (account.Role != _selectedRole)
            {
                ShowFeedback($"This account belongs to the {account.Role} portal. Please switch to the correct portal.", isError: true);
                return;
            }

            ShowFeedback(string.Empty, isError: false);

            var dashboardWindow = new DashboardMainWindow(account);
            dashboardWindow.Show();
            Close();
        }

        private void RoleRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton radioButton)
            {
                return;
            }

            if (radioButton.Tag is string roleName && Enum.TryParse<UserRole>(roleName, out var role))
            {
                _selectedRole = role;
                if (FeedbackTextBlock is not null)
                {
                    FeedbackTextBlock.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Credential_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AttemptLogin();
            }
        }

        private void ShowFeedback(string message, bool isError)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                FeedbackTextBlock.Visibility = Visibility.Collapsed;
                FeedbackTextBlock.Text = string.Empty;
                return;
            }

            FeedbackTextBlock.Text = message;
            FeedbackTextBlock.Foreground = isError
                ? System.Windows.Media.Brushes.Firebrick
                : System.Windows.Media.Brushes.ForestGreen;
            FeedbackTextBlock.Visibility = Visibility.Visible;
        }
    }
}
