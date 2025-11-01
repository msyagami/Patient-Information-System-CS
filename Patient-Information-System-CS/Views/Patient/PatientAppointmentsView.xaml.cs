using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;

namespace Patient_Information_System_CS.Views.Patient
{
    public partial class PatientAppointmentsView : UserControl
    {
        private readonly HospitalDataService _dataService = HospitalDataService.Instance;
        private readonly UserAccount? _patientAccount;

        public PatientAppointmentsView(UserAccount? patientAccount)
        {
            _patientAccount = patientAccount;
            InitializeComponent();
            Loaded += PatientAppointmentsView_Loaded;
        }

        private void PatientAppointmentsView_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshAppointments();
        }

        private void RefreshAppointments()
        {
            if (_patientAccount is null)
            {
                AppointmentsGrid.ItemsSource = Array.Empty<Appointment>();
                EmptyStateTextBlock.Visibility = Visibility.Visible;
                LastUpdatedTextBlock.Text = string.Empty;
                return;
            }

            var appointments = _dataService.GetAppointmentsForPatient(_patientAccount.UserId).ToList();
            AppointmentsGrid.ItemsSource = appointments;
            EmptyStateTextBlock.Visibility = appointments.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            LastUpdatedTextBlock.Text = appointments.Count == 0
                ? ""
                : $"Showing {appointments.Count} appointment(s). Last updated {DateTime.Now:T}.";
        }
    }
}
