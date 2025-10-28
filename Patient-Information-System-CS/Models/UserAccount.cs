using System;

namespace Patient_Information_System_CS.Models
{
    public sealed class UserAccount
    {
        public int UserId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public UserRole Role { get; init; }
        public bool IsActive { get; set; } = true;
        public bool IsSuperUser { get; init; }
        public AdminProfile? AdminProfile { get; init; }
        public StaffProfile? StaffProfile { get; init; }
        public DoctorProfile? DoctorProfile { get; init; }
        public PatientProfile? PatientProfile { get; init; }

        public string PrimaryContactNumber =>
            AdminProfile?.ContactNumber
            ?? StaffProfile?.ContactNumber
            ?? DoctorProfile?.ContactNumber
            ?? PatientProfile?.ContactNumber
            ?? string.Empty;

        public string PatientPortalStatus
        {
            get
            {
                if (PatientProfile is null)
                {
                    return string.Empty;
                }

                if (PatientProfile.HasUnpaidBills)
                {
                    return "Awaiting Payment";
                }

                return IsActive ? "Active" : "Inactive";
            }
        }

        public bool PasswordMatches(ReadOnlySpan<char> password)
        {
            return Password.AsSpan().SequenceEqual(password);
        }
    }

    public sealed class AdminProfile
    {
        public bool IsApproved { get; set; }
        public string ContactNumber { get; set; } = string.Empty;
    }

    public sealed class StaffProfile
    {
        public bool IsApproved { get; set; }
        public string ContactNumber { get; set; } = string.Empty;
    }

    public sealed class DoctorProfile
    {
        public DoctorStatus Status { get; set; } = DoctorStatus.OnHold;
        public string Department { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime ApplicationDate { get; set; } = DateTime.Today;
    }

    public sealed class PatientProfile
    {
        public bool IsApproved { get; set; }
        public bool HasUnpaidBills { get; set; }
        public string PatientNumber { get; set; } = string.Empty;
        public DateTime? AdmitDate { get; set; }
        public string RoomAssignment { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string EmergencyContact { get; set; } = string.Empty;
        public string InsuranceProvider { get; set; } = string.Empty;
        public int? AssignedDoctorId { get; set; }
        public string AssignedDoctorName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; } = DateTime.Today.AddYears(-30);
        public bool IsCurrentlyAdmitted { get; set; }
    }
}
