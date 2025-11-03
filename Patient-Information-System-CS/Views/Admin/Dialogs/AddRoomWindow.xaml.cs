using System;
using System.Windows;

namespace Patient_Information_System_CS.Views.Admin.Dialogs
{
    public partial class AddRoomWindow : Window
    {
        public AddRoomWindow()
        {
            InitializeComponent();
        }

        public int RoomNumber { get; private set; }
        public string RoomType { get; private set; } = string.Empty;
        public int Capacity { get; private set; }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Visibility = Visibility.Collapsed;

            if (!int.TryParse(RoomNumberTextBox.Text.Trim(), out var roomNumber) || roomNumber <= 0)
            {
                ShowError("Room number must be a positive number.");
                return;
            }

            if (string.IsNullOrWhiteSpace(RoomTypeTextBox.Text))
            {
                ShowError("Room type is required.");
                return;
            }

            if (!int.TryParse(RoomCapacityTextBox.Text.Trim(), out var capacity) || capacity <= 0 || capacity > byte.MaxValue)
            {
                ShowError("Capacity must be between 1 and 255.");
                return;
            }

            RoomNumber = roomNumber;
            RoomType = RoomTypeTextBox.Text.Trim();
            Capacity = capacity;

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
