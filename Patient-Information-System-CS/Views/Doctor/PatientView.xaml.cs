using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;

namespace Patient_Information_System_CS.Views.Doctor
{
    public partial class PatientView : UserControl
    {
        private readonly HospitalDataService _dataService = HospitalDataService.Instance;
        private readonly UserAccount? _doctorAccount;
        private bool _isSubscribed;

        public PatientView(UserAccount? doctorAccount)
        {
            _doctorAccount = doctorAccount;
            InitializeComponent();
            Loaded += PatientView_Loaded;
            Unloaded += PatientView_Unloaded;
        }

        private void PatientView_Loaded(object? sender, RoutedEventArgs e)
        {
            if (!_isSubscribed)
            {
                _dataService.AdmissionsChanged += OnAdmissionsChanged;
                _isSubscribed = true;
            }

            PopulateTables();
        }

        private void PatientView_Unloaded(object sender, RoutedEventArgs e)
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
                Dispatcher.Invoke(PopulateTables);
                return;
            }

            PopulateTables();
        }

        private void PopulateTables()
        {
            if (_doctorAccount?.DoctorProfile is null)
            {
                ActivePatientsDataGrid.ItemsSource = Array.Empty<ActivePatientRow>();
                ActivePatientsEmptyTextBlock.Visibility = Visibility.Visible;
                DischargedPatientsDataGrid.ItemsSource = Array.Empty<DischargedPatientRow>();
                DischargedPatientsEmptyTextBlock.Visibility = Visibility.Visible;
                return;
            }

            var patientAccounts = _dataService.GetPatientsForDoctor(_doctorAccount.UserId).ToList();

            var activeRows = patientAccounts
                .Where(account => account.PatientProfile?.IsCurrentlyAdmitted == true)
                .Select(account => CreateActivePatientRow(account))
                .OrderBy(row => row.Name)
                .ToList();

            var dischargedRows = patientAccounts
                .Where(account => account.PatientProfile?.IsCurrentlyAdmitted != true)
                .Select(account => CreateDischargedPatientRow(account))
                .OrderByDescending(row => row.DischargeDateSortKey)
                .ToList();

            ActivePatientsDataGrid.ItemsSource = activeRows;
            ActivePatientsEmptyTextBlock.Visibility = activeRows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            DischargedPatientsDataGrid.ItemsSource = dischargedRows;
            DischargedPatientsEmptyTextBlock.Visibility = dischargedRows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private ActivePatientRow CreateActivePatientRow(UserAccount account)
        {
            var profile = account.PatientProfile!;
            var admitDate = profile.AdmitDate?.ToString("MMM dd, yyyy", CultureInfo.CurrentCulture) ?? "-";
            var room = string.IsNullOrWhiteSpace(profile.RoomAssignment) ? "Not assigned" : profile.RoomAssignment;
            var contact = string.IsNullOrWhiteSpace(profile.ContactNumber) ? "-" : profile.ContactNumber;
            var insurance = string.IsNullOrWhiteSpace(profile.InsuranceProvider) ? "Not provided" : profile.InsuranceProvider;

            return new ActivePatientRow
            {
                Name = account.DisplayName,
                AdmitDate = admitDate,
                Room = room,
                Contact = contact,
                Insurance = insurance
            };
        }

        private DischargedPatientRow CreateDischargedPatientRow(UserAccount account)
        {
            var profile = account.PatientProfile!;
            var invoices = _dataService.GetInvoicesForPatient(account.UserId).ToList();
            var latestInvoice = invoices.FirstOrDefault();

            var admitDate = profile.AdmitDate?.ToString("MMM dd, yyyy", CultureInfo.CurrentCulture) ?? "-";
            var dischargeDate = latestInvoice?.ReleaseDate.ToString("MMM dd, yyyy", CultureInfo.CurrentCulture) ?? "-";
            var outstanding = invoices.Where(invoice => !invoice.IsPaid)
                                       .Sum(invoice => invoice.Total);

            var outstandingDisplay = outstanding == 0
                ? "Cleared"
                : outstanding.ToString("C", CultureInfo.CurrentCulture);

            var contact = string.IsNullOrWhiteSpace(profile.ContactNumber) ? "-" : profile.ContactNumber;

            return new DischargedPatientRow
            {
                Name = account.DisplayName,
                AdmitDate = admitDate,
                DischargeDate = dischargeDate,
                OutstandingBalance = outstandingDisplay,
                Contact = contact,
                DischargeDateSortKey = latestInvoice?.ReleaseDate
            };
        }

        private sealed class ActivePatientRow
        {
            public string Name { get; set; } = string.Empty;
            public string AdmitDate { get; set; } = string.Empty;
            public string Room { get; set; } = string.Empty;
            public string Contact { get; set; } = string.Empty;
            public string Insurance { get; set; } = string.Empty;
        }

        private sealed class DischargedPatientRow
        {
            public string Name { get; set; } = string.Empty;
            public string AdmitDate { get; set; } = string.Empty;
            public string DischargeDate { get; set; } = string.Empty;
            public string OutstandingBalance { get; set; } = string.Empty;
            public string Contact { get; set; } = string.Empty;
            public DateTime? DischargeDateSortKey { get; set; }
        }
    }
}
