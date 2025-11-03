using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;
using Patient_Information_System_CS.Views.Admin.Dialogs;

namespace Patient_Information_System_CS.Views.Staff
{
    public partial class ReceptionistStaffsView : UserControl
    {
        private readonly HospitalDataService _dataService = HospitalDataService.Instance;
        private bool _isSubscribed;

        public ReceptionistStaffsView()
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
            var active = _dataService.GetApprovedStaff()
                                      .Where(account => account.Role == UserRole.Staff)
                                      .ToList();
            ActiveStaffGrid.ItemsSource = active;

            var pending = _dataService.GetPendingStaff().ToList();
            PendingStaffGrid.ItemsSource = pending;
            PendingStaffBanner.Text = pending.Count switch
            {
                0 => "All staff registrations are approved.",
                1 => "1 staff registration awaiting approval.",
                _ => $"{pending.Count} staff registrations awaiting approval."
            };
        }

        private void ApproveStaff_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: UserAccount account })
            {
                return;
            }

            _dataService.ApproveStaff(account);
            RefreshTables();
            MessageBox.Show($"{account.DisplayName} has been approved.",
                            "Approval Complete",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }

        private void RejectStaff_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: UserAccount account })
            {
                return;
            }

            var confirmation = MessageBox.Show($"Reject and remove {account.DisplayName}?",
                                               "Reject Staff",
                                               MessageBoxButton.YesNo,
                                               MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            _dataService.RejectStaff(account);
            RefreshTables();
        }

        private void AddStaffButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddStaffWindow(allowAdminCreation: false)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var role = dialog.SelectedRole == UserRole.Admin ? UserRole.Staff : dialog.SelectedRole;
            var account = _dataService.CreateStaffAccount(dialog.FullName,
                                                          dialog.Email,
                                                          dialog.ContactNumber,
                                                          dialog.ShouldApprove,
                                                          dialog.BirthDate,
                                                          dialog.Sex,
                                                          dialog.Address,
                                                          dialog.EmergencyContact,
                                                          dialog.EmergencyRelationship,
                                                          dialog.Nationality,
                                                          role);

            RefreshTables();

            var credentialsMessage = $"Staff account for {account.DisplayName} created.\n\nUsername: {account.Username}\nTemporary password: {account.GetPlainTextPassword()}";
            MessageBox.Show(credentialsMessage,
                            "Staff Added",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }
    }
}
