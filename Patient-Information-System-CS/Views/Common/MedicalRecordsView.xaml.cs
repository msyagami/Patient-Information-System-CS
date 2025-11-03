using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;

namespace Patient_Information_System_CS.Views.Common;

public partial class MedicalRecordsView : UserControl
{
    private readonly HospitalDataService _dataService;
    private IEnumerable<MedicalRecordEntry> _records = Enumerable.Empty<MedicalRecordEntry>();
    private MedicalRecordEntry? _selectedRecord;
    private int? _patientFilter;
    private int? _doctorFilter;
    private int? _patientScope;
    private int? _doctorScope;
    private int? _currentUserId;
    private readonly PdfExportService _pdfExport = PdfExportService.Instance;

    public MedicalRecordsView()
    {
        InitializeComponent();
        _dataService = HospitalDataService.Instance;
        _dataService.MedicalRecordsChanged += OnMedicalRecordsChanged;
        Unloaded += OnUnloaded;
    }

    public void InitializeForAdmin(UserAccount currentUser, IEnumerable<UserAccount> patients, IEnumerable<UserAccount> doctors)
    {
        _currentUserId = currentUser.UserId;
        _patientScope = null;
        _doctorScope = null;
        _patientFilter = null;
        _doctorFilter = null;

        TitleTextBlock.Text = "Medical Records";
        SubtitleTextBlock.Text = "Browse all medical records across the organization";
        EnableFilters(patients, doctors);
        SetCreateVisibility(true);
        RefreshRecords();
    }

    public void InitializeForDoctor(UserAccount doctor, IEnumerable<UserAccount> patients)
    {
        _currentUserId = doctor.UserId;
        _doctorScope = doctor.UserId;
        _patientScope = null;
        _doctorFilter = doctor.UserId;
        _patientFilter = null;

        TitleTextBlock.Text = "My Patients' Medical Records";
        SubtitleTextBlock.Text = "Review the medical history for the patients under your care";
        EnableFilters(patients, null);
        SetCreateVisibility(true);
        RefreshRecords();
    }

    public void InitializeForStaff(UserAccount staff, IEnumerable<UserAccount> patients, IEnumerable<UserAccount> doctors)
    {
        _currentUserId = staff.UserId;
        _patientScope = null;
        _doctorScope = null;
        _patientFilter = null;
        _doctorFilter = null;

        TitleTextBlock.Text = "Medical Records";
        SubtitleTextBlock.Text = "Locate and update medical records for patients";
        EnableFilters(patients, doctors);
        SetCreateVisibility(true);
        RefreshRecords();
    }

    public void InitializeForPatient(UserAccount patient)
    {
        _currentUserId = patient.UserId;
        _patientScope = patient.UserId;
        _doctorScope = null;
        _patientFilter = null;
        _doctorFilter = null;

        TitleTextBlock.Text = "My Medical Records";
        SubtitleTextBlock.Text = "Review your medical history and treatment details";
        DisableFilters();
        SetCreateVisibility(false);
        RefreshRecords();
    }

    private void EnableFilters(IEnumerable<UserAccount>? patients, IEnumerable<UserAccount>? doctors)
    {
        if (patients != null)
        {
            PatientFilterContainer.Visibility = Visibility.Visible;
            PatientFilterComboBox.ItemsSource = patients
                .Select(account => new ComboOption(account.UserId, account.DisplayName))
                .Prepend(new ComboOption(null, "All patients"))
                .ToList();
            PatientFilterComboBox.SelectedIndex = 0;
        }
        else
        {
            PatientFilterContainer.Visibility = Visibility.Collapsed;
        }

        if (doctors != null)
        {
            DoctorFilterContainer.Visibility = Visibility.Visible;
            DoctorFilterComboBox.ItemsSource = doctors
                .Select(account => new ComboOption(account.UserId, account.DisplayName))
                .Prepend(new ComboOption(null, "All doctors"))
                .ToList();
            DoctorFilterComboBox.SelectedIndex = 0;
        }
        else
        {
            DoctorFilterContainer.Visibility = Visibility.Collapsed;
        }
    }

    private void DisableFilters()
    {
        PatientFilterContainer.Visibility = Visibility.Collapsed;
        DoctorFilterContainer.Visibility = Visibility.Collapsed;
    }

    private void SetCreateVisibility(bool isVisible)
    {
        AddRecordButton.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshRecords()
    {
        IEnumerable<MedicalRecordEntry> scope = _patientScope.HasValue
            ? _dataService.GetMedicalRecordsForPatient(_patientScope.Value)
            : _doctorScope.HasValue
                ? _dataService.GetMedicalRecordsForDoctor(_doctorScope.Value)
                : _dataService.GetAllMedicalRecords();

        if (_patientFilter.HasValue)
        {
            scope = scope.Where(r => r.PatientUserId == _patientFilter.Value).ToList();
        }

        if (_doctorFilter.HasValue)
        {
            scope = scope.Where(r => r.DoctorUserId == _doctorFilter.Value).ToList();
        }

        _records = scope.ToList();
        RecordsListBox.ItemsSource = _records;

        if (_selectedRecord != null)
        {
            var match = _records.FirstOrDefault(r => r.RecordId == _selectedRecord.RecordId);
            if (match != null)
            {
                RecordsListBox.SelectedItem = match;
                return;
            }
        }

        RecordsListBox.SelectedItem = null;
        SetDetails(null);
    }

    private void SetDetails(MedicalRecordEntry? record)
    {
        _selectedRecord = record;

        if (record == null)
        {
            RecordTitleTextBlock.Text = "Select a record";
            RecordedOnTextBlock.Text = "-";
            PatientTextBlock.Text = "-";
            DoctorTextBlock.Text = "-";
            DiagnosisTextBlock.Text = "-";
            TreatmentTextBlock.Text = "-";
            PrescriptionsTextBlock.Text = "-";
            UpdateExportButtonVisibility(Visibility.Collapsed);
            return;
        }

        RecordTitleTextBlock.Text = record.RecordNumber;
        RecordedOnTextBlock.Text = record.RecordedOnDisplay;
        PatientTextBlock.Text = record.PatientName;
        DoctorTextBlock.Text = record.DoctorName;
        DiagnosisTextBlock.Text = record.Diagnosis;
        TreatmentTextBlock.Text = record.Treatment;
        PrescriptionsTextBlock.Text = record.Prescriptions ?? "-";
        UpdateExportButtonVisibility(Visibility.Visible);
    }

    private void UpdateExportButtonVisibility(Visibility visibility)
    {
        if (FindName("ExportRecordButton") is Button button)
        {
            button.Visibility = visibility;
        }
    }

    private void PatientFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PatientFilterComboBox.SelectedItem is ComboOption option)
        {
            _patientFilter = option.Key;
            RefreshRecords();
        }
    }

    private void DoctorFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DoctorFilterComboBox.SelectedItem is ComboOption option)
        {
            _doctorFilter = option.Key;
            RefreshRecords();
        }
    }

    private void RecordsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RecordsListBox.SelectedItem is MedicalRecordEntry record)
        {
            SetDetails(record);
        }
        else
        {
            SetDetails(null);
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshRecords();
    }

    private void AddRecordButton_Click(object sender, RoutedEventArgs e)
    {
        var request = new MedicalRecordRequest
        {
            PatientIdentifier = _patientScope ?? 0,
            DoctorIdentifier = _doctorScope ?? 0
        };
        request.CreatedByUserId = _currentUserId;

        var dialog = new MedicalRecordEditorDialog(
            request,
            _dataService.GetAllPatients(),
            _dataService.GetAllDoctors(),
            _patientScope,
            _doctorScope);
        if (dialog.ShowDialog() == true)
        {
            try
            {
                _dataService.CreateMedicalRecord(request);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unable to save the medical record. {ex.Message}",
                    "Save Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private void ExportRecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRecord is null)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Save Medical Record as PDF",
            FileName = BuildRecordFileName(_selectedRecord) + ".pdf",
            Filter = "PDF files (*.pdf)|*.pdf"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _pdfExport.ExportMedicalRecord(_selectedRecord, dialog.FileName);
            MessageBox.Show("Medical record exported successfully.", "Export complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to export the medical record. {ex.Message}", "Export failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string BuildRecordFileName(MedicalRecordEntry record)
    {
        var baseName = string.IsNullOrWhiteSpace(record.RecordNumber)
            ? $"MedicalRecord_{record.RecordId}"
            : record.RecordNumber;

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            baseName = baseName.Replace(invalidChar, '_');
        }

        baseName = baseName.Trim('_');
        return string.IsNullOrWhiteSpace(baseName) ? $"MedicalRecord_{record.RecordId}" : baseName;
    }

    private void OnMedicalRecordsChanged(object? sender, EventArgs e)
    {
        RefreshRecords();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _dataService.MedicalRecordsChanged -= OnMedicalRecordsChanged;
    }

    private sealed record ComboOption(int? Key, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }
}
