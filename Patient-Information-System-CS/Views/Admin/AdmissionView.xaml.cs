using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;

namespace Patient_Information_System_CS.Views.Admin
{
    public partial class AdmissionView : UserControl
    {
        private readonly HospitalDataService _dataService = HospitalDataService.Instance;

        public AdmissionView()
        {
            InitializeComponent();
            Loaded += AdmissionView_Loaded;
        }

        private void AdmissionView_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateDoctors();
            PopulateRooms();
            RefreshTables();
        }

        private void PopulateRooms()
        {
            var rooms = _dataService.GetRoomOptions();
            RoomComboBox.ItemsSource = rooms;

            if (rooms.Count > 0 && RoomComboBox.SelectedIndex < 0)
            {
                RoomComboBox.SelectedIndex = 0;
            }
        }

        private void PopulateDoctors()
        {
            var doctors = _dataService.GetActiveDoctors().ToList();
            DoctorComboBox.ItemsSource = doctors;

            if (doctors.Count > 0 && DoctorComboBox.SelectedIndex < 0)
            {
                DoctorComboBox.SelectedIndex = 0;
            }
        }

        private void RefreshTables()
        {
            CurrentAdmissionsGrid.ItemsSource = _dataService.GetCurrentAdmissions().ToList();
            ReactivationGrid.ItemsSource = _dataService.GetDeactivatedPatients().ToList();
        }

        private void ConfirmAdmissionButton_Click(object sender, RoutedEventArgs e)
        {
            var fullName = PatientNameTextBox.Text.Trim();
            var dateOfBirth = BirthDatePicker.SelectedDate ?? DateTime.Today.AddYears(-30);
            var contact = ContactTextBox.Text.Trim();
            var address = AddressTextBox.Text.Trim();
            var emergencyContact = EmergencyContactTextBox.Text.Trim();
            var insurance = InsuranceTextBox.Text.Trim();
            var roomAssignment = GetRoomSelection();
            var doctor = DoctorComboBox.SelectedItem as UserAccount;

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(contact) || string.IsNullOrWhiteSpace(address))
            {
                MessageBox.Show("Please complete the patient's name, contact, and address.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newPatient = _dataService.AdmitNewPatient(fullName,
                dateOfBirth,
                contact,
                address,
                emergencyContact,
                insurance,
                doctor,
                roomAssignment);

            MessageBox.Show(
                $"Admission complete for {newPatient.DisplayName}.\n\nUsername: {newPatient.Username}\nTemporary Password: {newPatient.Password}",
                "Admission Recorded",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            ClearAdmissionForm();
            RefreshTables();
        }

        private void DischargePatient_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: UserAccount patient })
            {
                return;
            }

            _dataService.DischargePatient(patient);
            RefreshTables();

            MessageBox.Show(
                $"{patient.DisplayName} has been marked for discharge and billing.",
                "Discharge Initiated",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ReactivatePatient_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: UserAccount patient })
            {
                return;
            }

            _dataService.ReactivatePatient(patient);
            RefreshTables();

            MessageBox.Show(
                $"{patient.DisplayName} has been reactivated and readmitted.",
                "Patient Reactivated",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private string GetRoomSelection()
        {
            if (RoomComboBox.SelectedItem is RoomOption room)
            {
                return room.DisplayLabel;
            }

            return RoomComboBox.Text switch
            {
                { Length: > 0 } text => text,
                _ => "Room 101 - General Ward"
            };
        }

        private void ClearAdmissionForm()
        {
            PatientNameTextBox.Clear();
            BirthDatePicker.SelectedDate = DateTime.Today.AddYears(-30);
            ContactTextBox.Clear();
            AddressTextBox.Clear();
            EmergencyContactTextBox.Clear();
            InsuranceTextBox.Text = "No Insurance";
            if (RoomComboBox.Items.Count > 0)
            {
                RoomComboBox.SelectedIndex = 0;
            }

            PopulateDoctors();
        }
    }
}
