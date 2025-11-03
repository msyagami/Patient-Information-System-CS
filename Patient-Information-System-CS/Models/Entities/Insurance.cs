using System;
using System.Collections.Generic;

namespace Patient_Information_System_CS.Models.Entities;

public partial class Insurance
{
    public int InsuranceId { get; set; }

    public string PolicyNumber { get; set; } = null!;

    public string ProviderName { get; set; } = null!;

    public string CoverageType { get; set; } = null!;

    public decimal? CoverageAmount { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public int AssignedPatientId { get; set; }

    public virtual Patient AssignedPatient { get; set; } = null!;
}
