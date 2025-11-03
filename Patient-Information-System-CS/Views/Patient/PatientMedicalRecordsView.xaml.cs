using System.Windows.Controls;
using Patient_Information_System_CS.Models;

namespace Patient_Information_System_CS.Views.Patient;

public partial class PatientMedicalRecordsView : UserControl
{
    private readonly UserAccount? _patientAccount;
    private bool _isInitialized;

    public PatientMedicalRecordsView(UserAccount? patientAccount)
    {
        _patientAccount = patientAccount;
        InitializeComponent();
        Loaded += (_, _) => EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        var patient = _patientAccount ?? new UserAccount
        {
            UserId = 0,
            Role = UserRole.Patient,
            DisplayName = "Patient"
        };

        MedicalRecords.InitializeForPatient(patient);
        _isInitialized = true;
    }
}
