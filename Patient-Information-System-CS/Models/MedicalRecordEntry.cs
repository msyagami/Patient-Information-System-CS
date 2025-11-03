using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Patient_Information_System_CS.Models
{
    public sealed class MedicalRecordEntry
    {
        [Key]
        public int RecordId { get; set; }

        [MaxLength(50)]
        public string RecordNumber { get; set; } = string.Empty;

        public DateTime RecordedOn { get; set; } = DateTime.Today;

        public int PatientId { get; set; }

        public int PatientUserId { get; set; }

        [MaxLength(128)]
        public string PatientName { get; set; } = string.Empty;

        public int DoctorId { get; set; }

        public int DoctorUserId { get; set; }

        [MaxLength(128)]
        public string DoctorName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Diagnosis { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Treatment { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Prescriptions { get; set; } = string.Empty;

        [NotMapped]
        public string RecordedOnDisplay => RecordedOn.ToString("MMM dd, yyyy");

        [NotMapped]
        public string DiagnosisSummary => string.IsNullOrWhiteSpace(Diagnosis)
            ? "No diagnosis recorded"
            : Diagnosis.Length > 120
                ? Diagnosis[..120] + "..."
                : Diagnosis;

        [NotMapped]
        public bool HasPrescriptions => !string.IsNullOrWhiteSpace(Prescriptions);
    }
}
