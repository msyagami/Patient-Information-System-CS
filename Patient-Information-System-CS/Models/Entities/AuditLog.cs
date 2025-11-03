using System;
using System.Collections.Generic;

namespace Patient_Information_System_CS.Models.Entities;

public partial class AuditLog
{
    public int LogId { get; set; }

    public int UserId { get; set; }

    public string Action { get; set; } = null!;

    public string TableName { get; set; } = null!;

    public int? RecordId { get; set; }

    public DateTime ActionDate { get; set; }

    public string? Details { get; set; }
}
