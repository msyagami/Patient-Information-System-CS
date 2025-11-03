using System;
using System.ComponentModel.DataAnnotations;

namespace Patient_Information_System_CS.Models
{
    public sealed class MedicalRecordRequest
    {
        public int PatientIdentifier { get; set; }

        public int DoctorIdentifier { get; set; }

        public DateTime RecordDate { get; set; } = DateTime.Today;

        [Required]
        [MaxLength(500)]
        public string Diagnosis { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Treatment { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Prescriptions { get; set; } = string.Empty;

        public int? CreatedByUserId { get; set; }
    }
}
