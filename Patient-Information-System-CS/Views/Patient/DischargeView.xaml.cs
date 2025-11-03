using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;

namespace Patient_Information_System_CS.Views.Patient
{
    public partial class DischargeView : UserControl
    {
        private readonly HospitalDataService _dataService = HospitalDataService.Instance;
        private readonly PdfExportService _pdfExport = PdfExportService.Instance;
        private readonly UserAccount? _currentPatient;

        public DischargeView() : this(null)
        {
        }

        public DischargeView(UserAccount? currentPatient)
        {
            _currentPatient = currentPatient;
            InitializeComponent();
            Loaded += DischargeView_Loaded;
        }

        private void DischargeView_Loaded(object sender, RoutedEventArgs e)
        {
            RenderInvoices();
        }

        private void RenderInvoices()
        {
            var invoices = _currentPatient is { } patient
                ? _dataService.GetInvoicesForPatient(patient.UserId).ToList()
                : _dataService.GetOutstandingInvoices().Take(3).ToList();

            PatientSummaryTextBlock.Text = _currentPatient is { } patientAccount
                ? $"Invoices for {patientAccount.DisplayName}"
                : "Preview of recent discharge invoices";

            if (invoices.Count == 0)
            {
                InvoicesItemsControl.ItemsSource = null;
                InvoicesItemsControl.Visibility = Visibility.Collapsed;
                EmptyStateBorder.Visibility = Visibility.Visible;
                return;
            }

            EmptyStateBorder.Visibility = Visibility.Collapsed;
            InvoicesItemsControl.Visibility = Visibility.Visible;
            InvoicesItemsControl.ItemsSource = invoices;
        }

        private void DownloadInvoiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { DataContext: BillingRecord invoice })
            {
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Save Invoice as PDF",
                FileName = $"Invoice_{invoice.InvoiceId}.pdf",
                Filter = "PDF files (*.pdf)|*.pdf"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                _pdfExport.ExportInvoice(invoice, dialog.FileName);
                MessageBox.Show("Invoice exported successfully.", "Export complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to export invoice. {ex.Message}", "Export failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
