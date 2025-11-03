using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;

namespace Patient_Information_System_CS.Views.Admin
{
    public partial class BillingView : UserControl
    {
        private readonly HospitalDataService _dataService = HospitalDataService.Instance;
        private BillingRecord? _selectedInvoice;

        public BillingView()
        {
            InitializeComponent();
            Loaded += BillingView_Loaded;
        }

        private void BillingView_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshTables();
        }

        private void RefreshTables()
        {
            OutstandingInvoicesGrid.ItemsSource = _dataService.GetOutstandingInvoices().ToList();
            PaidInvoicesGrid.ItemsSource = _dataService.GetPaidInvoices().ToList();

            if (_selectedInvoice is not null)
            {
                _selectedInvoice = _dataService.GetInvoiceById(_selectedInvoice.InvoiceId);
            }

            if (_selectedInvoice is null)
            {
                OutstandingInvoicesGrid.SelectedItem = null;
                PaidInvoicesGrid.SelectedItem = null;
            }
            else if (_selectedInvoice.IsPaid)
            {
                PaidInvoicesGrid.SelectedItem = _selectedInvoice;
                OutstandingInvoicesGrid.SelectedItem = null;
            }
            else
            {
                OutstandingInvoicesGrid.SelectedItem = _selectedInvoice;
                PaidInvoicesGrid.SelectedItem = null;
            }

            DisplayInvoiceDetails(_selectedInvoice);
        }

        private void OutstandingInvoicesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            if (OutstandingInvoicesGrid.SelectedItem is BillingRecord invoice)
            {
                PaidInvoicesGrid.SelectedItem = null;
                _selectedInvoice = invoice;
                DisplayInvoiceDetails(invoice);
            }
            else if (PaidInvoicesGrid.SelectedItem is null)
            {
                _selectedInvoice = null;
                DisplayInvoiceDetails(null);
            }
        }

        private void PaidInvoicesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            if (PaidInvoicesGrid.SelectedItem is BillingRecord invoice)
            {
                OutstandingInvoicesGrid.SelectedItem = null;
                _selectedInvoice = invoice;
                DisplayInvoiceDetails(invoice);
            }
            else if (OutstandingInvoicesGrid.SelectedItem is null)
            {
                _selectedInvoice = null;
                DisplayInvoiceDetails(null);
            }
        }

        private void MarkAsPaidButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedInvoice is null)
            {
                return;
            }

            _dataService.MarkInvoicePaid(_selectedInvoice);
            MessageBox.Show($"Invoice #{_selectedInvoice.InvoiceId} has been marked as paid.", "Invoice Updated", MessageBoxButton.OK, MessageBoxImage.Information);
            RefreshTables();
        }

        private void DownloadInvoiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedInvoice is null)
            {
                return;
            }

            MessageBox.Show("Exporting invoices will be added in a future update.", "Export not available", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DisplayInvoiceDetails(BillingRecord? invoice)
        {
            if (invoice is null)
            {
                InvoiceTitleTextBlock.Text = "Select an invoice";
                InvoiceStatusBadge.Visibility = Visibility.Collapsed;
                PatientValueTextBlock.Text = "-";
                DoctorValueTextBlock.Text = "-";
                ContactValueTextBlock.Text = "-";
                AdmitDateTextBlock.Text = "-";
                ReleaseDateTextBlock.Text = "-";
                DaysStayedTextBlock.Text = "-";
                RoomChargeTextBlock.Text = "-";
                DoctorFeeTextBlock.Text = "-";
                MedicineCostTextBlock.Text = "-";
                OtherChargeTextBlock.Text = "-";
                TotalAmountTextBlock.Text = "Total: -";
                NotesTextBlock.Text = "-";
                MarkAsPaidButton.Visibility = Visibility.Collapsed;
                DownloadInvoiceButton.Visibility = Visibility.Collapsed;
                return;
            }

            InvoiceTitleTextBlock.Text = $"Invoice #{invoice.InvoiceId}";
            InvoiceStatusBadge.Visibility = Visibility.Visible;
            InvoiceStatusTextBlock.Text = invoice.IsPaid ? "Paid" : "Outstanding";
            InvoiceStatusBadge.Background = invoice.IsPaid ? Brushes.SeaGreen : Brushes.Firebrick;

            PatientValueTextBlock.Text = invoice.PatientName;
            DoctorValueTextBlock.Text = string.IsNullOrWhiteSpace(invoice.DoctorName) ? "Unassigned" : invoice.DoctorName;
            ContactValueTextBlock.Text = string.IsNullOrWhiteSpace(invoice.Address)
                ? invoice.ContactNumber
                : string.Join(Environment.NewLine, new[] { invoice.ContactNumber, invoice.Address });

            AdmitDateTextBlock.Text = invoice.AdmitDate.ToString("MMM dd, yyyy");
            ReleaseDateTextBlock.Text = invoice.ReleaseDate.ToString("MMM dd, yyyy");
            DaysStayedTextBlock.Text = invoice.DaysStayed switch
            {
                1 => "1 day",
                _ => $"{invoice.DaysStayed} days"
            };

            RoomChargeTextBlock.Text = invoice.RoomCharge.ToString("C");
            DoctorFeeTextBlock.Text = invoice.DoctorFee.ToString("C");
            MedicineCostTextBlock.Text = invoice.MedicineCost.ToString("C");
            OtherChargeTextBlock.Text = invoice.OtherCharge.ToString("C");
            TotalAmountTextBlock.Text = $"Total: {invoice.Total:C}";
            NotesTextBlock.Text = string.IsNullOrWhiteSpace(invoice.Notes) ? "-" : invoice.Notes;

            MarkAsPaidButton.Visibility = invoice.IsPaid ? Visibility.Collapsed : Visibility.Visible;
            DownloadInvoiceButton.Visibility = Visibility.Visible;
        }
    }
}
