using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;

namespace Patient_Information_System_CS.Views.Staff
{
    public partial class ReceptionistAdmissionView : UserControl
    {
        private readonly HospitalDataService _dataService = HospitalDataService.Instance;
        private readonly UserAccount? _staffAccount;
        private bool _isSubscribed;
        private int _entriesLimit = 10;
        private string _searchTerm = string.Empty;

        public ReceptionistAdmissionView(UserAccount? staffAccount)
        {
            _staffAccount = staffAccount;
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_isSubscribed)
            {
                _dataService.AdmissionsChanged += OnAdmissionsChanged;
                _isSubscribed = true;
            }

            EnsureDefaultFieldValues();
            PopulateDoctors();
            PopulateRooms();
            PopulateActiveAdmissions();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_isSubscribed)
            {
                _dataService.AdmissionsChanged -= OnAdmissionsChanged;
                _isSubscribed = false;
            }
        }

        private void OnAdmissionsChanged(object? sender, EventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() =>
                {
                    PopulateDoctors();
                    PopulateRooms();
                    PopulateActiveAdmissions();
                });
                return;
            }

            PopulateDoctors();
            PopulateRooms();
            PopulateActiveAdmissions();
        }

        private void EnsureDefaultFieldValues()
        {
            if (BirthDatePicker.SelectedDate is null)
            {
                BirthDatePicker.SelectedDate = DateTime.Today.AddYears(-30);
            }

            if (AdmissionDatePicker.SelectedDate is null)
            {
                AdmissionDatePicker.SelectedDate = DateTime.Today;
            }

            if (RoomComboBox.SelectedIndex < 0 && RoomComboBox.Items.Count > 0)
            {
                RoomComboBox.SelectedIndex = 0;
            }

            if (string.IsNullOrWhiteSpace(InsuranceTextBox.Text))
            {
                InsuranceTextBox.Text = "Not Provided";
            }
        }

        private void PopulateDoctors()
        {
            var doctors = _dataService.GetActiveDoctors().ToList();
            DoctorComboBox.ItemsSource = doctors;

            if (doctors.Count == 0)
            {
                DoctorComboBox.SelectedItem = null;
                DoctorComboBox.IsEnabled = false;
                return;
            }

            DoctorComboBox.IsEnabled = true;

            if (DoctorComboBox.SelectedItem is not UserAccount selected || !doctors.Contains(selected))
            {
                DoctorComboBox.SelectedIndex = 0;
            }
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

        private void PopulateActiveAdmissions()
        {
            if (!IsLoaded || ActivePatientsDataGrid is null)
            {
                return;
            }

            var rows = _dataService.GetCurrentAdmissions()
                .Select(CreateActiveRow)
                .Where(row => string.IsNullOrWhiteSpace(_searchTerm) ||
                              row.Name.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) ||
                              row.PatientNumber.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase))
                .OrderBy(row => row.Name)
                .ToList();

            var visibleRows = rows.Take(_entriesLimit).ToList();

            ActivePatientsDataGrid.ItemsSource = visibleRows;
            ActivePatientsEmptyTextBlock.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ActivePatientsSummaryTextBlock.Text = rows.Count switch
            {
                0 => "Showing 0 admissions",
                _ => $"Showing {visibleRows.Count} of {rows.Count} admissions"
            };
        }

        private ActivePatientRow CreateActiveRow(UserAccount account)
        {
            var profile = account.PatientProfile!;
            var doctorName = profile.AssignedDoctorId is int doctorId
                ? _dataService.GetDoctorById(doctorId)?.DisplayName ?? profile.AssignedDoctorName ?? "Unassigned"
                : string.IsNullOrWhiteSpace(profile.AssignedDoctorName) ? "Unassigned" : profile.AssignedDoctorName;

            return new ActivePatientRow
            {
                Account = account,
                PatientNumber = profile.PatientNumber,
                Name = account.DisplayName,
                AdmitDate = profile.AdmitDate?.ToString("MMM dd, yyyy", CultureInfo.CurrentCulture) ?? "-",
                Status = profile.IsCurrentlyAdmitted ? "Admitted" : "Inactive",
                Room = string.IsNullOrWhiteSpace(profile.RoomAssignment) ? "Not assigned" : profile.RoomAssignment,
                Doctor = doctorName
            };
        }

        private void AdmitPatientButton_Click(object sender, RoutedEventArgs e)
        {
            var fullName = PatientNameTextBox.Text.Trim();
            var dateOfBirth = BirthDatePicker.SelectedDate ?? DateTime.Today.AddYears(-30);
            var contact = ContactTextBox.Text.Trim();
            var address = AddressTextBox.Text.Trim();
            var emergencyContactName = EmergencyContactTextBox.Text.Trim();
            var relationship = EmergencyRelationshipTextBox.Text.Trim();
            var emergencyContact = relationship.Length > 0
                ? string.Format(CultureInfo.CurrentCulture, "{0} ({1})", emergencyContactName, relationship)
                : emergencyContactName;
            var insurance = string.IsNullOrWhiteSpace(InsuranceTextBox.Text) ? "Not Provided" : InsuranceTextBox.Text.Trim();
            var doctor = DoctorComboBox.SelectedItem as UserAccount;
            var roomAssignment = GetRoomSelection();
            var admitDate = AdmissionDatePicker.SelectedDate ?? DateTime.Today;

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(contact) || string.IsNullOrWhiteSpace(address))
            {
                MessageBox.Show("Please complete the patient's name, contact number, and address before admitting.",
                    "Missing Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(emergencyContact))
            {
                MessageBox.Show("Please provide at least one emergency contact.",
                    "Missing Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var newPatient = _dataService.AdmitNewPatient(
                fullName,
                dateOfBirth,
                contact,
                address,
                emergencyContact,
                insurance,
                doctor,
                roomAssignment,
                admitDate);

            MessageBox.Show(
                $"{newPatient.DisplayName} is now admitted.\n\nUsername: {newPatient.Username}\nTemporary Password: {newPatient.Password}",
                "Admission Successful",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            ClearAdmissionForm();
            PopulateActiveAdmissions();
        }

        private void DischargePatientButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { CommandParameter: UserAccount patient })
            {
                return;
            }

            _dataService.DischargePatient(patient);

            MessageBox.Show(
                $"{patient.DisplayName} has been marked for discharge and billing.",
                "Discharge Initiated",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void EntriesComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EntriesComboBox.SelectedItem is ComboBoxItem { Content: string content } && int.TryParse(content, out var limit))
            {
                _entriesLimit = limit;
            }
            else
            {
                _entriesLimit = 10;
            }

            PopulateActiveAdmissions();
        }

        private void SearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchTerm = SearchTextBox.Text.Trim();
            PopulateActiveAdmissions();
        }

        private void ClearAdmissionForm()
        {
            PatientNameTextBox.Clear();
            BirthDatePicker.SelectedDate = DateTime.Today.AddYears(-30);
            ContactTextBox.Clear();
            AddressTextBox.Clear();
            EmergencyContactTextBox.Clear();
            EmergencyRelationshipTextBox.Clear();
            InsuranceTextBox.Text = "Not Provided";
            AdmissionDatePicker.SelectedDate = DateTime.Today;
            if (RoomComboBox.Items.Count > 0)
            {
                RoomComboBox.SelectedIndex = 0;
            }

            PopulateDoctors();
        }

        private string GetRoomSelection()
        {
            return RoomComboBox.SelectedItem switch
            {
                RoomOption room => room.DisplayLabel,
                _ when !string.IsNullOrWhiteSpace(RoomComboBox.Text) => RoomComboBox.Text,
                _ => "Room 101 - General Ward"
            };
        }

        private sealed class ActivePatientRow
        {
            public UserAccount Account { get; set; } = null!;
            public string PatientNumber { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string AdmitDate { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string Room { get; set; } = string.Empty;
            public string Doctor { get; set; } = string.Empty;
        }
    }
}
