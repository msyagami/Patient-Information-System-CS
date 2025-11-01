using System;

namespace Patient_Information_System_CS.Models
{
    public sealed class BillingRecord
    {
        public int InvoiceId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int? DoctorId { get; set; }
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

        public int Total => RoomCharge + DoctorFee + MedicineCost + OtherCharge;
        public string StatusDisplay => IsPaid ? "Paid" : "Pending";
    }
}
