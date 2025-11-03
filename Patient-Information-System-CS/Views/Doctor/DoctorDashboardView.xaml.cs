using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;

namespace Patient_Information_System_CS.Views.Doctor
{
    public partial class DoctorDashboardView : UserControl
    {
        private readonly HospitalDataService _dataService = HospitalDataService.Instance;
        private readonly UserAccount? _doctorAccount;

        public DoctorDashboardView(UserAccount? doctorAccount)
        {
            _doctorAccount = doctorAccount;
            InitializeComponent();
            Loaded += DoctorDashboardView_Loaded;
        }

        private void DoctorDashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateDashboard();
        }

        private void PopulateDashboard()
        {
            if (_doctorAccount?.DoctorProfile is null)
            {
                DoctorAppointmentsValueTextBlock.Text = "0";
                DoctorAppointmentsSubtitleTextBlock.Text = "No appointments tracked";
                DoctorPatientsValueTextBlock.Text = "0";
                DoctorPatientsSubtitleTextBlock.Text = "No patients assigned";
                DoctorDischargedValueTextBlock.Text = "0";
                DoctorDischargedSubtitleTextBlock.Text = "Past 30 days";
                AppointmentsEmptyTextBlock.Visibility = Visibility.Visible;
                UpcomingAppointmentsDataGrid.ItemsSource = Array.Empty<DoctorAppointmentRow>();
                AppointmentsSummaryTextBlock.Text = "";
                return;
            }

            var allAppointments = _dataService.GetAppointmentsForDoctor(_doctorAccount.UserId).ToList();
            var upcomingAppointments = allAppointments
                .Where(appointment => appointment.ScheduledFor >= DateTime.Now.AddMinutes(-30))
                .Where(appointment => appointment.Status is AppointmentStatus.Pending or AppointmentStatus.Accepted)
                .OrderBy(appointment => appointment.ScheduledFor)
                .Take(20)
                .Select(appointment => new DoctorAppointmentRow
                {
                    ScheduledFor = appointment.ScheduledFor.ToString("MMM dd, yyyy h:mm tt", CultureInfo.CurrentCulture),
                    PatientName = appointment.PatientName,
                    Status = appointment.StatusDisplay,
                    Description = appointment.Description
                })
                .ToList();

            var todayAppointments = allAppointments.Count(appointment => appointment.ScheduledFor.Date == DateTime.Today);

            DoctorAppointmentsValueTextBlock.Text = upcomingAppointments.Count.ToString(CultureInfo.InvariantCulture);
            DoctorAppointmentsSubtitleTextBlock.Text = todayAppointments == 0
                ? "No visits scheduled today"
                : todayAppointments == 1
                    ? "1 visit today"
                    : $"{todayAppointments} visits today";

            UpcomingAppointmentsDataGrid.ItemsSource = upcomingAppointments;
            AppointmentsEmptyTextBlock.Visibility = upcomingAppointments.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            AppointmentsSummaryTextBlock.Text = allAppointments.Count == 0
                ? "No appointments on record."
                : $"Tracking {allAppointments.Count} appointment(s). {upcomingAppointments.Count} upcoming.";

            var assignedPatients = _dataService.GetPatientsForDoctor(_doctorAccount.UserId).ToList();

            var admittedPatients = assignedPatients.Count(account => account.PatientProfile?.IsCurrentlyAdmitted == true);
            var recentDischarges = assignedPatients
                .Count(account => account.PatientProfile?.IsCurrentlyAdmitted == false
                                  && account.PatientProfile?.AdmitDate is not null
                                  && account.PatientProfile.AdmitDate >= DateTime.Today.AddDays(-30));

            DoctorPatientsValueTextBlock.Text = assignedPatients.Count.ToString(CultureInfo.InvariantCulture);
            DoctorPatientsSubtitleTextBlock.Text = admittedPatients == 0
                ? "No current admissions"
                : admittedPatients == 1
                    ? "1 patient admitted"
                    : $"{admittedPatients} patients admitted";

            DoctorDischargedValueTextBlock.Text = recentDischarges.ToString(CultureInfo.InvariantCulture);
            DoctorDischargedSubtitleTextBlock.Text = "Past 30 days";
        }

        private sealed class DoctorAppointmentRow
        {
            public string ScheduledFor { get; set; } = string.Empty;
            public string PatientName { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }
    }
}
