using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;
using Patient_Information_System_CS.ViewModels;

namespace Patient_Information_System_CS.Views.Common
{
    public partial class AccountSettingsView : UserControl
    {
        private readonly HospitalDataService _dataService = HospitalDataService.Instance;
        private readonly MainViewModel _mainViewModel;

        public AccountSettingsView(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            InitializeComponent();
            Loaded += AccountSettingsView_Loaded;
        }

        private void AccountSettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= AccountSettingsView_Loaded;
            LoadProfile();
        }

        private void LoadProfile()
        {
            if (_mainViewModel.CurrentUser is null)
            {
                SetStatus("No active user is currently signed in.", Brushes.Firebrick);
                return;
            }

            try
            {
                var profile = _dataService.GetAccountProfile(_mainViewModel.CurrentUser.UserId);

                UsernameTextBox.Text = profile.Username;
                GivenNameTextBox.Text = profile.GivenName;
                MiddleNameTextBox.Text = profile.MiddleName ?? string.Empty;
                LastNameTextBox.Text = profile.LastName;
                SuffixTextBox.Text = profile.Suffix ?? string.Empty;

                NewPasswordBox.Password = string.Empty;
                ConfirmPasswordBox.Password = string.Empty;

                SetStatus("Profile loaded successfully.", Brushes.ForestGreen);
            }
            catch (Exception ex)
            {
                SetStatus($"Failed to load profile: {ex.Message}", Brushes.Firebrick);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mainViewModel.CurrentUser is null)
            {
                SetStatus("No active user is currently signed in.", Brushes.Firebrick);
                return;
            }

            var givenName = GivenNameTextBox.Text?.Trim() ?? string.Empty;
            var middleName = MiddleNameTextBox.Text?.Trim();
            var lastName = LastNameTextBox.Text?.Trim() ?? string.Empty;
            var suffix = SuffixTextBox.Text?.Trim();
            var newPassword = NewPasswordBox.Password?.Trim();
            var confirmPassword = ConfirmPasswordBox.Password?.Trim();
            var username = UsernameTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username))
            {
                SetStatus("Username is required.", Brushes.Firebrick);
                return;
            }

            if (string.IsNullOrWhiteSpace(givenName) || string.IsNullOrWhiteSpace(lastName))
            {
                SetStatus("First and last name are required.", Brushes.Firebrick);
                return;
            }

            if (!string.IsNullOrEmpty(newPassword) || !string.IsNullOrEmpty(confirmPassword))
            {
                if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
                {
                    SetStatus("New password and confirmation do not match.", Brushes.Firebrick);
                    return;
                }

                if (newPassword!.Length < 6)
                {
                    SetStatus("Password must be at least 6 characters long.", Brushes.Firebrick);
                    return;
                }
            }

            var update = new AccountProfileUpdate
            {
                UserId = _mainViewModel.CurrentUser.UserId,
                Username = username,
                GivenName = givenName,
                MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName,
                LastName = lastName,
                Suffix = string.IsNullOrWhiteSpace(suffix) ? null : suffix,
                NewPassword = string.IsNullOrWhiteSpace(newPassword) ? null : newPassword
            };

            try
            {
                var updatedAccount = _dataService.UpdateAccountProfile(update);
                _mainViewModel.UpdateCurrentUser(updatedAccount);
                LoadProfile();
                SetStatus("Account details updated successfully.", Brushes.ForestGreen);
            }
            catch (Exception ex)
            {
                SetStatus($"Failed to update account: {ex.Message}", Brushes.Firebrick);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            LoadProfile();
        }

        private void SetStatus(string message, Brush brush)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = brush;
        }
    }
}
