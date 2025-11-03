using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Patient_Information_System_CS.Models
{
    public class UserAccount
    {
        [Key]
        public int UserId { get; set; }
        [MaxLength(64)]
        public string Username { get; set; } = string.Empty;
        [MaxLength(128)]
        public string Password { get; set; } = string.Empty;
        [MaxLength(64)]
        public string EmergencyRelationship { get; set; } = string.Empty;
        [MaxLength(128)]
        public string DisplayName { get; set; } = string.Empty;
        [MaxLength(32)]
        public string Nationality { get; set; } = string.Empty;
        [MaxLength(16)]
        public string Sex { get; set; } = string.Empty;
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsSuperUser { get; set; }
        public AdminProfile? AdminProfile { get; set; }
        public StaffProfile? StaffProfile { get; set; }
        public DoctorProfile? DoctorProfile { get; set; }
        public NurseProfile? NurseProfile { get; set; }
        public PatientProfile? PatientProfile { get; set; }

        [NotMapped]
        public string PrimaryContactNumber =>
            AdminProfile?.ContactNumber
            ?? StaffProfile?.ContactNumber
            ?? DoctorProfile?.ContactNumber
            ?? NurseProfile?.ContactNumber
            ?? PatientProfile?.ContactNumber
            ?? string.Empty;

        [NotMapped]
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
            var decodedPassword = DecodePassword(Password);
            return decodedPassword.AsSpan().SequenceEqual(password);
        }

        public void SetPlainTextPassword(string plainText)
        {
            Password = EncodePassword(plainText);
        }

        public string GetPlainTextPassword() => DecodePassword(Password);

        private static string EncodePassword(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }

            var bytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(bytes);
        }

        private static string DecodePassword(string encodedPassword)
        {
            if (string.IsNullOrEmpty(encodedPassword))
            {
                return string.Empty;
            }

            try
            {
                var bytes = Convert.FromBase64String(encodedPassword);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                // fallback for legacy plaintext entries
                return encodedPassword;
            }
        }
    }

    public class AdminProfile
    {
        public bool IsApproved { get; set; }
        [MaxLength(32)]
        public string ContactNumber { get; set; } = string.Empty;
    }

    public class StaffProfile
    {
        public bool IsApproved { get; set; }
        [MaxLength(32)]
        public string ContactNumber { get; set; } = string.Empty;
        public bool HasCompletedOnboarding { get; set; }
    }

    public class DoctorProfile
    {
        public int DoctorId { get; set; }
        public DoctorStatus Status { get; set; } = DoctorStatus.OnHold;
        [MaxLength(64)]
        public string Department { get; set; } = string.Empty;
        [MaxLength(32)]
        public string ContactNumber { get; set; } = string.Empty;
        [MaxLength(64)]
        public string LicenseNumber { get; set; } = string.Empty;
        [MaxLength(32)]
        public string DoctorNumber { get; set; } = string.Empty;
        [MaxLength(256)]
        public string Address { get; set; } = string.Empty;
        public DateTime ApplicationDate { get; set; } = DateTime.Today;
    }

    public class NurseProfile
    {
        public int NurseId { get; set; }
        public NurseStatus Status { get; set; } = NurseStatus.OnHold;
        [MaxLength(64)]
        public string Department { get; set; } = string.Empty;
        [MaxLength(64)]
        public string Specialization { get; set; } = string.Empty;
        [MaxLength(32)]
        public string ContactNumber { get; set; } = string.Empty;
        [MaxLength(64)]
        public string LicenseNumber { get; set; } = string.Empty;
        [MaxLength(32)]
        public string NurseNumber { get; set; } = string.Empty;
        public DateTime EmploymentDate { get; set; } = DateTime.Today;
        public int AssignedPatientsCount { get; set; }
        [MaxLength(256)]
        public string AssignedPatientsSummary { get; set; } = string.Empty;
    }

    public class PatientProfile
    {
        public bool IsApproved { get; set; }
        public bool HasUnpaidBills { get; set; }
        [MaxLength(32)]
        public string PatientNumber { get; set; } = string.Empty;
        public DateTime? AdmitDate { get; set; }
        [MaxLength(128)]
        public string RoomAssignment { get; set; } = string.Empty;
        [MaxLength(32)]
        public string ContactNumber { get; set; } = string.Empty;
        [MaxLength(256)]
        public string Address { get; set; } = string.Empty;
        [MaxLength(128)]
        public string EmergencyContact { get; set; } = string.Empty;
        [MaxLength(128)]
        public string EmergencyRelationship { get; set; } = "Unknown";
        [MaxLength(128)]
        public string InsuranceProvider { get; set; } = string.Empty;
        public int? AssignedDoctorId { get; set; }
        [MaxLength(128)]
        public string AssignedDoctorName { get; set; } = string.Empty;
        public int? AssignedNurseId { get; set; }
        [MaxLength(128)]
        public string AssignedNurseName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; } = DateTime.Today.AddYears(-30);
        public bool IsCurrentlyAdmitted { get; set; }
        [MaxLength(32)]
        public string Nationality { get; set; } = "Unknown";
        [MaxLength(4)]
        public string Sex { get; set; } = "U";
    }
}
