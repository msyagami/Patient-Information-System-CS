using System;
using System.Collections.Generic;

namespace Patient_Information_System_CS.Models.Entities;

public partial class MedicalRecord
{
    public int RecordId { get; set; }

    public string RecordIdNumber { get; set; } = null!;

    public DateOnly RecordDate { get; set; }

    public int AssignedPatientId { get; set; }

    public int AssignedDoctorId { get; set; }

    public string Diagnosis { get; set; } = null!;

    public string Treatment { get; set; } = null!;

    public string Prescriptions { get; set; } = null!;

    public virtual Doctor AssignedDoctor { get; set; } = null!;

    public virtual Patient AssignedPatient { get; set; } = null!;
}
