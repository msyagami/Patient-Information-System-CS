using System;
using System.Linq;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Models.Entities;

namespace Patient_Information_System_CS.Services
{
    public sealed partial class HospitalDataService
    {
        private void EnsureReferenceData()
        {
            using var context = CreateContext(tracking: true);

            if (!context.Rooms.Any())
            {
                var roomId = NextRoomId(context);
                context.Rooms.AddRange(
                    new Room { RoomId = roomId++, RoomNumber = 101, RoomType = "General Ward", Capacity = 4 },
                    new Room { RoomId = roomId++, RoomNumber = 102, RoomType = "General Ward", Capacity = 4 },
                    new Room { RoomId = roomId++, RoomNumber = 201, RoomType = "ICU", Capacity = 2 },
                    new Room { RoomId = roomId, RoomNumber = 301, RoomType = "Private", Capacity = 1 }
                );
                context.SaveChanges();
            }

            if (!context.Departments.Any())
            {
                var departmentId = NextDepartmentId(context);
                context.Departments.AddRange(
                    new Department { DepartmentId = departmentId++, DepartmentName = "General Medicine", Description = "Primary care and general services" },
                    new Department { DepartmentId = departmentId++, DepartmentName = "Cardiology", Description = "Heart and vascular care" },
                    new Department { DepartmentId = departmentId++, DepartmentName = "Pediatrics", Description = "Child health services" },
                    new Department { DepartmentId = departmentId, DepartmentName = "Emergency", Description = "Emergency and trauma" }
                );
                context.SaveChanges();
            }

            if (!context.Nurses.Any())
            {
                var personId = NextPersonId(context);
                var nurseId = NextNurseId(context);

                var nursePerson = new Person
                {
                    PersonId = personId,
                    GivenName = "Default",
                    LastName = "Nurse",
                    MiddleName = string.Empty,
                    Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-28)),
                    Sex = "F",
                    ContactNumber = "639000000001",
                    Address = "Hospital Campus",
                    EmergencyContact = "Administrator",
                    RelationshipToEmergencyContact = "Supervisor",
                    Email = "nurse.oncall@example.com",
                    Nationality = "PH"
                };

                var nurse = new Nurse
                {
                    NurseId = nurseId,
                    NurseIdNumber = $"NUR-{nurseId:D5}",
                    LicenseNumber = $"NUR-LIC-{nurseId:D5}",
                    Department = "General Medicine",
                    Specialization = "General",
                    EmploymentDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-1)),
                    RegularStaff = true,
                    PersonId = personId
                };

                context.People.Add(nursePerson);
                context.Nurses.Add(nurse);
                context.SaveChanges();
            }

            if (!context.Doctors.Any())
            {
                var personId = NextPersonId(context);
                var doctorId = NextDoctorId(context);

                var doctorPerson = new Person
                {
                    PersonId = personId,
                    GivenName = "On-Call",
                    LastName = "Physician",
                    MiddleName = string.Empty,
                    Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-35)),
                    Sex = "M",
                    ContactNumber = "639000000002",
                    Address = "Hospital Campus",
                    EmergencyContact = "Administrator",
                    RelationshipToEmergencyContact = "Supervisor",
                    Email = "doctor.oncall@example.com",
                    Nationality = "PH"
                };

                var doctor = new Doctor
                {
                    DoctorId = doctorId,
                    DoctorIdNumber = $"DOC-{doctorId:D5}",
                    LicenseNumber = $"LIC-{doctorId:D5}",
                    Department = "General Medicine",
                    Specialization = "General",
                    EmploymentDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-3)),
                    RegularStaff = true,
                    ApprovalStatus = MapDoctorStatusToByte(DoctorStatus.Available),
                    PersonId = personId
                };

                context.People.Add(doctorPerson);
                context.Doctors.Add(doctor);
                context.SaveChanges();
            }
        }
    }
}
