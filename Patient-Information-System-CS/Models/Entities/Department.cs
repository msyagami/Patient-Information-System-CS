using System;
using System.Collections.Generic;

namespace Patient_Information_System_CS.Models.Entities;

public partial class Department
{
    public int DepartmentId { get; set; }

    public string DepartmentName { get; set; } = null!;

    public string? Description { get; set; }
}
