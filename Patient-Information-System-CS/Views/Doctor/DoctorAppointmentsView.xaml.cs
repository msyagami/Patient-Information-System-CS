using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;

namespace Patient_Information_System_CS.Views.Doctor
{
    public partial class DoctorAppointmentsView : UserControl
    {
        private readonly HospitalDataService _dataService = HospitalDataService.Instance;
        private readonly UserAccount? _doctorAccount;
        private Appointment? _selectedAppointment;

        public DoctorAppointmentsView(UserAccount? doctorAccount)
        {
            _doctorAccount = doctorAccount;
            InitializeComponent();
            Loaded += DoctorAppointmentsView_Loaded;
        }

        private void DoctorAppointmentsView_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshAppointments();
        }

        private void RefreshAppointments(int? appointmentIdToSelect = null)
        {
            if (_doctorAccount is null)
            {
                AppointmentsDataGrid.ItemsSource = Array.Empty<Appointment>();
                DisplayAppointment(null);
                TodayBadge.Visibility = Visibility.Collapsed;
                return;
            }

            var appointments = _dataService.GetAppointmentsForDoctor(_doctorAccount.UserId).ToList();
            AppointmentsDataGrid.ItemsSource = appointments;

            var hasTodayAppointment = appointments.Any(a => a.ScheduledFor.Date == DateTime.Today);
            TodayBadge.Visibility = hasTodayAppointment ? Visibility.Visible : Visibility.Collapsed;

            Appointment? appointmentToDisplay = null;
            if (appointmentIdToSelect is not null)
            {
                appointmentToDisplay = appointments.FirstOrDefault(a => a.AppointmentId == appointmentIdToSelect);
            }

            if (appointmentToDisplay is null)
            {
                appointmentToDisplay = AppointmentsDataGrid.SelectedItem as Appointment;
            }

            DisplayAppointment(appointmentToDisplay);
        }

        private void AppointmentsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AppointmentsDataGrid.SelectedItem is Appointment appointment)
            {
                DisplayAppointment(appointment);
            }
            else
            {
                DisplayAppointment(null);
            }
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAppointment is null)
            {
                return;
            }

            if (_selectedAppointment.Status != AppointmentStatus.Pending)
            {
                MessageBox.Show("Only pending appointments can be accepted.", "Action not allowed", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _dataService.AcceptAppointment(_selectedAppointment);
            RefreshAppointments(_selectedAppointment.AppointmentId);
        }

        private void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAppointment is null)
            {
                return;
            }

            if (_selectedAppointment.Status != AppointmentStatus.Accepted)
            {
                MessageBox.Show("Accept the appointment before marking it as completed.", "Action not allowed", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _dataService.CompleteAppointment(_selectedAppointment);
            RefreshAppointments(_selectedAppointment.AppointmentId);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAppointment is null)
            {
                return;
            }

            if (_selectedAppointment.Status == AppointmentStatus.Completed)
            {
                MessageBox.Show("Completed appointments can no longer be cancelled.", "Action not allowed", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirmation = MessageBox.Show($"Cancel the appointment with {_selectedAppointment.PatientName}?", "Cancel Appointment", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            _dataService.CancelAppointment(_selectedAppointment);
            RefreshAppointments();
        }

        private void DisplayAppointment(Appointment? appointment)
        {
            _selectedAppointment = appointment;

            if (appointment is null)
            {
                AppointmentTitleTextBlock.Text = "Select an appointment";
                PatientNameTextBlock.Text = "-";
                ScheduledTextBlock.Text = "-";
                StatusTextBlock.Text = "-";
                DescriptionTextBlock.Text = "-";
                AcceptButton.Visibility = Visibility.Collapsed;
                CompleteButton.Visibility = Visibility.Collapsed;
                CancelButton.Visibility = Visibility.Collapsed;
                InstructionBorder.Visibility = Visibility.Collapsed;
                return;
            }

            AppointmentTitleTextBlock.Text = $"Appointment #{appointment.AppointmentId}";
            PatientNameTextBlock.Text = appointment.PatientName;
            ScheduledTextBlock.Text = appointment.ScheduledFor.ToString("MMMM dd, yyyy h:mm tt");
            StatusTextBlock.Text = appointment.StatusDisplay;
            DescriptionTextBlock.Text = string.IsNullOrWhiteSpace(appointment.Description) ? "No description provided." : appointment.Description;

            AcceptButton.Visibility = appointment.Status == AppointmentStatus.Pending ? Visibility.Visible : Visibility.Collapsed;
            CompleteButton.Visibility = appointment.Status == AppointmentStatus.Accepted ? Visibility.Visible : Visibility.Collapsed;
            CancelButton.Visibility = appointment.Status == AppointmentStatus.Completed ? Visibility.Collapsed : Visibility.Visible;
            InstructionBorder.Visibility = Visibility.Visible;
        }
    }
}
