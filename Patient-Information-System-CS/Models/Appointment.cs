using System;

namespace Patient_Information_System_CS.Models
{
    public sealed class Appointment
    {
        public int AppointmentId { get; set; }
        public int? PatientId { get; set; }
        public int? DoctorId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public DateTime ScheduledFor { get; set; } = DateTime.Today;
        public string Description { get; set; } = string.Empty;
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        public string StatusDisplay => Status switch
        {
            AppointmentStatus.Pending => "Pending",
            AppointmentStatus.Accepted => "Accepted",
            AppointmentStatus.Completed => "Completed",
            AppointmentStatus.Rejected => "Rejected",
            _ => Status.ToString()
        };
    }
}
