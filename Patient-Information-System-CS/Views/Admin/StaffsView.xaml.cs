using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;
using Patient_Information_System_CS.Views.Admin.Dialogs;

namespace Patient_Information_System_CS.Views.Admin
{
    public partial class StaffsView : UserControl
    {
        private readonly HospitalDataService _dataService = HospitalDataService.Instance;

        public StaffsView()
        {
            InitializeComponent();
            Loaded += StaffsView_Loaded;
        }

        private void StaffsView_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshTables();
        }

        private void RefreshTables()
        {
            ActiveStaffGrid.ItemsSource = _dataService.GetApprovedStaff().ToList();

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
            MessageBox.Show($"{account.DisplayName} has been approved.", "Approval Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RejectStaff_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: UserAccount account })
            {
                return;
            }

            var confirmation = MessageBox.Show(
                $"Are you sure you want to reject and remove {account.DisplayName}?",
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
            var dialog = new AddStaffWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

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
                                                           dialog.SelectedRole);

            RefreshTables();

            var credentialsMessage = $"Staff account for {account.DisplayName} created.\n\nUsername: {account.Username}\nTemporary password: {account.GetPlainTextPassword()}";
            MessageBox.Show(credentialsMessage,
                            "Staff Added",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }
    }
}
