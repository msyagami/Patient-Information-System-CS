using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;
using Patient_Information_System_CS.Views.Admin.Dialogs;

namespace Patient_Information_System_CS.Views.Admin
{
    public partial class PatientsView : UserControl
    {
        private readonly HospitalDataService _dataService = HospitalDataService.Instance;

        public PatientsView()
        {
            InitializeComponent();
            Loaded += PatientsView_Loaded;
        }

        private void PatientsView_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshTables();
        }

        private void RefreshTables()
        {
            ActivePatientsGrid.ItemsSource = _dataService.GetApprovedPatients().ToList();

            var pending = _dataService.GetPendingPatients().ToList();
            PendingPatientsGrid.ItemsSource = pending;
            PendingPatientsBanner.Text = pending.Count switch
            {
                0 => "No patient registrations awaiting approval.",
                1 => "1 patient registration awaiting approval.",
                _ => $"{pending.Count} patient registrations awaiting approval."
            };
        }

        private void ApprovePatient_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: UserAccount patient })
            {
                return;
            }

            _dataService.ApprovePatient(patient);
            RefreshTables();
            MessageBox.Show($"{patient.DisplayName} has been approved.", "Patient Approved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RejectPatient_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: UserAccount patient })
            {
                return;
            }

            var confirmation = MessageBox.Show(
                $"Reject and remove {patient.DisplayName}?",
                "Reject Patient",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            _dataService.RejectPatient(patient);
            RefreshTables();
        }

        private void AddPatientButton_Click(object sender, RoutedEventArgs e)
        {
            var doctors = _dataService.GetActiveDoctors().Concat(_dataService.GetUnavailableDoctors()).ToList();
            var availableRooms = _dataService.GetRoomStatuses()
                                             .Where(room => room.AvailableSlots > 0)
                                             .ToList();

            var dialog = new AddPatientWindow(doctors, availableRooms)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var account = _dataService.CreatePatientAccount(dialog.FullName,
                                                            dialog.Email,
                                                            dialog.ContactNumber,
                                                            dialog.Address,
                                                            dialog.DateOfBirth,
                                                            dialog.ShouldApprove,
                                                            dialog.IsCurrentlyAdmitted,
                                                            dialog.SelectedDoctorId,
                                                            dialog.InsuranceProvider,
                                                            dialog.EmergencyContact,
                                                            dialog.RoomAssignment,
                                                            dialog.Sex,
                                                            dialog.EmergencyRelationship,
                                                            dialog.Nationality);

            RefreshTables();
            MessageBox.Show($"Patient record for {account.DisplayName} created.",
                            "Patient Added",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }
    }
}
