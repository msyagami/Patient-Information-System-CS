using System;
using System.Collections.Generic;

namespace Patient_Information_System_CS.Models.Entities;

public partial class Room
{
    public int RoomId { get; set; }

    public short RoomNumber { get; set; }

    public string RoomType { get; set; } = null!;

    public byte Capacity { get; set; }

    public int? AssignedPatientId { get; set; }

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
}
