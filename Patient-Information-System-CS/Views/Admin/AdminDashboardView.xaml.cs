using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;

namespace Patient_Information_System_CS.Views.Admin
{
    public partial class AdminDashboardView : UserControl
    {
        private readonly HospitalDataService _dataService = HospitalDataService.Instance;
        private IList<RecentPatientRow> _recentPatients = new List<RecentPatientRow>();
        private ICollectionView? _recentPatientsView;
        private string _recentPatientsSearchTerm = string.Empty;

        public AdminDashboardView()
        {
            InitializeComponent();
            Loaded += AdminDashboardView_Loaded;
        }

        private void AdminDashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshDashboard();
        }

        private void RefreshDashboard()
        {
            PopulateDoctorCards();
            PopulatePatientCards();
            PopulateAppointmentCards();
            PopulatePatientBreakdown();
            PopulateAdmissionTrend();
            PopulateAppointmentBreakdown();
            PopulateRecentPatients();
        }

        private void PopulateDoctorCards()
        {
            var doctors = _dataService.GetAllDoctors().ToList();
            var activeDoctors = doctors.Count(account => account.DoctorProfile?.Status == DoctorStatus.Available);
            var pendingDoctors = doctors.Count(account => account.DoctorProfile?.Status == DoctorStatus.OnHold);
            var unavailableDoctors = doctors.Count(account => account.DoctorProfile?.Status == DoctorStatus.NotAvailable);

            ActiveDoctorsCountTextBlock.Text = activeDoctors.ToString(CultureInfo.InvariantCulture);
            PendingDoctorsTextBlock.Text = FormatCount(pendingDoctors, "doctor awaiting approval", "doctors awaiting approval");
            UnavailableDoctorsTextBlock.Text = FormatCount(unavailableDoctors, "doctor unavailable", "doctors unavailable");
        }

        private void PopulatePatientCards()
        {
                var patients = _dataService.GetAllPatients().ToList();
            var approvedPatients = patients.Count(account => account.PatientProfile?.IsApproved == true);
            var pendingPatients = patients.Count - approvedPatients;
            var admittedPatients = patients.Count(account => account.PatientProfile?.IsCurrentlyAdmitted == true);

            TotalPatientsCountTextBlock.Text = patients.Count.ToString(CultureInfo.InvariantCulture);
            AdmittedPatientsTextBlock.Text = FormatCount(admittedPatients, "patient currently admitted", "patients currently admitted");
            PendingPatientsTextBlock.Text = FormatCount(pendingPatients, "patient awaiting approval", "patients awaiting approval");
        }

        private void PopulateAppointmentCards()
        {
            var appointments = _dataService.GetAllAppointments().ToList();
            var pending = appointments.Count(appointment => appointment.Status == AppointmentStatus.Pending);
            var today = appointments.Count(appointment => appointment.ScheduledFor.Date == DateTime.Today);

            TotalAppointmentsCountTextBlock.Text = appointments.Count.ToString(CultureInfo.InvariantCulture);
            PendingAppointmentsTextBlock.Text = FormatCount(pending, "appointment pending", "appointments pending");
            TodayAppointmentsTextBlock.Text = FormatCount(today, "appointment today", "appointments today");
        }

        private void PopulatePatientBreakdown()
        {
            var patients = _dataService.GetAllPatients().ToList();
            var approved = patients.Count(account => account.PatientProfile?.IsApproved == true);
            var pending = patients.Count - approved;
            var admitted = patients.Count(account => account.PatientProfile?.IsCurrentlyAdmitted == true);
            var discharged = patients.Count(account => account.PatientProfile?.IsApproved == true && account.PatientProfile?.IsCurrentlyAdmitted != true);

            var metrics = new List<MetricItem>
            {
                new("Approved patients", approved.ToString(CultureInfo.InvariantCulture)),
                new("Pending approval", pending.ToString(CultureInfo.InvariantCulture)),
                new("Currently admitted", admitted.ToString(CultureInfo.InvariantCulture)),
                new("Discharged patients", discharged.ToString(CultureInfo.InvariantCulture))
            };

            PatientBreakdownItemsControl.ItemsSource = metrics;
            PatientTotalsSummaryTextBlock.Text = string.Join(" • ", new[]
            {
                $"{approved} approved",
                $"{pending} pending",
                $"{admitted} admitted",
                $"{discharged} discharged"
            });
        }

        private void PopulateAdmissionTrend()
        {
            var patients = _dataService.GetAllPatients().ToList();
            var dayRange = Enumerable.Range(0, 7)
                                     .Select(offset => DateTime.Today.AddDays(-offset))
                                     .OrderBy(date => date)
                                     .ToList();

            var trend = dayRange.Select(date => new TrendItem
            {
                Label = date.ToString("MMM dd", CultureInfo.CurrentCulture),
                Count = patients.Count(account => account.PatientProfile?.AdmitDate?.Date == date)
            }).ToList();

            var maxCount = Math.Max(1, trend.Max(item => item.Count));
            foreach (var item in trend)
            {
                item.Percent = Math.Round(item.Count / (double)maxCount * 100, 2);
            }

            AdmissionTrendItemsControl.ItemsSource = trend;
            var totalAdmissions = trend.Sum(item => item.Count);
            AdmissionsSummaryTextBlock.Text = totalAdmissions == 0
                ? "No admissions recorded in the last 7 days"
                : FormatCount(totalAdmissions, "admission recorded in the last 7 days", "admissions recorded in the last 7 days");
        }

        private void PopulateAppointmentBreakdown()
        {
            var appointments = _dataService.GetAllAppointments().ToList();
            var pending = appointments.Count(appointment => appointment.Status == AppointmentStatus.Pending);
            var accepted = appointments.Count(appointment => appointment.Status == AppointmentStatus.Accepted);
            var completed = appointments.Count(appointment => appointment.Status == AppointmentStatus.Completed);
            var cancelled = appointments.Count(appointment => appointment.Status == AppointmentStatus.Rejected);

            var metrics = new List<MetricItem>
            {
                new("Pending", pending.ToString(CultureInfo.InvariantCulture)),
                new("Accepted", accepted.ToString(CultureInfo.InvariantCulture)),
                new("Completed", completed.ToString(CultureInfo.InvariantCulture)),
                new("Cancelled", cancelled.ToString(CultureInfo.InvariantCulture))
            };

            AppointmentBreakdownItemsControl.ItemsSource = metrics;
            AppointmentSummaryTextBlock.Text = FormatCount(appointments.Count, "appointment currently tracked", "appointments currently tracked");
        }

        private void PopulateRecentPatients()
        {
            var patients = _dataService.GetAllPatients().ToList();
            _recentPatients = patients.Where(account => account.PatientProfile?.AdmitDate is not null)
                                       .OrderByDescending(account => account.PatientProfile!.AdmitDate)
                                       .Take(50)
                                       .Select(account => new RecentPatientRow
                                       {
                                           AdmissionDateDisplay = account.PatientProfile?.AdmitDate?.ToString("MMM dd, yyyy", CultureInfo.CurrentCulture) ?? "-",
                                           Name = account.DisplayName,
                                           Contact = string.IsNullOrWhiteSpace(account.PatientProfile?.ContactNumber) ? "-" : account.PatientProfile!.ContactNumber,
                                           Status = account.PatientProfile?.IsCurrentlyAdmitted == true
                                               ? "Admitted"
                                               : account.PatientProfile?.HasUnpaidBills == true ? "Discharged (Pending Bill)" : "Discharged"
                                       })
                                       .ToList();

            _recentPatientsView = CollectionViewSource.GetDefaultView(_recentPatients);
            if (_recentPatientsView is not null)
            {
                _recentPatientsView.Filter = RecentPatientsFilter;
            }

            RecentPatientsGrid.ItemsSource = _recentPatientsView;
            ApplyRecentPatientsFilter();
        }

        private void RecentPatientsSearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            _recentPatientsSearchTerm = RecentPatientsSearchTextBox.Text.Trim();
            ApplyRecentPatientsFilter();
        }

        private void ApplyRecentPatientsFilter()
        {
            _recentPatientsView?.Refresh();
            UpdateRecentPatientsSummary();
        }

        private bool RecentPatientsFilter(object item)
        {
            if (string.IsNullOrWhiteSpace(_recentPatientsSearchTerm))
            {
                return true;
            }

            if (item is not RecentPatientRow row)
            {
                return true;
            }

            return row.Name.Contains(_recentPatientsSearchTerm, StringComparison.OrdinalIgnoreCase) ||
                   row.Contact.Contains(_recentPatientsSearchTerm, StringComparison.OrdinalIgnoreCase) ||
                   row.Status.Contains(_recentPatientsSearchTerm, StringComparison.OrdinalIgnoreCase) ||
                   row.AdmissionDateDisplay.Contains(_recentPatientsSearchTerm, StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateRecentPatientsSummary()
        {
            var total = _recentPatients.Count;
            if (total == 0)
            {
                RecentPatientsSummaryTextBlock.Text = "No admissions recorded yet";
                return;
            }

            var filtered = _recentPatientsView?.Cast<RecentPatientRow>().Count() ?? total;
            if (filtered == total)
            {
                RecentPatientsSummaryTextBlock.Text = total <= 10
                    ? FormatCount(total, "admission listed", "admissions listed")
                    : $"Showing {filtered} of {total} admissions";
                return;
            }

            RecentPatientsSummaryTextBlock.Text = $"Showing {filtered} of {total} admissions";
        }

        private static string FormatCount(int count, string singular, string plural)
        {
            if (count == 0)
            {
                return $"No {plural}";
            }

            return count == 1 ? $"1 {singular}" : $"{count} {plural}";
        }

        private sealed class MetricItem
        {
            public MetricItem(string label, string value)
            {
                Label = label;
                Value = value;
            }

            public string Label { get; }
            public string Value { get; }
        }

        private sealed class TrendItem
        {
            public string Label { get; set; } = string.Empty;
            public int Count { get; set; }
            public double Percent { get; set; }
        }

        private sealed class RecentPatientRow
        {
            public string AdmissionDateDisplay { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Contact { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }
    }
}
