using System.ComponentModel;
using System.Runtime.CompilerServices;
using Patient_Information_System_CS.Models;

namespace Patient_Information_System_CS.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private UserAccount? _currentUser;
        private object? _currentView;
        private string _selectedNavigationItem = "Dashboard";
        private UserRole? _lastRecordedRole;

        public MainViewModel(UserAccount? account)
        {
            CurrentUser = account;
            NavigateToView(_selectedNavigationItem);
        }

        public UserAccount? CurrentUser
        {
            get => _currentUser;
            private set
            {
                _currentUser = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentUserRole));
                OnPropertyChanged(nameof(CurrentUserDisplayName));
                OnPropertyChanged(nameof(IsAdminRole));
                OnPropertyChanged(nameof(IsDoctorRole));
                OnPropertyChanged(nameof(IsNurseRole));
                OnPropertyChanged(nameof(IsPatientRole));
                OnPropertyChanged(nameof(IsStaffRole));
                UpdateNavigationItems();
            }
        }

        public UserRole CurrentUserRole => CurrentUser?.Role ?? UserRole.Admin;

        public string CurrentUserDisplayName => CurrentUser?.DisplayName ?? "Administrator";

        public bool IsAdminRole => CurrentUserRole == UserRole.Admin;
        public bool IsDoctorRole => CurrentUserRole == UserRole.Doctor;
        public bool IsNurseRole => CurrentUserRole == UserRole.Nurse;
        public bool IsPatientRole => CurrentUserRole == UserRole.Patient;
        public bool IsStaffRole => CurrentUserRole == UserRole.Staff;

        public object? CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public string SelectedNavigationItem
        {
            get => _selectedNavigationItem;
            set
            {
                _selectedNavigationItem = value;
                OnPropertyChanged();
                NavigateToView(value);
            }
        }

        private void UpdateNavigationItems()
        {
            var newRole = CurrentUserRole;
            if (_lastRecordedRole != newRole)
            {
                _lastRecordedRole = newRole;
                SelectedNavigationItem = "Dashboard";
            }
        }

        private void NavigateToView(string viewName)
        {
            CurrentView = viewName switch
            {
                "Account" when CurrentUser is not null => new Views.Common.AccountSettingsView(this),
                // Admin Views
                "Dashboard" when IsAdminRole => new Views.Admin.AdminDashboardView(),
                "Admission" when IsAdminRole => new Views.Admin.AdmissionView(),
                "Staffs" when IsAdminRole => new Views.Admin.StaffsView(),
                "Doctors" when IsAdminRole => new Views.Admin.DoctorsView(),
                "Nurses" when IsAdminRole => new Views.Admin.NursesView(),
                "Patients" when IsAdminRole => new Views.Admin.PatientsView(),
                "Rooms" when IsAdminRole => new Views.Admin.RoomsView(),
                "Appointments" when IsAdminRole => new Views.Admin.AppointmentsView(),
                "MedicalRecords" when IsAdminRole => new Views.Admin.AdminMedicalRecordsView(CurrentUser),
                "Billing/Invoice" when IsAdminRole => new Views.Admin.BillingView(),

                // Doctor Views
                "Dashboard" when IsDoctorRole => new Views.Doctor.DoctorDashboardView(CurrentUser),
                "Patient" when IsDoctorRole => new Views.Doctor.PatientView(CurrentUser),
                "Appointments" when IsDoctorRole => new Views.Doctor.DoctorAppointmentsView(CurrentUser),
                "MedicalRecords" when IsDoctorRole => new Views.Doctor.DoctorMedicalRecordsView(CurrentUser),

                // Patient Views
                "Dashboard" when IsPatientRole => new Views.Patient.PatientDashboardView(CurrentUser),
                "Appointments" when IsPatientRole => new Views.Patient.PatientAppointmentsView(CurrentUser),
                "MedicalRecords" when IsPatientRole => new Views.Patient.PatientMedicalRecordsView(CurrentUser),
                "Discharge" when IsPatientRole => new Views.Patient.DischargeView(CurrentUser),
                "Insurance" when IsPatientRole => new Views.Patient.InsuranceView(),

                // Staff/Receptionist Views
                "Dashboard" when IsStaffRole => new Views.Staff.ReceptionistDashboardView(CurrentUser),
                "Admission" when IsStaffRole => new Views.Staff.ReceptionistAdmissionView(CurrentUser),
                "Staffs" when IsStaffRole => new Views.Staff.ReceptionistStaffsView(),
                "Doctors" when IsStaffRole => new Views.Staff.ReceptionistDoctorsView(),
                "Nurses" when IsStaffRole => new Views.Staff.ReceptionistNursesView(),
                "Patients" when IsStaffRole => new Views.Staff.ReceptionistPatientsView(CurrentUser),
                "Rooms" when IsStaffRole => new Views.Admin.RoomsView(),
                "Appointments" when IsStaffRole => new Views.Staff.ReceptionistAppointmentsView(CurrentUser),
                "MedicalRecords" when IsStaffRole => new Views.Staff.ReceptionistMedicalRecordsView(CurrentUser),
                "Billing/Invoice" when IsStaffRole => new Views.Staff.ReceptionistBillingInvoiceView(CurrentUser),

                // Nurse Views
                "Dashboard" when IsNurseRole => new Views.Staff.ReceptionistDashboardView(CurrentUser),
                "Patients" when IsNurseRole => new Views.Staff.ReceptionistPatientsView(CurrentUser),

                _ => new Views.Admin.AdminDashboardView() // Default
            };
        }

        public void UpdateCurrentUser(UserAccount account)
        {
            CurrentUser = account;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
