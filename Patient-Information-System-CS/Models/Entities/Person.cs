using System;
using System.Collections.Generic;

namespace Patient_Information_System_CS.Models.Entities;

public partial class Person
{
    public int PersonId { get; set; }

    public string LastName { get; set; } = null!;

    public string GivenName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string? Suffix { get; set; }

    public DateOnly Birthdate { get; set; }

    public string Sex { get; set; } = null!;

    public long ContactNumber { get; set; }

    public string Address { get; set; } = null!;

    public string EmergencyContact { get; set; } = null!;

    public string RelationshipToEmergencyContact { get; set; } = null!;

    public string? Email { get; set; }

    public string Nationality { get; set; } = null!;

    public virtual ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();

    public virtual ICollection<Nurse> Nurses { get; set; } = new List<Nurse>();

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();

    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
