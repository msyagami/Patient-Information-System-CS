using System;
using System.Linq;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Services;
using Xunit;
using EntityAppointment = Patient_Information_System_CS.Models.Entities.Appointment;
using EntityBill = Patient_Information_System_CS.Models.Entities.Bill;
using EntityDoctor = Patient_Information_System_CS.Models.Entities.Doctor;
using EntityInsurance = Patient_Information_System_CS.Models.Entities.Insurance;
using EntityNurse = Patient_Information_System_CS.Models.Entities.Nurse;
using EntityPatient = Patient_Information_System_CS.Models.Entities.Patient;
using EntityPerson = Patient_Information_System_CS.Models.Entities.Person;
using EntityRoom = Patient_Information_System_CS.Models.Entities.Room;
using EntityUser = Patient_Information_System_CS.Models.Entities.User;

namespace Patient_Information_System_CS.Tests.Services;

public class MappingTests
{
    [Fact]
    public void MapUserAccount_WithOutstandingBill_MapsPatientProfileDetails()
    {
        var doctorPerson = BuildPerson(200, "Ada", "Lovelace", "F", 639888777111);
        var doctor = BuildDoctor(501, doctorPerson, "Cardiology");

        var nurse = BuildNurse(601);
        var room = new EntityRoom
        {
            RoomId = 301,
            RoomNumber = 42,
            RoomType = "ICU",
            Capacity = 1
        };

        var patientPerson = BuildPerson(100, "Grace", "Hopper", "F", 639111222333);
        var patient = BuildPatient(701, patientPerson, doctor, room, nurse);
        patient.Insurances.Add(new EntityInsurance
        {
            InsuranceId = 9100,
            AssignedPatientId = patient.PatientId,
            ProviderName = "PhilHealth",
            PolicyNumber = "PH-123"
        });

        var bill = new EntityBill
        {
            BillId = patient.BillId,
            BillIdNumber = "BILL-9001",
            AssignedPatientId = patient.PatientId,
            Amount = 2500.75m,
            Description = "Room Charges",
            DateBilled = new DateTime(2025, 11, 2),
            Status = 0,
            AssignedPatient = patient
        };
        patient.Bills.Add(bill);

        var user = new EntityUser
        {
            UserId = 400,
            Username = "grace",
            Password = "encoded",
            UserRole = "patient",
            AssociatedPersonId = patientPerson.PersonId,
            AssociatedPerson = patientPerson
        };
        patientPerson.Users.Add(user);

        var lookup = new[] { bill }.ToLookup(b => b.AssignedPatientId);

        var account = HospitalDataService.MapUserAccount(user, lookup);

        var profile = account.PatientProfile;
        Assert.NotNull(profile);
        Assert.Equal(UserRole.Patient, account.Role);
        Assert.True(profile!.HasUnpaidBills);
        Assert.Equal("P-00701", profile.PatientNumber);
        Assert.Equal("Room 42 - ICU", profile.RoomAssignment);
        Assert.Equal("PhilHealth (PH-123)", profile.InsuranceProvider);
        Assert.Equal("Ada Lovelace", profile.AssignedDoctorName);
        Assert.Equal(new DateTime(2025, 11, 1), profile.AdmitDate);
        Assert.Equal("639111222333", profile.ContactNumber);
        Assert.True(profile.IsCurrentlyAdmitted);
        Assert.True(account.IsActive);
        Assert.Equal(patientPerson.Birthdate.ToDateTime(TimeOnly.MinValue), profile.DateOfBirth);
    }

    [Fact]
    public void MapAppointment_PopulatesNamesAndFallsBackToMedicalRecordsText()
    {
        var doctorPerson = BuildPerson(400, "Meredith", "Grey", "F", 639777000111);
        var doctor = BuildDoctor(1100, doctorPerson, "Surgery");

        var nurse = BuildNurse(1200);
        var room = new EntityRoom
        {
            RoomId = 3200,
            RoomNumber = 12,
            RoomType = "ER",
            Capacity = 1
        };

        var patientPerson = BuildPerson(500, "Cristina", "Yang", "F", 639555123456);
        var patient = BuildPatient(1500, patientPerson, doctor, room, nurse);
        patient.PatientIdNumber = "PAT-1500";

        var appointment = new EntityAppointment
        {
            AppointmentId = 2000,
            AppointmentIdNumber = "APT-2000",
            AssignedDoctorId = doctor.DoctorId,
            AssignedDoctor = doctor,
            AssignedPatientId = patient.PatientId,
            AssignedPatient = patient,
            AppointmentSchedule = new DateTime(2025, 11, 10, 9, 0, 0),
            AppointmentPurpose = string.Empty,
            MedicalRecordsText = "Post-surgery follow up",
            AppointmentStatus = 2
        };

        var mapped = HospitalDataService.MapAppointment(appointment);

        Assert.Equal("Cristina Yang", mapped.PatientName);
        Assert.Equal("Meredith Grey", mapped.DoctorName);
        Assert.Equal("Post-surgery follow up", mapped.Description);
        Assert.Equal(AppointmentStatus.Completed, mapped.Status);
        Assert.Equal(patient.PatientId, mapped.PatientId);
        Assert.Equal(doctor.DoctorId, mapped.DoctorId);
        Assert.Equal(appointment.AppointmentSchedule, mapped.ScheduledFor);
    }

    [Fact]
    public void MapBill_UsesContactInfoAndOutstandingStatus()
    {
        var doctorPerson = BuildPerson(600, "Henry", "Jones", "M", 639444222111);
        var doctor = BuildDoctor(1600, doctorPerson, "General Medicine");

        var nurse = BuildNurse(1700);
        var room = new EntityRoom
        {
            RoomId = 3600,
            RoomNumber = 8,
            RoomType = "Ward",
            Capacity = 2
        };

        var patientPerson = BuildPerson(700, "Indiana", "Jones", "M", 639555000111);
        var patient = BuildPatient(1800, patientPerson, doctor, room, nurse);
        patient.PatientIdNumber = "P-00042";

        var bill = new EntityBill
        {
            BillId = 2200,
            BillIdNumber = "BILL-2200",
            AssignedPatientId = patient.PatientId,
            AssignedPatient = patient,
            Amount = 1999.6m,
            Description = "Hospital stay",
            DateBilled = new DateTime(2025, 11, 5),
            Status = 0
        };

        var mapped = HospitalDataService.MapBill(bill);

        Assert.Equal(bill.BillId, mapped.InvoiceId);
        Assert.Equal(patient.PatientId, mapped.PatientId);
        Assert.Equal("Indiana Jones", mapped.PatientName);
        Assert.Equal("Henry Jones", mapped.DoctorName);
        Assert.Equal("639555000111", mapped.ContactNumber);
        Assert.False(mapped.IsPaid);
        Assert.Null(mapped.PaidDate);
        Assert.Equal(2000, mapped.RoomCharge);
        Assert.Equal(patientPerson.Address, mapped.Address);
    }

    private static EntityPerson BuildPerson(int id, string givenName, string lastName, string sex, long contactNumber)
    {
        return new EntityPerson
        {
            PersonId = id,
            GivenName = givenName,
            LastName = lastName,
            MiddleName = null,
            Suffix = null,
            Birthdate = DateOnly.FromDateTime(new DateTime(1980, 1, 1)),
            Sex = sex,
            ContactNumber = contactNumber,
            Address = $"{lastName} Residence",
            EmergencyContact = "Primary Contact",
            RelationshipToEmergencyContact = "Sibling",
            Email = $"{givenName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}@example.com",
            Nationality = "Testland"
        };
    }

    private static EntityDoctor BuildDoctor(int id, EntityPerson person, string department)
    {
        var doctor = new EntityDoctor
        {
            DoctorId = id,
            DoctorIdNumber = $"DOC-{id}",
            LicenseNumber = $"LIC-{id}",
            Department = department,
            Specialization = department,
            EmploymentDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-5)),
            RegularStaff = true,
            ResidencyDate = null,
            SupervisorId = null,
            Salary = 0m,
            PersonId = person.PersonId,
            Person = person,
            ApprovalStatus = 1
        };
        person.Doctors.Add(doctor);
        return doctor;
    }

    private static EntityNurse BuildNurse(int id)
    {
        var person = BuildPerson(id + 10000, $"Nora{id}", "Nurse", "F", 639111000000 + id);
        var nurse = new EntityNurse
        {
            NurseId = id,
            NurseIdNumber = $"NUR-{id}",
            LicenseNumber = $"NUR-LIC-{id}",
            Department = "General Medicine",
            Specialization = "General",
            EmploymentDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-2)),
            RegularStaff = true,
            ResidencyDate = null,
            SupervisorId = null,
            Salary = 0m,
            PersonId = person.PersonId,
            Person = person
        };
        person.Nurses.Add(nurse);
        return nurse;
    }

    private static EntityPatient BuildPatient(int patientId, EntityPerson person, EntityDoctor doctor, EntityRoom room, EntityNurse nurse)
    {
        var patient = new EntityPatient
        {
            PatientId = patientId,
            PersonId = person.PersonId,
            AssignedDoctorId = doctor.DoctorId,
            AssignedNurseId = nurse.NurseId,
            PatientIdNumber = string.Empty,
            Allergens = string.Empty,
            BloodType = "UNK",
            DateAdmitted = new DateTime(2025, 11, 1),
            DateDischarged = null,
            MedicalHistory = "Hypertension",
            CurrentMedications = null,
            RoomId = room.RoomId,
            MedicalRecords = null,
            BillId = patientId + 9000,
            Status = 1,
            AssignedDoctor = doctor,
            AssignedNurse = nurse,
            Person = person,
            Room = room
        };

        person.Patients.Add(patient);
        doctor.Patients.Add(patient);
        nurse.Patients.Add(patient);
        room.Patients.Add(patient);
        return patient;
    }
}
