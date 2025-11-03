using System;
using System.Collections.Generic;

namespace Patient_Information_System_CS.Models.Entities;

public partial class AppointmentInvoiceLink
{
    public int LinkId { get; set; }

    public int AppointmentId { get; set; }

    public int BillId { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual Bill Bill { get; set; } = null!;
}
