using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Patient_Information_System_CS.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }
        public int? PatientId { get; set; }
        public int? DoctorId { get; set; }
        [MaxLength(128)]
        public string PatientName { get; set; } = string.Empty;
        [MaxLength(128)]
        public string DoctorName { get; set; } = string.Empty;
        public DateTime ScheduledFor { get; set; } = DateTime.Today;
        [MaxLength(512)]
        public string Description { get; set; } = string.Empty;
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        [NotMapped]
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
