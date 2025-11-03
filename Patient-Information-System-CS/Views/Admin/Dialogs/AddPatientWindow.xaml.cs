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
        private readonly IReadOnlyList<UserAccount> _nurses;
        private readonly IReadOnlyList<RoomStatus> _rooms;

        public AddPatientWindow(IReadOnlyList<UserAccount> doctors,
                                IReadOnlyList<UserAccount> nurses,
                                IReadOnlyList<RoomStatus> rooms)
        {
            InitializeComponent();
            _doctors = doctors;
            _nurses = nurses;
            _rooms = rooms;

            Loaded += OnLoaded;
        }

        public string FullName => FullNameTextBox.Text.Trim();
        public string Email => EmailTextBox.Text.Trim();
        public string ContactNumber => ContactTextBox.Text.Trim();
        public string Address => AddressTextBox.Text.Trim();
        public DateTime DateOfBirth => BirthDatePicker.SelectedDate ?? DateTime.Today.AddYears(-30);
        public int? SelectedDoctorId => DoctorComboBox.SelectedItem is UserAccount account ? account.UserId : null;
        public int? SelectedNurseId => NurseComboBox.SelectedItem is UserAccount account ? account.UserId : null;
        public string InsuranceProvider => InsuranceTextBox.Text.Trim();
        public string EmergencyContact => EmergencyTextBox.Text.Trim();
        public string EmergencyRelationship => EmergencyRelationshipTextBox.Text.Trim();
        public string Nationality => NationalityTextBox.Text.Trim();
        public string Sex => SexComboBox.SelectedValue as string ?? "U";
        public bool ShouldApprove => ApproveCheckBox.IsChecked == true;
        public bool IsCurrentlyAdmitted => CurrentlyAdmittedCheckBox.IsChecked == true;
        public string RoomAssignment => RoomComboBox.SelectedItem is RoomStatus room
            ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "Room {0} - {1}", room.RoomNumber, room.RoomType)
            : string.Empty;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            DoctorComboBox.ItemsSource = _doctors;
            NurseComboBox.ItemsSource = _nurses;
            RoomComboBox.ItemsSource = _rooms;

            if (_doctors.Count > 0 && DoctorComboBox.SelectedIndex < 0)
            {
                DoctorComboBox.SelectedIndex = 0;
            }

            if (_nurses.Count == 1)
            {
                NurseComboBox.SelectedIndex = 0;
            }

            BirthDatePicker.SelectedDate ??= DateTime.Today.AddYears(-30);
            SexComboBox.SelectedValue = "U";

            if (string.IsNullOrWhiteSpace(NationalityTextBox.Text))
            {
                NationalityTextBox.Text = "PH";
            }

            CurrentlyAdmittedCheckBox_OnChanged(this, new RoutedEventArgs());
        }

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

            if (SelectedDoctorId is null)
            {
                ShowError("Please select an attending doctor.");
                return;
            }

            if (IsCurrentlyAdmitted && RoomComboBox.SelectedItem is not RoomStatus)
            {
                ShowError("Please select a room for admitted patients.");
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
            var enable = CurrentlyAdmittedCheckBox.IsChecked == true;
            RoomComboBox.IsEnabled = enable;

            if (!enable)
            {
                RoomComboBox.SelectedIndex = -1;
            }
            else if (RoomComboBox.SelectedIndex < 0 && RoomComboBox.Items.Count > 0)
            {
                RoomComboBox.SelectedIndex = 0;
            }
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }
    }
}
