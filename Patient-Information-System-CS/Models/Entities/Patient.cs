using System;
using System.Collections.Generic;

namespace Patient_Information_System_CS.Models.Entities;

public partial class Patient
{
    public int PatientId { get; set; }

    public int PersonId { get; set; }

    public int AssignedDoctorId { get; set; }

    public int AssignedNurseId { get; set; }

    public string PatientIdNumber { get; set; } = null!;

    public string? Allergens { get; set; }

    public string BloodType { get; set; } = null!;

    public DateTime DateAdmitted { get; set; }

    public DateTime? DateDischarged { get; set; }

    public string MedicalHistory { get; set; } = null!;

    public string? CurrentMedications { get; set; }

    public int RoomId { get; set; }

    public int? MedicalRecords { get; set; }

    public int BillId { get; set; }

    public byte Status { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual Doctor AssignedDoctor { get; set; } = null!;

    public virtual Nurse AssignedNurse { get; set; } = null!;

    public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();

    public virtual ICollection<Insurance> Insurances { get; set; } = new List<Insurance>();

    public virtual ICollection<MedicalRecord> MedicalRecordsNavigation { get; set; } = new List<MedicalRecord>();

    public virtual Person Person { get; set; } = null!;

    public virtual Room Room { get; set; } = null!;
}
