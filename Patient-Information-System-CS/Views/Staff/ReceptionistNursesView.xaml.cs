using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;
using Patient_Information_System_CS.Views.Admin.Dialogs;

namespace Patient_Information_System_CS.Views.Staff
{
    public partial class ReceptionistNursesView : UserControl
    {
        private readonly HospitalDataService _dataService = HospitalDataService.Instance;
        private bool _isSubscribed;

        public ReceptionistNursesView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_isSubscribed)
            {
                _dataService.AdmissionsChanged += OnAdmissionsChanged;
                _isSubscribed = true;
            }

            RefreshTables();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_isSubscribed)
            {
                _dataService.AdmissionsChanged -= OnAdmissionsChanged;
                _isSubscribed = false;
            }
        }

        private void OnAdmissionsChanged(object? sender, System.EventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(RefreshTables);
                return;
            }

            RefreshTables();
        }

        private void RefreshTables()
        {
            var active = _dataService.GetActiveNurses()
                .Concat(_dataService.GetUnavailableNurses())
                .OrderBy(account => account.DisplayName)
                .ToList();
            ActiveNursesGrid.ItemsSource = active;

            var pending = _dataService.GetPendingNurses().OrderBy(account => account.DisplayName).ToList();
            PendingNursesGrid.ItemsSource = pending;
            PendingNursesBanner.Text = pending.Count switch
            {
                0 => "No nurse registrations awaiting approval.",
                1 => "1 nurse registration awaiting approval.",
                _ => $"{pending.Count} nurse registrations awaiting approval."
            };
        }

        private void ApproveNurse_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: UserAccount nurse })
            {
                return;
            }

            _dataService.ApproveNurse(nurse);
            RefreshTables();
            MessageBox.Show($"{nurse.DisplayName} has been approved.",
                            "Nurse Approved",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }

        private void RejectNurse_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: UserAccount nurse })
            {
                return;
            }

            var confirmation = MessageBox.Show($"Reject and remove {nurse.DisplayName}?",
                                               "Reject Nurse",
                                               MessageBoxButton.YesNo,
                                               MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            _dataService.RejectNurse(nurse);
            RefreshTables();
        }

        private void ToggleNurseAvailability_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: UserAccount nurse })
            {
                return;
            }

            _dataService.ToggleNurseAvailability(nurse);
            RefreshTables();
        }

        private void AddNurseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddNurseWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var account = _dataService.CreateNurseAccount(dialog.FullName,
                                                          dialog.Email,
                                                          dialog.ContactNumber,
                                                          dialog.Department,
                                                          dialog.Specialization,
                                                          dialog.LicenseNumber,
                                                          dialog.Address,
                                                          dialog.SelectedStatus,
                                                          dialog.BirthDate,
                                                          dialog.Sex,
                                                          dialog.EmergencyContact,
                                                          dialog.EmergencyRelationship,
                                                          dialog.Nationality);

            RefreshTables();

            var credentialsMessage = $"Nurse account for {account.DisplayName} created.\n\nUsername: {account.Username}\nTemporary password: {account.GetPlainTextPassword()}";
            MessageBox.Show(credentialsMessage,
                            "Nurse Added",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }
    }
}
