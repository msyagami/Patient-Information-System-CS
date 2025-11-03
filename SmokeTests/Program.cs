using System;
using System.Linq;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Patient_Information_System_CS.Configuration;
using Patient_Information_System_CS.Data;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Models.Entities;
using Patient_Information_System_CS.Services;

Console.WriteLine("=== HospitalDataService Smoke Test ===");

try
{
    var service = HospitalDataService.Instance;

    var baselineAccounts = service.GetAllAccounts().Count();
    var baselineAppointments = service.GetAllAppointments().Count();
    Console.WriteLine($"Baseline accounts: {baselineAccounts}");
    Console.WriteLine($"Baseline appointments: {baselineAppointments}");

    using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

    EnsureSeedData();

    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
    var patientName = $"Smoke Test Patient {timestamp}";
    var patientAccount = service.CreatePatientAccount(
        fullName: patientName,
        email: string.Empty,
        contactNumber: "+63 912 000 0000",
        address: "Smoke Test Ward",
        dateOfBirth: DateTime.Today.AddYears(-30),
        approve: true,
        currentlyAdmitted: true,
        assignedDoctorUserId: null,
        insuranceProvider: "SmokeHealth",
        emergencyContact: "Smoke Contact",
        roomAssignment: "Room 101",
        admitDateOverride: DateTime.Today.AddHours(-1));

    Console.WriteLine($"Created patient account #{patientAccount.UserId} ({patientAccount.DisplayName})");

    var admissions = service.GetCurrentAdmissions().ToList();
    var admissionFound = admissions.Any(a => a.UserId == patientAccount.UserId);
    Console.WriteLine($"Admission listing includes patient: {admissionFound}");

    var appointment = service.ScheduleAppointment(
        patientId: null,
        doctorId: null,
        scheduledFor: DateTime.Now.AddHours(4),
        description: "Smoke test appointment");

    Console.WriteLine($"Scheduled appointment #{appointment.AppointmentId} for doctor {appointment.DoctorId} / patient {appointment.PatientId}");

    service.AcceptAppointment(appointment);
    Console.WriteLine("Accepted appointment");

    service.CompleteAppointment(appointment);
    Console.WriteLine("Completed appointment");

    var outstandingInvoices = service.GetOutstandingInvoices().ToList();
    var invoiceForPatient = outstandingInvoices.FirstOrDefault(i => i.PatientId == appointment.PatientId);
    Console.WriteLine(invoiceForPatient is null
        ? "No outstanding invoices for smoke appointment (expected if approved)."
        : $"Found outstanding invoice #{invoiceForPatient.InvoiceId} for smoke appointment patient.");

    var updatedAccounts = service.GetAllAccounts().Count();
    var updatedAppointments = service.GetAllAppointments().Count();
    Console.WriteLine($"Accounts observed post-admission: {updatedAccounts}");
    Console.WriteLine($"Appointments observed post-scheduling: {updatedAppointments}");

    // Dispose without scope.Complete() to roll back changes
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Smoke test failed:");
    Console.WriteLine(ex);
    Console.ResetColor();
    Environment.ExitCode = 1;
    return;
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Smoke test completed successfully.");
Console.ResetColor();

return;

static void EnsureSeedData()
{
    using (var context = CreateDbContext())
    {
        if (!context.Rooms.Any())
        {
            context.Rooms.Add(new Room
            {
                RoomId = 9901,
                RoomNumber = 101,
                RoomType = "General Ward",
                Capacity = 4
            });
            context.SaveChanges();
        }
    }

    using (var context = CreateDbContext())
    {
        if (!context.Doctors.Any())
        {
            var doctorPerson = new Person
            {
                PersonId = 998001,
                GivenName = "Smoke",
                LastName = "Doctor",
                MiddleName = "Test",
                Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-40)),
                Sex = "U",
                ContactNumber = 639120000001,
                Address = "Smoke Doctor Residence",
                EmergencyContact = "Smoke Contact",
                RelationshipToEmergencyContact = "Colleague",
                Email = "smoke.doctor@example.com",
                Nationality = "Smoke"
            };

            context.People.Add(doctorPerson);
            context.SaveChanges();

            context.Doctors.Add(new Doctor
            {
                DoctorId = 997001,
                DoctorIdNumber = "DOC-SMOKE",
                LicenseNumber = "LIC-SMOKE",
                Department = "General Medicine",
                Specialization = "General",
                EmploymentDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-5)),
                RegularStaff = true,
                Salary = 0m,
                PersonId = doctorPerson.PersonId,
                ApprovalStatus = 1
            });

            context.SaveChanges();
        }
    }

    using (var context = CreateDbContext())
    {
        if (!context.Nurses.Any())
        {
            var nursePerson = new Person
            {
                PersonId = 998002,
                GivenName = "Smoke",
                LastName = "Nurse",
                MiddleName = "Test",
                Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-35)),
                Sex = "U",
                ContactNumber = 639120000002,
                Address = "Smoke Nurse Residence",
                EmergencyContact = "Smoke Contact",
                RelationshipToEmergencyContact = "Colleague",
                Email = "smoke.nurse@example.com",
                Nationality = "Smoke"
            };

            context.People.Add(nursePerson);
            context.SaveChanges();

            context.Nurses.Add(new Nurse
            {
                NurseId = 996001,
                NurseIdNumber = "NUR-SMOKE",
                LicenseNumber = "NUR-LIC-SMOKE",
                Department = "General Medicine",
                Specialization = "General",
                EmploymentDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-3)),
                RegularStaff = true,
                Salary = 0m,
                PersonId = nursePerson.PersonId
            });

            context.SaveChanges();
        }
    }
}

static HospitalDbContext CreateDbContext()
{
    var connectionString = AppConfiguration.GetConnectionString("HospitalContext");
    var options = new DbContextOptionsBuilder<HospitalDbContext>()
        .UseSqlServer(connectionString)
        .Options;

    return new HospitalDbContext(options);
}
