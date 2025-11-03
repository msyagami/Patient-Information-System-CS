using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;

namespace Patient_Information_System_CS.Views.Staff
{
    public partial class ReceptionistDashboardView : UserControl
    {
        private readonly HospitalDataService _dataService = HospitalDataService.Instance;
        private readonly UserAccount? _staffAccount;

        public ReceptionistDashboardView(UserAccount? staffAccount)
        {
            _staffAccount = staffAccount;
            InitializeComponent();
            Loaded += ReceptionistDashboardView_Loaded;
        }

        private void ReceptionistDashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateDashboard();
        }

        private void PopulateDashboard()
        {
            var activeDoctors = _dataService.GetActiveDoctors().ToList();
            var pendingDoctors = _dataService.GetPendingDoctors().ToList();
            var unavailableDoctors = _dataService.GetUnavailableDoctors().ToList();

            ActiveDoctorsValueTextBlock.Text = activeDoctors.Count.ToString(CultureInfo.InvariantCulture);
            ActiveDoctorsSubtitleTextBlock.Text = unavailableDoctors.Count == 0
                ? "All other doctors available"
                : unavailableDoctors.Count == 1
                    ? "1 doctor currently unavailable"
                    : $"{unavailableDoctors.Count} doctors unavailable";

            PendingDoctorsValueTextBlock.Text = pendingDoctors.Count.ToString(CultureInfo.InvariantCulture);
            PendingDoctorsSubtitleTextBlock.Text = pendingDoctors.Count == 0
                ? "No approvals pending"
                : pendingDoctors.Count == 1
                    ? "1 request awaiting review"
                    : $"{pendingDoctors.Count} requests awaiting review";

            var approvedPatients = _dataService.GetApprovedPatients().ToList();
            var pendingPatients = _dataService.GetPendingPatients().ToList();

            TotalPatientsValueTextBlock.Text = (approvedPatients.Count + pendingPatients.Count).ToString(CultureInfo.InvariantCulture);
            PendingPatientsSubtitleTextBlock.Text = pendingPatients.Count == 0
                ? "All patients cleared"
                : pendingPatients.Count == 1
                    ? "1 registration pending"
                    : $"{pendingPatients.Count} registrations pending";

            var allAppointments = _dataService.GetAllAppointments().ToList();
            var pendingAppointments = _dataService.GetPendingAppointments().ToList();
            var todayAppointments = allAppointments.Count(appointment => appointment.ScheduledFor.Date == DateTime.Today);

            TotalAppointmentsValueTextBlock.Text = allAppointments.Count.ToString(CultureInfo.InvariantCulture);
            PendingAppointmentsSubtitleTextBlock.Text = pendingAppointments.Count == 0
                ? "No pending approvals"
                : pendingAppointments.Count == 1
                    ? "1 appointment pending"
                    : $"{pendingAppointments.Count} appointments pending";

            var outstandingInvoices = _dataService.GetOutstandingInvoices().ToList();
            var outstandingTotal = outstandingInvoices.Sum(invoice => invoice.Total);
            OutstandingInvoicesValueTextBlock.Text = outstandingInvoices.Count.ToString(CultureInfo.InvariantCulture);
            OutstandingInvoicesSubtitleTextBlock.Text = outstandingInvoices.Count == 0
                ? "All invoices settled"
                : $"Total balance {outstandingTotal.ToString("C", CultureInfo.CurrentCulture)}";

            var currentAdmissions = _dataService.GetCurrentAdmissions().ToList();
            var admissionsToday = currentAdmissions.Count(account => account.PatientProfile?.AdmitDate?.Date == DateTime.Today);

            TodayAdmissionsValueTextBlock.Text = admissionsToday.ToString(CultureInfo.InvariantCulture);
            CurrentAdmissionsSubtitleTextBlock.Text = currentAdmissions.Count == 0
                ? "No active admissions"
                : currentAdmissions.Count == 1
                    ? "1 patient currently admitted"
                    : $"{currentAdmissions.Count} patients currently admitted";

            var doctorRows = activeDoctors.Select(account => new DoctorRow
            {
                Name = account.DisplayName,
                Department = account.DoctorProfile?.Department ?? "Not specified",
                Contact = string.IsNullOrWhiteSpace(account.DoctorProfile?.ContactNumber)
                    ? "-"
                    : account.DoctorProfile.ContactNumber,
                Status = account.DoctorProfile?.Status switch
                {
                    DoctorStatus.Available => "Available",
                    DoctorStatus.NotAvailable => "Unavailable",
                    DoctorStatus.OnHold => "On hold",
                    _ => "Unknown"
                }
            }).ToList();

            ActiveDoctorsDataGrid.ItemsSource = doctorRows;
            ActiveDoctorsEmptyTextBlock.Visibility = doctorRows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ActiveDoctorsSummaryTextBlock.Text = doctorRows.Count == 0
                ? "No available doctors to display."
                : doctorRows.Count == 1
                    ? "Showing 1 active doctor."
                    : $"Showing {doctorRows.Count} active doctors.";
        }

        private sealed class DoctorRow
        {
            public string Name { get; set; } = string.Empty;
            public string Department { get; set; } = string.Empty;
            public string Contact { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }
    }
}
