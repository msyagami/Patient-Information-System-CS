using System;
using System.Linq;
using System.Windows;
using Patient_Information_System_CS.Models;

namespace Patient_Information_System_CS.Views.Admin.Dialogs
{
    public partial class AddDoctorWindow : Window
    {
        public AddDoctorWindow()
        {
            InitializeComponent();
            StatusComboBox.ItemsSource = Enum.GetValues(typeof(DoctorStatus)).Cast<DoctorStatus>();
            StatusComboBox.SelectedItem = DoctorStatus.OnHold;
        }

        public string FullName => FullNameTextBox.Text.Trim();
        public string Email => EmailTextBox.Text.Trim();
        public string Department => DepartmentTextBox.Text.Trim();
        public string LicenseNumber => LicenseTextBox.Text.Trim();
        public string ContactNumber => ContactTextBox.Text.Trim();
        public string Address => AddressTextBox.Text.Trim();
        public DoctorStatus SelectedStatus => StatusComboBox.SelectedItem is DoctorStatus status ? status : DoctorStatus.OnHold;

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FullName))
            {
                ShowError("Full name is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Department))
            {
                ShowError("Department is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(ContactNumber))
            {
                ShowError("Contact number is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(LicenseNumber))
            {
                ShowError("License number is required.");
                return;
            }

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }
    }
}
