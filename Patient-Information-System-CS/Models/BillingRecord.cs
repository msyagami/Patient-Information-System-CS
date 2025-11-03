using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Patient_Information_System_CS.Models
{
    public class BillingRecord
    {
        [Key]
        public int InvoiceId { get; set; }
        public int PatientId { get; set; }
        [MaxLength(128)]
        public string PatientName { get; set; } = string.Empty;
        [MaxLength(32)]
        public string ContactNumber { get; set; } = string.Empty;
        [MaxLength(256)]
        public string Address { get; set; } = string.Empty;
        public int? DoctorId { get; set; }
        [MaxLength(128)]
        public string DoctorName { get; set; } = string.Empty;
        public DateTime AdmitDate { get; set; } = DateTime.Today;
        public DateTime ReleaseDate { get; set; } = DateTime.Today;
        public int DaysStayed { get; set; }
        public int RoomCharge { get; set; }
        public int DoctorFee { get; set; }
        public int MedicineCost { get; set; }
        public int OtherCharge { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? PaidDate { get; set; }

        [NotMapped]
        public int Total => RoomCharge + DoctorFee + MedicineCost + OtherCharge;
        [NotMapped]
        public string StatusDisplay => IsPaid ? "Paid" : "Pending";
    }
}
