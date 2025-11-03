using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Patient_Information_System_CS.Models;

namespace Patient_Information_System_CS.Views.Common;

public partial class MedicalRecordEditorDialog : Window
{
    private readonly MedicalRecordRequest _request;
    private readonly IReadOnlyList<UserAccount> _patients;
    private readonly IReadOnlyList<UserAccount> _doctors;
    private readonly int? _patientScope;
    private readonly int? _doctorScope;

    public MedicalRecordEditorDialog(
        MedicalRecordRequest request,
        IEnumerable<UserAccount> patients,
        IEnumerable<UserAccount> doctors,
        int? patientScope,
        int? doctorScope)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
        _patients = patients?.OrderBy(p => p.DisplayName).ToList()
            ?? throw new ArgumentNullException(nameof(patients));
        _doctors = doctors?.OrderBy(d => d.DisplayName).ToList()
            ?? throw new ArgumentNullException(nameof(doctors));
        _patientScope = patientScope;
        _doctorScope = doctorScope;

        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PopulatePatientOptions();
        PopulateDoctorOptions();

        RecordDatePicker.SelectedDate = _request.RecordDate == default
            ? DateTime.Today
            : _request.RecordDate;

        DiagnosisTextBox.Text = _request.Diagnosis;
        TreatmentTextBox.Text = _request.Treatment;
        PrescriptionsTextBox.Text = _request.Prescriptions;

        UpdateSaveButtonState();
    }

    private void PopulatePatientOptions()
    {
        PatientComboBox.ItemsSource = _patients
            .Select(account => new ComboOption(account.UserId, account.DisplayName))
            .ToList();

        if (_patientScope.HasValue)
        {
            PatientComboBox.SelectedValue = _patientScope.Value;
            PatientComboBox.IsEnabled = false;
        }
        else if (_patients.Count == 1)
        {
            PatientComboBox.SelectedIndex = 0;
        }
        else if (_request.PatientIdentifier > 0)
        {
            PatientComboBox.SelectedValue = _request.PatientIdentifier;
        }
    }

    private void PopulateDoctorOptions()
    {
        DoctorComboBox.ItemsSource = _doctors
            .Select(account => new ComboOption(account.UserId, account.DisplayName))
            .ToList();

        if (_doctorScope.HasValue)
        {
            DoctorComboBox.SelectedValue = _doctorScope.Value;
            DoctorComboBox.IsEnabled = false;
        }
        else if (_doctors.Count == 1)
        {
            DoctorComboBox.SelectedIndex = 0;
        }
        else if (_request.DoctorIdentifier > 0)
        {
            DoctorComboBox.SelectedValue = _request.DoctorIdentifier;
        }
    }

    private void UpdateSaveButtonState()
    {
        SaveButton.IsEnabled =
            PatientComboBox.SelectedValue is int &&
            DoctorComboBox.SelectedValue is int &&
            !string.IsNullOrWhiteSpace(DiagnosisTextBox.Text) &&
            !string.IsNullOrWhiteSpace(TreatmentTextBox.Text) &&
            RecordDatePicker.SelectedDate.HasValue;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (PatientComboBox.SelectedValue is not int patientId ||
            DoctorComboBox.SelectedValue is not int doctorId)
        {
            MessageBox.Show(this, "Please select both a patient and a doctor before saving.", "Missing Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var recordDate = RecordDatePicker.SelectedDate ?? DateTime.Today;

        _request.PatientIdentifier = patientId;
        _request.DoctorIdentifier = doctorId;
        _request.RecordDate = recordDate;
        _request.Diagnosis = DiagnosisTextBox.Text.Trim();
        _request.Treatment = TreatmentTextBox.Text.Trim();
        _request.Prescriptions = PrescriptionsTextBox.Text.Trim();

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnFieldChanged(object? sender, RoutedEventArgs e)
    {
        UpdateSaveButtonState();
    }

    private sealed record ComboOption(int Key, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }
}
