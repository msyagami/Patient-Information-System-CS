using System.Linq;
using System.Windows.Controls;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;

namespace Patient_Information_System_CS.Views.Staff;

public partial class ReceptionistMedicalRecordsView : UserControl
{
    private readonly HospitalDataService _dataService = HospitalDataService.Instance;
    private bool _isInitialized;

    public ReceptionistMedicalRecordsView(UserAccount? currentUser)
    {
        InitializeComponent();
        Loaded += (_, _) => EnsureInitialized(currentUser);
    }

    private void EnsureInitialized(UserAccount? currentUser)
    {
        if (_isInitialized)
        {
            return;
        }

        var user = currentUser ?? new UserAccount
        {
            UserId = 0,
            Role = UserRole.Staff,
            DisplayName = "Staff"
        };

        var patients = _dataService.GetAllPatients().ToList();
        var doctors = _dataService.GetAllDoctors().ToList();

        MedicalRecords.InitializeForStaff(user, patients, doctors);
        _isInitialized = true;
    }
}
