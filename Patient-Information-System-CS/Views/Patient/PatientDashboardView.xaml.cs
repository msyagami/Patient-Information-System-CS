using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;

namespace Patient_Information_System_CS.Views.Patient
{
    public partial class PatientDashboardView : UserControl
    {
        private static readonly SolidColorBrush DangerBrush = new(Color.FromRgb(254, 226, 226));
        private static readonly SolidColorBrush WarningBrush = new(Color.FromRgb(255, 243, 207));
        private static readonly SolidColorBrush InfoBrush = new(Color.FromRgb(219, 234, 254));

        private readonly HospitalDataService _dataService = HospitalDataService.Instance;
        private readonly UserAccount? _patientAccount;

        public PatientDashboardView(UserAccount? patientAccount)
        {
            _patientAccount = patientAccount;
            InitializeComponent();
            Loaded += PatientDashboardView_Loaded;
        }

        private void PatientDashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateDashboard();
        }

        private void PopulateDashboard()
        {
            if (_patientAccount?.PatientProfile is null)
            {
                NotificationBorder.Visibility = Visibility.Collapsed;
                DoctorNameTextBlock.Text = "Unassigned";
                DoctorDepartmentTextBlock.Text = "Not available";
                DoctorStatusTextBlock.Text = "No doctor assigned";
                NextAppointmentValueTextBlock.Text = "No upcoming visits";
                NextAppointmentStatusTextBlock.Text = "Status: Not scheduled";
                AdmissionStatusTextBlock.Text = "Not admitted";
                AdmissionRoomTextBlock.Text = "Room assignment pending";
                PendingBillAmountTextBlock.Text = 0m.ToString("C", CultureInfo.CurrentCulture);
                PendingBillStatusTextBlock.Text = "No outstanding balance";
                return;
            }

            var profile = _patientAccount.PatientProfile;
            var doctorAccount = _dataService.GetDoctorById(profile.AssignedDoctorId);

            DoctorNameTextBlock.Text = !string.IsNullOrWhiteSpace(doctorAccount?.DisplayName)
                ? doctorAccount.DisplayName
                : string.IsNullOrWhiteSpace(profile.AssignedDoctorName) ? "Unassigned" : profile.AssignedDoctorName;

            DoctorDepartmentTextBlock.Text = string.IsNullOrWhiteSpace(doctorAccount?.DoctorProfile?.Department)
                ? "Not specified"
                : doctorAccount.DoctorProfile!.Department;

            DoctorStatusTextBlock.Text = doctorAccount?.DoctorProfile?.Status switch
            {
                DoctorStatus.Available => "Available for consultations",
                DoctorStatus.NotAvailable => "Currently unavailable",
                DoctorStatus.OnHold => "Awaiting approval",
                _ => profile.IsCurrentlyAdmitted ? "Assigned" : "Unassigned"
            };

            var appointments = _dataService.GetAppointmentsForPatient(_patientAccount.UserId).ToList();
            var nextAppointment = appointments
                .Where(appointment => appointment.ScheduledFor >= DateTime.Now && appointment.Status != AppointmentStatus.Completed && appointment.Status != AppointmentStatus.Rejected)
                .OrderBy(appointment => appointment.ScheduledFor)
                .FirstOrDefault();

            if (nextAppointment is null)
            {
                NextAppointmentValueTextBlock.Text = "No upcoming visits";
                NextAppointmentStatusTextBlock.Text = "Status: Not scheduled";
            }
            else
            {
                NextAppointmentValueTextBlock.Text = nextAppointment.ScheduledFor.ToString("MMM dd, yyyy h:mm tt", CultureInfo.CurrentCulture);
                NextAppointmentStatusTextBlock.Text = $"Status: {nextAppointment.StatusDisplay}";
            }

            AdmissionStatusTextBlock.Text = profile.IsCurrentlyAdmitted ? "Admitted" : "Discharged";

            var invoices = _dataService.GetInvoicesForPatient(_patientAccount.UserId).ToList();
            var latestInvoice = invoices.FirstOrDefault();
            var pendingInvoice = invoices.FirstOrDefault(invoice => !invoice.IsPaid);

            if (profile.IsCurrentlyAdmitted)
            {
                AdmissionRoomTextBlock.Text = string.IsNullOrWhiteSpace(profile.RoomAssignment)
                    ? "Room assignment pending"
                    : profile.RoomAssignment;
            }
            else
            {
                AdmissionRoomTextBlock.Text = latestInvoice is null
                    ? "Last stay completed"
                    : $"Released on {latestInvoice.ReleaseDate:MMM dd, yyyy}";
            }

            if (pendingInvoice is null)
            {
                PendingBillAmountTextBlock.Text = 0m.ToString("C", CultureInfo.CurrentCulture);
                PendingBillStatusTextBlock.Text = "No outstanding balance";
            }
            else
            {
                PendingBillAmountTextBlock.Text = pendingInvoice.Total.ToString("C", CultureInfo.CurrentCulture);
                PendingBillStatusTextBlock.Text = $"Invoice #{pendingInvoice.InvoiceId} released {pendingInvoice.ReleaseDate:MMM dd, yyyy}";
            }

            UpdateNotification(profile, pendingInvoice);
        }

        private void UpdateNotification(PatientProfile profile, BillingRecord? pendingInvoice)
        {
            if (profile.HasUnpaidBills)
            {
                NotificationBorder.Visibility = Visibility.Visible;
                NotificationBorder.Background = DangerBrush;
                var amountText = pendingInvoice is null
                    ? "Outstanding hospital charges remain."
                    : $"Outstanding balance: {pendingInvoice.Total.ToString("C", CultureInfo.CurrentCulture)}.";
                NotificationTextBlock.Text = amountText + " Please contact billing to settle your account.";
                return;
            }

            if (!profile.IsCurrentlyAdmitted)
            {
                NotificationBorder.Visibility = Visibility.Visible;
                NotificationBorder.Background = WarningBrush;
                NotificationTextBlock.Text = "You have been discharged. Your account stays active so you can review bills and schedule follow-ups.";
                return;
            }

            NotificationBorder.Visibility = Visibility.Visible;
            NotificationBorder.Background = InfoBrush;
            NotificationTextBlock.Text = "You are currently admitted. Reach out to your care team if you need assistance.";
        }
    }
}
