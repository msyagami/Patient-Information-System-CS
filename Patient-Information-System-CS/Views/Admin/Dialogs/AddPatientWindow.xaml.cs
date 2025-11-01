using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Patient_Information_System_CS.Models;

namespace Patient_Information_System_CS.Views.Admin.Dialogs
{
    public partial class AddPatientWindow : Window
    {
        private readonly IReadOnlyList<UserAccount> _doctors;

        public AddPatientWindow(IReadOnlyList<UserAccount> doctors)
        {
            InitializeComponent();
            _doctors = doctors;
            DoctorComboBox.ItemsSource = _doctors;
            BirthDatePicker.SelectedDate = DateTime.Today.AddYears(-30);
            CurrentlyAdmittedCheckBox_OnChanged(this, new RoutedEventArgs());
        }

        public string FullName => FullNameTextBox.Text.Trim();
        public string Email => EmailTextBox.Text.Trim();
        public string ContactNumber => ContactTextBox.Text.Trim();
        public string Address => AddressTextBox.Text.Trim();
        public DateTime DateOfBirth => BirthDatePicker.SelectedDate ?? DateTime.Today.AddYears(-30);
        public int? SelectedDoctorId => DoctorComboBox.SelectedItem is UserAccount account ? account.UserId : null;
        public string InsuranceProvider => InsuranceTextBox.Text.Trim();
        public string EmergencyContact => EmergencyTextBox.Text.Trim();
        public bool ShouldApprove => ApproveCheckBox.IsChecked == true;
        public bool IsCurrentlyAdmitted => CurrentlyAdmittedCheckBox.IsChecked == true;
        public string RoomAssignment => RoomTextBox.Text.Trim();

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FullName))
            {
                ShowError("Full name is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(ContactNumber))
            {
                ShowError("Contact number is required.");
                return;
            }

            if (IsCurrentlyAdmitted && string.IsNullOrWhiteSpace(RoomAssignment))
            {
                ShowError("Room assignment is required for admitted patients.");
                return;
            }

            ErrorTextBlock.Visibility = Visibility.Collapsed;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void CurrentlyAdmittedCheckBox_OnChanged(object sender, RoutedEventArgs e)
        {
            RoomTextBox.IsEnabled = CurrentlyAdmittedCheckBox.IsChecked == true;
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }
    }
}
