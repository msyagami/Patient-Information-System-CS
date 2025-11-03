using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;
using Patient_Information_System_CS.Views.Admin.Dialogs;

namespace Patient_Information_System_CS.Views.Admin
{
    public partial class DoctorsView : UserControl
    {
        private readonly HospitalDataService _dataService = HospitalDataService.Instance;

        public DoctorsView()
        {
            InitializeComponent();
            Loaded += DoctorsView_Loaded;
        }

        private void DoctorsView_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshTables();
        }

        private void RefreshTables()
        {
            ActiveDoctorsGrid.ItemsSource = _dataService.GetActiveDoctors().Concat(_dataService.GetUnavailableDoctors()).ToList();

            var pending = _dataService.GetPendingDoctors().ToList();
            PendingDoctorsGrid.ItemsSource = pending;
            PendingDoctorsBanner.Text = pending.Count switch
            {
                0 => "No doctor registrations awaiting approval.",
                1 => "1 doctor registration awaiting approval.",
                _ => $"{pending.Count} doctor registrations awaiting approval."
            };
        }

        private void ApproveDoctor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: UserAccount doctor })
            {
                return;
            }

            _dataService.ApproveDoctor(doctor);
            RefreshTables();
            MessageBox.Show($"{doctor.DisplayName} has been approved.", "Doctor Approved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RejectDoctor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: UserAccount doctor })
            {
                return;
            }

            var confirmation = MessageBox.Show(
                $"Reject and remove {doctor.DisplayName}?",
                "Reject Doctor",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            _dataService.RejectDoctor(doctor);
            RefreshTables();
        }

        private void ToggleDoctorAvailability_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: UserAccount doctor })
            {
                return;
            }

            _dataService.ToggleDoctorAvailability(doctor);
            RefreshTables();
        }

        private void AddDoctorButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddDoctorWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var account = _dataService.CreateDoctorAccount(dialog.FullName,
                                                           dialog.Email,
                                                           dialog.ContactNumber,
                                                           dialog.Department,
                                                           dialog.LicenseNumber,
                                                           dialog.Address,
                                                           dialog.SelectedStatus,
                                                           dialog.BirthDate,
                                                           dialog.Sex,
                                                           dialog.EmergencyContact,
                                                           dialog.EmergencyRelationship,
                                                           dialog.Nationality);

            RefreshTables();

            var credentialsMessage = $"Doctor account for {account.DisplayName} created.\n\nUsername: {account.Username}\nTemporary password: {account.GetPlainTextPassword()}";
            MessageBox.Show(credentialsMessage,
                            "Doctor Added",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }
    }
}
