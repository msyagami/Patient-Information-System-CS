using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Patient_Information_System_CS.Models;

namespace Patient_Information_System_CS.Views.Admin.Dialogs
{
    public partial class DischargeBillingWindow : Window
    {
        public DischargeBillingWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        public decimal RoomCharge { get; private set; }
        public decimal DoctorFee { get; private set; }
        public decimal MedicineCost { get; private set; }
        public decimal OtherCharges { get; private set; }
        public bool MarkAsPaid { get; private set; }
        public string? Notes { get; private set; }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            RoomChargeTextBox.Focus();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParseAmount(RoomChargeTextBox, out var roomCharge))
            {
                ShowError("Enter a valid amount for the room charge (0 or higher).");
                return;
            }

            if (!TryParseAmount(DoctorFeeTextBox, out var doctorFee))
            {
                ShowError("Enter a valid amount for the doctor fee (0 or higher).");
                return;
            }

            if (!TryParseAmount(MedicineCostTextBox, out var medicineCost))
            {
                ShowError("Enter a valid amount for the medicine cost (0 or higher).");
                return;
            }

            if (!TryParseAmount(OtherChargesTextBox, out var otherCharges))
            {
                ShowError("Enter a valid amount for the other charges (0 or higher).");
                return;
            }

            RoomCharge = roomCharge;
            DoctorFee = doctorFee;
            MedicineCost = medicineCost;
            OtherCharges = otherCharges;
            MarkAsPaid = MarkAsPaidCheckBox.IsChecked == true;
            Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private static bool TryParseAmount(TextBox textBox, out decimal value)
        {
            var raw = textBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                value = 0m;
                return true;
            }

            return decimal.TryParse(raw, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.CurrentCulture, out value) && value >= 0m;
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }

        public DischargeBillingRequest BuildRequest(int patientUserId) => new()
        {
            PatientUserId = patientUserId,
            RoomCharge = RoomCharge,
            DoctorFee = DoctorFee,
            MedicineCost = MedicineCost,
            OtherCharges = OtherCharges,
            MarkAsPaid = MarkAsPaid,
            Notes = Notes
        };
    }
}
