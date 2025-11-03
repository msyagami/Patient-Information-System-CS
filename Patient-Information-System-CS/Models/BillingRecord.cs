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
        public decimal RoomCharge { get; set; }
        public decimal DoctorFee { get; set; }
        public decimal MedicineCost { get; set; }
        public decimal OtherCharge { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? PaidDate { get; set; }
        [MaxLength(256)]
        public string Notes { get; set; } = string.Empty;

        [NotMapped]
        public decimal TotalAmount => RoomCharge + DoctorFee + MedicineCost + OtherCharge;

        [NotMapped]
        public decimal Total => TotalAmount;

        [NotMapped]
        public string StatusDisplay => IsPaid ? "Paid" : "Pending";
    }
}
