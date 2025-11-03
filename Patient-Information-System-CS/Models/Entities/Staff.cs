using System;
using System.Collections.Generic;

namespace Patient_Information_System_CS.Models.Entities;

public partial class Staff
{
    public int StaffId { get; set; }

    public string StaffIdNumber { get; set; } = null!;

    public string Department { get; set; } = null!;

    public DateOnly EmploymentDate { get; set; }

    public bool RegularStaff { get; set; }

    public int? SupervisorId { get; set; }

    public decimal Salary { get; set; }

    public int PersonId { get; set; }

    public virtual Person Person { get; set; } = null!;
}
