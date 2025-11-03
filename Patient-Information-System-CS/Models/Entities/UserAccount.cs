using System;
using System.Collections.Generic;

namespace Patient_Information_System_CS.Models.Entities;

public partial class UserAccount
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int Role { get; set; }

    public bool IsActive { get; set; }

    public bool IsSuperUser { get; set; }

    public bool? AdminProfileIsApproved { get; set; }

    public string? AdminProfileContactNumber { get; set; }

    public bool? StaffProfileIsApproved { get; set; }

    public string? StaffProfileContactNumber { get; set; }

    public int? DoctorProfileStatus { get; set; }

    public string? DoctorProfileDepartment { get; set; }

    public string? DoctorProfileContactNumber { get; set; }

    public string? DoctorProfileLicenseNumber { get; set; }

    public string? DoctorProfileAddress { get; set; }

    public DateTime? DoctorProfileApplicationDate { get; set; }

    public bool? PatientProfileIsApproved { get; set; }

    public bool? PatientProfileHasUnpaidBills { get; set; }

    public string? PatientProfilePatientNumber { get; set; }

    public DateTime? PatientProfileAdmitDate { get; set; }

    public string? PatientProfileRoomAssignment { get; set; }

    public string? PatientProfileContactNumber { get; set; }

    public string? PatientProfileAddress { get; set; }

    public string? PatientProfileEmergencyContact { get; set; }

    public string? PatientProfileInsuranceProvider { get; set; }

    public int? PatientProfileAssignedDoctorId { get; set; }

    public string? PatientProfileAssignedDoctorName { get; set; }

    public DateTime? PatientProfileDateOfBirth { get; set; }

    public bool? PatientProfileIsCurrentlyAdmitted { get; set; }
}
