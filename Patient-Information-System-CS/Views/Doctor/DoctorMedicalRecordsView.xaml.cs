using System.Linq;
using System.Windows.Controls;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;

namespace Patient_Information_System_CS.Views.Doctor;

public partial class DoctorMedicalRecordsView : UserControl
{
    private readonly HospitalDataService _dataService = HospitalDataService.Instance;
    private bool _isInitialized;
    private readonly UserAccount? _doctorAccount;

    public DoctorMedicalRecordsView(UserAccount? doctorAccount)
    {
        _doctorAccount = doctorAccount;
        InitializeComponent();
        Loaded += (_, _) => EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        var doctor = _doctorAccount ?? new UserAccount
        {
            UserId = 0,
            Role = UserRole.Doctor,
            DisplayName = "Doctor"
        };

        var patients = _dataService.GetPatientsForDoctor(doctor.UserId).ToList();

        MedicalRecords.InitializeForDoctor(doctor, patients);
        _isInitialized = true;
    }
}
