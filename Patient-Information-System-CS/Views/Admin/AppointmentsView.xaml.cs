using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;

namespace Patient_Information_System_CS.Views.Admin
{
    public partial class AppointmentsView : UserControl
    {
        private readonly HospitalDataService _dataService = HospitalDataService.Instance;

        public AppointmentsView()
        {
            InitializeComponent();
            Loaded += AppointmentsView_Loaded;
        }

        private void AppointmentsView_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshTables();
        }

        private void RefreshTables()
        {
            var pending = _dataService.GetPendingAppointments().ToList();
            PendingAppointmentsGrid.ItemsSource = pending;
            PendingAppointmentsBanner.Text = pending.Count switch
            {
                0 => "No appointment requests are awaiting review.",
                1 => "1 appointment request is awaiting your decision.",
                _ => $"{pending.Count} appointment requests need action."
            };

            ManagedAppointmentsGrid.ItemsSource = _dataService.GetManagedAppointments().ToList();
        }

        private void ApproveAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: Appointment appointment })
            {
                return;
            }

            if (appointment.Status != AppointmentStatus.Pending)
            {
                MessageBox.Show("Only pending requests can be approved.", "Action not allowed", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _dataService.AcceptAppointment(appointment);
            RefreshTables();
            MessageBox.Show($"Appointment for {appointment.PatientName} has been approved.", "Appointment Approved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RejectAppointment_Click(object sender, RoutedEventArgs e)
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

            var confirmation = MessageBox.Show($"Reject the appointment requested by {appointment.PatientName}?", "Reject Appointment", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            _dataService.RejectAppointment(appointment);
            RefreshTables();
        }

        private void CompleteAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: Appointment appointment })
            {
                return;
            }

            if (appointment.Status != AppointmentStatus.Accepted)
            {
                MessageBox.Show("Only accepted appointments can be marked as completed.", "Action not allowed", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _dataService.CompleteAppointment(appointment);
            RefreshTables();
            MessageBox.Show($"Appointment with {appointment.PatientName} is now completed.", "Appointment Completed", MessageBoxButton.OK, MessageBoxImage.Information);
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

            var confirmation = MessageBox.Show($"Cancel the scheduled appointment for {appointment.PatientName}?", "Cancel Appointment", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            _dataService.RejectAppointment(appointment);
            RefreshTables();
        }
    }
}
