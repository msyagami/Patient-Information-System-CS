using System;
using System.Collections.Generic;

namespace Patient_Information_System_CS.Models.Entities;

public partial class Bill
{
    public int BillId { get; set; }

    public string BillIdNumber { get; set; } = null!;

    public int AssignedPatientId { get; set; }

    public decimal Amount { get; set; }

    public string Description { get; set; } = null!;

    public DateTime DateBilled { get; set; }

    public byte Status { get; set; }

    public string? PaymentMethod { get; set; }

    public virtual ICollection<AppointmentInvoiceLink> AppointmentInvoiceLinks { get; set; } = new List<AppointmentInvoiceLink>();

    public virtual Patient AssignedPatient { get; set; } = null!;
}
