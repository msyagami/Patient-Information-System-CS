using System;
using System.Collections.Generic;

namespace Patient_Information_System_CS.Models.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public int AssociatedPersonId { get; set; }

    public string UserRole { get; set; } = null!;

    public virtual Person AssociatedPerson { get; set; } = null!;
}
