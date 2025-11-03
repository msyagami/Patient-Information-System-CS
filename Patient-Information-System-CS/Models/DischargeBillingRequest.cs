using System;

namespace Patient_Information_System_CS.Models
{
    public sealed class DischargeBillingRequest
    {
        public int PatientUserId { get; init; }
        public decimal RoomCharge { get; init; }
        public decimal DoctorFee { get; init; }
        public decimal MedicineCost { get; init; }
        public decimal OtherCharges { get; init; }
        public bool MarkAsPaid { get; init; }
        public DateTime? DischargeDate { get; init; }
        public string? Notes { get; init; }

        public decimal TotalAmount => RoomCharge + DoctorFee + MedicineCost + OtherCharges;
    }

    public sealed class ExistingPatientOption
    {
        public int UserId { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public string ContactNumber { get; init; } = string.Empty;
        public string PatientNumber { get; init; } = string.Empty;
        public bool IsCurrentlyAdmitted { get; init; }
    }

    public sealed class ExistingPatientAdmissionRequest
    {
        public int UserId { get; init; }
        public int? AssignedDoctorUserId { get; init; }
        public int? AssignedNurseUserId { get; init; }
        public string RoomAssignment { get; init; } = string.Empty;
        public DateTime? AdmitDateOverride { get; init; }
        public string ContactNumber { get; init; } = string.Empty;
        public string Address { get; init; } = string.Empty;
        public string EmergencyContact { get; init; } = string.Empty;
        public string EmergencyRelationship { get; init; } = string.Empty;
        public string InsuranceProvider { get; init; } = string.Empty;
    }
}
