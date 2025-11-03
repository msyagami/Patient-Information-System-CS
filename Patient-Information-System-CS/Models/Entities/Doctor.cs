using System;
using System.Collections.Generic;

namespace Patient_Information_System_CS.Models.Entities;

public partial class Doctor
{
    public int DoctorId { get; set; }

    public string DoctorIdNumber { get; set; } = null!;

    public string LicenseNumber { get; set; } = null!;

    public string Department { get; set; } = null!;

    public string Specialization { get; set; } = null!;

    public DateOnly EmploymentDate { get; set; }

    public bool RegularStaff { get; set; }

    public DateOnly? ResidencyDate { get; set; }

    public string? SupervisorId { get; set; }

    public decimal Salary { get; set; }

    public int PersonId { get; set; }

    public byte ApprovalStatus { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();

    public virtual Person Person { get; set; } = null!;
}
