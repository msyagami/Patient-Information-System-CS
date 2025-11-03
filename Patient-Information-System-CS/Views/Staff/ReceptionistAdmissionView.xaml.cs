using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;
using Patient_Information_System_CS.Views.Admin.Dialogs;

namespace Patient_Information_System_CS.Views.Staff
{
    public partial class ReceptionistAdmissionView : UserControl
    {
        private readonly HospitalDataService _dataService = HospitalDataService.Instance;
        private IReadOnlyList<ExistingPatientOption> _existingPatients = Array.Empty<ExistingPatientOption>();
        private ExistingPatientOption? _selectedExistingPatient;
        private bool _isSubscribed;

        public ReceptionistAdmissionView(UserAccount? _)
        {
            InitializeComponent();
            Loaded += AdmissionView_Loaded;
            Unloaded += AdmissionView_Unloaded;
        }

        private void AdmissionView_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isSubscribed)
            {
                _dataService.AdmissionsChanged += OnAdmissionsChanged;
                _isSubscribed = true;
            }

            PopulateDoctors();
            PopulateNurses();
            PopulateRooms();
            LoadExistingPatients();
            InitializeAdmissionFormDefaults();
            ClearAdmissionForm();
            RefreshTables();
        }

        private void AdmissionView_Unloaded(object sender, RoutedEventArgs e)
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
                    PopulateNurses();
                    PopulateRooms();
                    LoadExistingPatients();
                    RefreshTables();
                });
                return;
            }

            PopulateDoctors();
            PopulateNurses();
            PopulateRooms();
            LoadExistingPatients();
            RefreshTables();
        }

        private void PopulateNurses()
        {
            var nurses = _dataService.GetAllNurses()
                .OrderBy(n => n.NurseProfile is { Status: NurseStatus.Available } ? 0 : 1)
                .ThenBy(n => n.DisplayName)
                .ToList();

            NurseComboBox.ItemsSource = nurses;
            NurseComboBox.IsEnabled = nurses.Count > 0;

            if (nurses.Count == 0)
            {
                NurseComboBox.SelectedIndex = -1;
                return;
            }

            if (NurseComboBox.SelectedValue is int nurseUserId && nurses.Any(n => n.UserId == nurseUserId))
            {
                NurseComboBox.SelectedValue = nurseUserId;
            }
            else
            {
                NurseComboBox.SelectedIndex = 0;
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
            if (ExistingPatientRadioButton.IsChecked == true)
            {
                HandleExistingPatientAdmission();
                return;
            }

            var fullName = PatientNameTextBox.Text.Trim();
            var email = EmailTextBox.Text.Trim();
            var dateOfBirth = BirthDatePicker.SelectedDate ?? DateTime.Today.AddYears(-30);
            var contact = ContactTextBox.Text.Trim();
            var address = AddressTextBox.Text.Trim();
            var emergencyContact = EmergencyContactTextBox.Text.Trim();
            var emergencyRelationship = EmergencyRelationshipTextBox.Text.Trim();
            var insurance = InsuranceTextBox.Text.Trim();
            var roomAssignment = GetRoomSelection();
            var doctor = DoctorComboBox.SelectedItem as UserAccount;
            var nurse = NurseComboBox.SelectedItem as UserAccount;
            var sex = SexComboBox.SelectedValue as string ?? "U";
            var nationality = string.IsNullOrWhiteSpace(NationalityTextBox.Text) ? "Unknown" : NationalityTextBox.Text.Trim();

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
                nurse,
                roomAssignment,
                email,
                admitDateOverride: DateTime.Now,
                sex: sex,
                emergencyRelationship: string.IsNullOrWhiteSpace(emergencyRelationship) ? "Unknown" : emergencyRelationship,
                nationality: nationality);

            MessageBox.Show(
                $"Admission complete for {newPatient.DisplayName}.\n\nUsername: {newPatient.Username}\nTemporary Password: {newPatient.GetPlainTextPassword()}",
                "Admission Recorded",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            ClearAdmissionForm();
            LoadExistingPatients();
            RefreshTables();
        }

        private void DischargePatient_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: UserAccount patient })
            {
                return;
            }

            var dialog = new DischargeBillingWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var billingRequest = dialog.BuildRequest(patient.UserId);
            var invoice = _dataService.DischargePatient(patient, billingRequest);
            RefreshTables();

            MessageBox.Show(
                $"{patient.DisplayName} has been discharged. Total charges recorded: {invoice.TotalAmount:C}.",
                "Discharge Complete",
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
            EmailTextBox.Clear();
            BirthDatePicker.SelectedDate = DateTime.Today.AddYears(-30);
            ContactTextBox.Clear();
            AddressTextBox.Clear();
            EmergencyContactTextBox.Clear();
            EmergencyRelationshipTextBox.Clear();
            InsuranceTextBox.Text = "No Insurance";
            NationalityTextBox.Text = "PH";
            SexComboBox.SelectedValue = "U";
            if (RoomComboBox.Items.Count > 0)
            {
                RoomComboBox.SelectedIndex = 0;
            }

            PopulateDoctors();
            PopulateNurses();
        }

        private void LoadExistingPatients()
        {
            _existingPatients = _dataService.GetExistingPatientOptions();
            ExistingPatientComboBox.ItemsSource = _existingPatients;
            ExistingPatientComboBox.SelectedIndex = -1;
            ExistingPatientInfoTextBlock.Text = _existingPatients.Count == 0
                ? "No patient records are available yet."
                : string.Empty;
        }

        private void InitializeAdmissionFormDefaults()
        {
            SexComboBox.SelectedValue = "U";
            NationalityTextBox.Text = "PH";
            NewPatientRadioButton.IsChecked = true;
        }

        private void AdmissionModeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            UpdateAdmissionMode();
        }

        private void UpdateAdmissionMode()
        {
            if (ExistingPatientSelectorPanel is null || ExistingPatientRadioButton is null)
            {
                return;
            }

            var useExisting = ExistingPatientRadioButton.IsChecked == true;
            ExistingPatientSelectorPanel.Visibility = useExisting ? Visibility.Visible : Visibility.Collapsed;

            if (!useExisting)
            {
                _selectedExistingPatient = null;
                ExistingPatientComboBox.SelectedIndex = -1;
                ExistingPatientInfoTextBlock.Text = string.Empty;
                InitializeAdmissionFormDefaults();
                ClearAdmissionForm();
            }
        }

        private void ExistingPatientComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ExistingPatientComboBox.SelectedItem is ExistingPatientOption option)
            {
                _selectedExistingPatient = option;
                PopulateExistingPatientDetails(option);
            }
        }

        private void PopulateExistingPatientDetails(ExistingPatientOption option)
        {
            var account = _dataService.GetAccountById(option.UserId);
            if (account?.PatientProfile is null)
            {
                ExistingPatientInfoTextBlock.Text = "Unable to load patient details.";
                return;
            }

            PatientNameTextBox.Text = account.DisplayName;
            EmailTextBox.Text = account.Email;
            BirthDatePicker.SelectedDate = account.PatientProfile.DateOfBirth;
            ContactTextBox.Text = account.PatientProfile.ContactNumber;
            AddressTextBox.Text = account.PatientProfile.Address;
            EmergencyContactTextBox.Text = account.PatientProfile.EmergencyContact;
            EmergencyRelationshipTextBox.Text = account.PatientProfile.EmergencyRelationship;
            InsuranceTextBox.Text = account.PatientProfile.InsuranceProvider;
            NationalityTextBox.Text = account.PatientProfile.Nationality;
            SexComboBox.SelectedValue = string.IsNullOrWhiteSpace(account.PatientProfile.Sex) ? "U" : account.PatientProfile.Sex;

            if (account.PatientProfile.AssignedNurseId.HasValue && NurseComboBox.ItemsSource is IEnumerable<UserAccount> nurseSource)
            {
                var assignedNurseId = account.PatientProfile.AssignedNurseId.Value;
                var nurses = nurseSource.ToList();
                var assignedNurse = nurses.FirstOrDefault(n => n.NurseProfile?.NurseId == assignedNurseId)
                    ?? _dataService.GetAllNurses().FirstOrDefault(n => n.NurseProfile?.NurseId == assignedNurseId);

                if (assignedNurse is null)
                {
                    NurseComboBox.SelectedIndex = -1;
                }
                else
                {
                    if (nurses.All(n => n.UserId != assignedNurse.UserId))
                    {
                        nurses.Add(assignedNurse);
                        NurseComboBox.ItemsSource = nurses
                            .OrderBy(n => n.NurseProfile is { Status: NurseStatus.Available } ? 0 : 1)
                            .ThenBy(n => n.DisplayName)
                            .ToList();
                    }

                    NurseComboBox.SelectedValue = assignedNurse.UserId;
                }
            }
            else
            {
                NurseComboBox.SelectedIndex = -1;
            }

            ExistingPatientInfoTextBlock.Text = option.IsCurrentlyAdmitted
                ? "This patient is already admitted."
                : $"Patient number {option.PatientNumber} • {option.ContactNumber}";
        }

        private void HandleExistingPatientAdmission()
        {
            if (_selectedExistingPatient is null)
            {
                MessageBox.Show("Select an existing patient record before continuing.", "No Patient Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var doctor = DoctorComboBox.SelectedItem as UserAccount;
            var nurse = NurseComboBox.SelectedItem as UserAccount;
            var request = new ExistingPatientAdmissionRequest
            {
                UserId = _selectedExistingPatient.UserId,
                AssignedDoctorUserId = doctor?.UserId,
                AssignedNurseUserId = nurse?.UserId,
                RoomAssignment = GetRoomSelection(),
                AdmitDateOverride = DateTime.Now,
                ContactNumber = ContactTextBox.Text.Trim(),
                Address = AddressTextBox.Text.Trim(),
                EmergencyContact = EmergencyContactTextBox.Text.Trim(),
                EmergencyRelationship = EmergencyRelationshipTextBox.Text.Trim(),
                InsuranceProvider = InsuranceTextBox.Text.Trim()
            };

            var updatedAccount = _dataService.ReadmitExistingPatient(request);
            RefreshTables();

            MessageBox.Show(
                $"{updatedAccount.DisplayName} has been readmitted successfully.",
                "Admission Recorded",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
