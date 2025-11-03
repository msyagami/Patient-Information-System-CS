using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;

namespace Patient_Information_System_CS.Views.Staff
{
    public partial class ReceptionistPatientsView : UserControl
    {
        private readonly HospitalDataService _dataService = HospitalDataService.Instance;
        private readonly UserAccount? _staffAccount;
        private bool _isSubscribed;

        public ReceptionistPatientsView(UserAccount? staffAccount)
        {
            _staffAccount = staffAccount;
            InitializeComponent();
            Loaded += ReceptionistPatientsView_Loaded;
            Unloaded += ReceptionistPatientsView_Unloaded;
        }

        private void ReceptionistPatientsView_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isSubscribed)
            {
                _dataService.AdmissionsChanged += OnAdmissionsChanged;
                _isSubscribed = true;
            }

            PopulateTables();
        }

        private void ReceptionistPatientsView_Unloaded(object sender, RoutedEventArgs e)
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
            var admitted = _dataService.GetCurrentAdmissions().ToList();
            var admittedRows = admitted.Select(CreateAdmittedRow)
                                       .OrderBy(row => row.Name)
                                       .ToList();

            AdmittedPatientsDataGrid.ItemsSource = admittedRows;
            AdmittedEmptyTextBlock.Visibility = admittedRows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            AdmittedCountTextBlock.Text = FormatCount(admittedRows.Count, "active admission", "active admissions");

            var discharged = _dataService.GetApprovedPatients()
                .Where(account => account.PatientProfile?.IsCurrentlyAdmitted != true)
                .ToList();

            var dischargedRows = discharged.Select(CreateDischargedRow)
                                           .OrderByDescending(row => row.DischargeDateSortKey ?? DateTime.MinValue)
                                           .ToList();

            DischargedPatientsDataGrid.ItemsSource = dischargedRows;
            DischargedEmptyTextBlock.Visibility = dischargedRows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            DischargedCountTextBlock.Text = FormatCount(dischargedRows.Count, "recent discharge", "recent discharges");
        }

        private AdmittedPatientRow CreateAdmittedRow(UserAccount account)
        {
            var profile = account.PatientProfile!;
            var doctorName = profile.AssignedDoctorId is int doctorId
                ? _dataService.GetDoctorById(doctorId)?.DisplayName ?? "Unassigned"
                : string.IsNullOrWhiteSpace(profile.AssignedDoctorName) ? "Unassigned" : profile.AssignedDoctorName;

            return new AdmittedPatientRow
            {
                Name = account.DisplayName,
                PatientNumber = profile.PatientNumber,
                AdmitDate = profile.AdmitDate?.ToString("MMM dd, yyyy", CultureInfo.CurrentCulture) ?? "-",
                Room = string.IsNullOrWhiteSpace(profile.RoomAssignment) ? "Not assigned" : profile.RoomAssignment,
                Doctor = doctorName,
                Contact = string.IsNullOrWhiteSpace(profile.ContactNumber) ? "-" : profile.ContactNumber
            };
        }

        private DischargedPatientRow CreateDischargedRow(UserAccount account)
        {
            var profile = account.PatientProfile!;
            var invoices = _dataService.GetInvoicesForPatient(account.UserId).ToList();
            var latestInvoice = invoices.FirstOrDefault();
            var outstanding = invoices.Where(invoice => !invoice.IsPaid)
                                      .Sum(invoice => invoice.Total);

            return new DischargedPatientRow
            {
                Name = account.DisplayName,
                PatientNumber = profile.PatientNumber,
                AdmitDate = profile.AdmitDate?.ToString("MMM dd, yyyy", CultureInfo.CurrentCulture) ?? "-",
                DischargeDate = latestInvoice?.ReleaseDate.ToString("MMM dd, yyyy", CultureInfo.CurrentCulture) ?? "-",
                OutstandingBalance = outstanding == 0 ? "Cleared" : outstanding.ToString("C", CultureInfo.CurrentCulture),
                Contact = string.IsNullOrWhiteSpace(profile.ContactNumber) ? "-" : profile.ContactNumber,
                DischargeDateSortKey = latestInvoice?.ReleaseDate
            };
        }

        private static string FormatCount(int count, string singular, string plural)
        {
            return count switch
            {
                0 => "No " + plural,
                1 => "1 " + singular,
                _ => count + " " + plural
            };
        }

        private sealed class AdmittedPatientRow
        {
            public string Name { get; set; } = string.Empty;
            public string PatientNumber { get; set; } = string.Empty;
            public string AdmitDate { get; set; } = string.Empty;
            public string Room { get; set; } = string.Empty;
            public string Doctor { get; set; } = string.Empty;
            public string Contact { get; set; } = string.Empty;
        }

        private sealed class DischargedPatientRow
        {
            public string Name { get; set; } = string.Empty;
            public string PatientNumber { get; set; } = string.Empty;
            public string AdmitDate { get; set; } = string.Empty;
            public string DischargeDate { get; set; } = string.Empty;
            public string OutstandingBalance { get; set; } = string.Empty;
            public string Contact { get; set; } = string.Empty;
            public DateTime? DischargeDateSortKey { get; set; }
        }
    }
}
