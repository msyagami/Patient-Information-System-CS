using System.Windows;

namespace Patient_Information_System_CS.Views.Admin.Dialogs
{
    public partial class AddStaffWindow : Window
    {
        public AddStaffWindow()
        {
            InitializeComponent();
        }

        public string FullName => FullNameTextBox.Text.Trim();
        public string Email => EmailTextBox.Text.Trim();
        public string ContactNumber => ContactTextBox.Text.Trim();
        public bool ShouldApprove => ApproveCheckBox.IsChecked == true;

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
