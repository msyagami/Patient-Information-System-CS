using System;
using System.Collections.Generic;

namespace Patient_Information_System_CS.Models.Entities;

public partial class BillingRecord
{
    public int InvoiceId { get; set; }

    public int PatientId { get; set; }

    public string PatientName { get; set; } = null!;

    public string ContactNumber { get; set; } = null!;

    public string Address { get; set; } = null!;

    public int? DoctorId { get; set; }

    public string DoctorName { get; set; } = null!;

    public DateTime AdmitDate { get; set; }

    public DateTime ReleaseDate { get; set; }

    public int DaysStayed { get; set; }

    public int RoomCharge { get; set; }

    public int DoctorFee { get; set; }

    public int MedicineCost { get; set; }

    public int OtherCharge { get; set; }

    public bool IsPaid { get; set; }

    public DateTime? PaidDate { get; set; }
}
