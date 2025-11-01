using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;

namespace Patient_Information_System_CS.Views.Staff
{
    public partial class ReceptionistAppointmentsView : UserControl
    {
        private readonly HospitalDataService _dataService = HospitalDataService.Instance;
        private readonly UserAccount? _staffAccount;

        public ReceptionistAppointmentsView(UserAccount? staffAccount)
        {
            _staffAccount = staffAccount;
            InitializeComponent();
            Loaded += ReceptionistAppointmentsView_Loaded;
        }

        private void ReceptionistAppointmentsView_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateSelectors();
            RefreshTables();
        }

        private void PopulateSelectors()
        {
            var patients = _dataService.GetSchedulablePatients().ToList();
            PatientComboBox.ItemsSource = patients;
            if (patients.Count > 0)
            {
                PatientComboBox.SelectedIndex = 0;
            }

            var doctors = _dataService.GetActiveDoctors().Concat(_dataService.GetUnavailableDoctors()).ToList();
            DoctorComboBox.ItemsSource = doctors;
            if (doctors.Count > 0)
            {
                DoctorComboBox.SelectedIndex = 0;
            }
        }

        private void RefreshTables()
        {
            var pending = _dataService.GetPendingAppointments().ToList();
            PendingAppointmentsGrid.ItemsSource = pending;
            PendingBanner.Text = pending.Count switch
            {
                0 => "No appointment requests awaiting attention.",
                1 => "1 appointment request is pending review.",
                _ => $"{pending.Count} appointment requests are pending review."
            };

            AllAppointmentsGrid.ItemsSource = _dataService.GetAllAppointments().ToList();
        }

        private void ScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            if (PatientComboBox.SelectedItem is not UserAccount patient)
            {
                ShowFormFeedback("Please select a patient before scheduling.", isError: true);
                return;
            }

            var doctor = DoctorComboBox.SelectedItem as UserAccount;
            var date = AppointmentDatePicker.SelectedDate ?? DateTime.Today;

            if (!DateTime.TryParse(AppointmentTimeTextBox.Text, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var timeValue))
            {
                ShowFormFeedback("Enter a valid time (e.g., 9:00 AM).", isError: true);
                AppointmentTimeTextBox.Focus();
                AppointmentTimeTextBox.SelectAll();
                return;
            }

            var scheduledFor = date.Date.Add(timeValue.TimeOfDay);
            var reason = ReasonTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(reason))
            {
                ShowFormFeedback("Please provide a brief reason for the visit.", isError: true);
                ReasonTextBox.Focus();
                return;
            }

            var appointment = _dataService.ScheduleAppointment(patient.UserId, doctor?.UserId, scheduledFor, reason);
            var staffName = _staffAccount?.DisplayName ?? "staff";
            ShowFormFeedback($"Appointment scheduled for {appointment.PatientName} on {scheduledFor:MMM dd, h:mm tt} by {staffName}.", isError: false);
            RefreshTables();
            ResetFormFields();
        }

        private void ApproveAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: Appointment appointment })
            {
                return;
            }

            if (appointment.Status != AppointmentStatus.Pending)
            {
                MessageBox.Show("Only pending appointments can be approved.", "Action not allowed", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _dataService.AcceptAppointment(appointment);
            RefreshTables();
        }

        private void RejectAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: Appointment appointment })
            {
                return;
            }

            var confirmation = MessageBox.Show($"Reject the appointment request from {appointment.PatientName}?", "Reject Appointment", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            _dataService.RejectAppointment(appointment);
            RefreshTables();
        }

        private void CancelAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: Appointment appointment })
            {
                return;
            }

            if (appointment.Status == AppointmentStatus.Completed)
            {
                MessageBox.Show("Completed appointments cannot be cancelled.", "Action not allowed", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirmation = MessageBox.Show($"Cancel the appointment for {appointment.PatientName}?", "Cancel Appointment", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            _dataService.CancelAppointment(appointment);
            RefreshTables();
        }

        private void ShowFormFeedback(string message, bool isError)
        {
            FormFeedbackBorder.Background = isError ? Brushes.FloralWhite : Brushes.Honeydew;
            FormFeedbackBorder.BorderBrush = isError ? Brushes.OrangeRed : Brushes.SeaGreen;
            FormFeedbackTextBlock.Text = message;
            FormFeedbackBorder.Visibility = Visibility.Visible;
        }

        private void ResetFormFields()
        {
            if (PatientComboBox.Items.Count > 0)
            {
                PatientComboBox.SelectedIndex = 0;
            }

            if (DoctorComboBox.Items.Count > 0)
            {
                DoctorComboBox.SelectedIndex = 0;
            }

            AppointmentDatePicker.SelectedDate = DateTime.Today;
            AppointmentTimeTextBox.Text = "9:00 AM";
            ReasonTextBox.Clear();
        }
    }
}
