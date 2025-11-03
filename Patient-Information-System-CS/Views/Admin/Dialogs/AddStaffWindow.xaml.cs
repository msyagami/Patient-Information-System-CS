using System;
using System.Windows;
using System.Windows.Controls;

namespace Patient_Information_System_CS.Views.Admin.Dialogs
{
    public partial class AddStaffWindow : Window
    {
        public AddStaffWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        public string FullName => FullNameTextBox.Text.Trim();
        public string Email => EmailTextBox.Text.Trim();
        public string ContactNumber => ContactTextBox.Text.Trim();
        public bool ShouldApprove => ApproveCheckBox.IsChecked == true;
        public DateTime BirthDate => BirthDatePicker.SelectedDate ?? DateTime.Today.AddYears(-25);
        public string Sex => SexComboBox.SelectedValue as string ?? "U";
        public string Address => AddressTextBox.Text.Trim();
        public string EmergencyContact => EmergencyContactTextBox.Text.Trim();
        public string EmergencyRelationship => EmergencyRelationshipTextBox.Text.Trim();
        public string Nationality => NationalityTextBox.Text.Trim();

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            BirthDatePicker.SelectedDate ??= DateTime.Today.AddYears(-25);
            SexComboBox.SelectedValue ??= "U";
            if (string.IsNullOrWhiteSpace(NationalityTextBox.Text))
            {
                NationalityTextBox.Text = "PH";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FullName))
            {
                ShowError("Full name is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(ContactNumber))
            {
                ShowError("Contact number is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Address))
            {
                ShowError("Address is required.");
                return;
            }

            if (BirthDatePicker.SelectedDate is null)
            {
                ShowError("Please select a birth date.");
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
