using System;
using System.Collections.Generic;
using System.Linq;

namespace Patient_Information_System_CS.Models
{
    public sealed class RoomOccupantInfo
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string NurseName { get; set; } = string.Empty;
    }

    public sealed class RoomStatus
    {
        public int RoomId { get; set; }
        public int RoomNumber { get; set; }
        public string RoomType { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public IReadOnlyList<RoomOccupantInfo> Occupants { get; set; } = Array.Empty<RoomOccupantInfo>();

        public int OccupiedCount => Occupants.Count;
        public int AvailableSlots => Math.Max(0, Capacity - OccupiedCount);
        public bool IsAvailable => AvailableSlots > 0;
        public string OccupancyDisplay => $"{OccupiedCount}/{Capacity}";

        public string OccupantSummary => Occupants.Count == 0
            ? "No patients assigned"
            : string.Join(Environment.NewLine, Occupants.Select(o =>
            {
                var doctor = string.IsNullOrWhiteSpace(o.DoctorName) ? null : $"Dr. {o.DoctorName}";
                var nurse = string.IsNullOrWhiteSpace(o.NurseName) ? null : $"Nurse {o.NurseName}";
                var assignments = new[] { doctor, nurse }
                    .Where(label => !string.IsNullOrWhiteSpace(label));

                var assignmentDisplay = string.Join(" / ", assignments);
                return string.IsNullOrWhiteSpace(assignmentDisplay)
                    ? o.PatientName
                    : $"{o.PatientName} ({assignmentDisplay})";
            }));
    }
}
