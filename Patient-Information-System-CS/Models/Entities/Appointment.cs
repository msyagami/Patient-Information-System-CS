using System;
using System.Collections.Generic;

namespace Patient_Information_System_CS.Models.Entities;

public partial class Appointment
{
    public int AppointmentId { get; set; }

    public string AppointmentIdNumber { get; set; } = null!;

    public int AssignedPatientId { get; set; }

    public int AssignedDoctorId { get; set; }

    public DateTime AppointmentSchedule { get; set; }

    public string AppointmentPurpose { get; set; } = null!;

    public byte AppointmentStatus { get; set; }

    public string? MedicalRecordsText { get; set; }

    public string? DiagnosisText { get; set; }

    public string? TreatmentText { get; set; }

    public string? PrescriptionsText { get; set; }

    public virtual ICollection<AppointmentInvoiceLink> AppointmentInvoiceLinks { get; set; } = new List<AppointmentInvoiceLink>();

    public virtual Doctor AssignedDoctor { get; set; } = null!;

    public virtual Patient AssignedPatient { get; set; } = null!;
}
