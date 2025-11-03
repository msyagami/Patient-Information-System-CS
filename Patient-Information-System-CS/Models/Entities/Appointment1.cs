using System;
using System.Collections.Generic;

namespace Patient_Information_System_CS.Models.Entities;

public partial class Appointment1
{
    public int AppointmentId { get; set; }

    public int? PatientId { get; set; }

    public int? DoctorId { get; set; }

    public string PatientName { get; set; } = null!;

    public string DoctorName { get; set; } = null!;

    public DateTime ScheduledFor { get; set; }

    public string Description { get; set; } = null!;

    public int Status { get; set; }
}
