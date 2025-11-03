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
    var baselineInvoices = service.GetOutstandingInvoices().Count();
    Console.WriteLine($"Baseline accounts: {baselineAccounts}");
    Console.WriteLine($"Baseline appointments: {baselineAppointments}");
    Console.WriteLine($"Baseline outstanding invoices: {baselineInvoices}");

    using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

    EnsureSeedData();

    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

    var doctorAccount = service.CreateDoctorAccount(
        fullName: $"Smoke Test Doctor {timestamp}",
        email: string.Empty,
        contactNumber: "+63 912 000 0001",
        department: "General Medicine",
        licenseNumber: string.Empty,
        address: "Smoke Test Clinic",
        status: DoctorStatus.Available,
        birthDate: DateTime.Today.AddYears(-35),
        sex: "M",
        emergencyContact: "Smoke Contact",
        emergencyRelationship: "Colleague",
        nationality: "Smoke");

    Console.WriteLine($"Created doctor account #{doctorAccount.UserId} ({doctorAccount.DisplayName})");

    var nurseAccount = service.CreateNurseAccount(
        fullName: $"Smoke Test Nurse {timestamp}",
        email: string.Empty,
        contactNumber: "+63 912 000 0002",
        department: "General Medicine",
        specialization: "General",
        licenseNumber: string.Empty,
        address: "Smoke Test Residence",
        status: NurseStatus.Available,
        birthDate: DateTime.Today.AddYears(-30),
        sex: "F",
        emergencyContact: "Smoke Contact",
        emergencyRelationship: "Colleague",
        nationality: "Smoke");

    Console.WriteLine($"Created nurse account #{nurseAccount.UserId} ({nurseAccount.DisplayName})");

    var patientName = $"Smoke Test Patient {timestamp}";
    var patientAccount = service.CreatePatientAccount(
        fullName: patientName,
        email: string.Empty,
        contactNumber: "+63 912 000 0003",
        address: "Smoke Test Ward",
        dateOfBirth: DateTime.Today.AddYears(-30),
        approve: true,
        currentlyAdmitted: true,
        assignedDoctorUserId: doctorAccount.UserId,
        assignedNurseUserId: nurseAccount.UserId,
        insuranceProvider: "SmokeHealth",
        emergencyContact: "Smoke Contact",
        roomAssignment: "Room 101",
        sex: "U",
        emergencyRelationship: "Colleague",
        nationality: "Smoke",
        admitDateOverride: DateTime.Today.AddHours(-1));

    Console.WriteLine($"Created patient account #{patientAccount.UserId} ({patientAccount.DisplayName})");

    var admissions = service.GetCurrentAdmissions().ToList();
    var admissionFound = admissions.Any(a => a.UserId == patientAccount.UserId);
    Console.WriteLine($"Admission listing includes patient: {admissionFound}");

    var appointment = service.ScheduleAppointment(
        patientId: patientAccount.UserId,
        doctorId: doctorAccount.UserId,
        scheduledFor: DateTime.Now.AddHours(4),
        description: "Smoke test appointment");

    Console.WriteLine($"Scheduled appointment #{appointment.AppointmentId} for doctor {appointment.DoctorId} / patient {appointment.PatientId}");

    service.AcceptAppointment(appointment);
    service.CompleteAppointment(appointment);
    var managedAppointments = service.GetManagedAppointments().ToList();
    var appointmentCompleted = managedAppointments.Any(a => a.AppointmentId == appointment.AppointmentId && a.Status == AppointmentStatus.Completed);
    Console.WriteLine($"Appointment completed and tracked: {appointmentCompleted}");

    var dischargeRequest = new DischargeBillingRequest
    {
        PatientUserId = patientAccount.UserId,
        RoomCharge = 750m,
        DoctorFee = 250m,
        MedicineCost = 125.50m,
        OtherCharges = 50m,
        MarkAsPaid = false,
        Notes = "Smoke discharge invoice",
        DischargeDate = DateTime.Now
    };

    var dischargeInvoice = service.DischargePatient(patientAccount, dischargeRequest);
    Console.WriteLine($"Discharged patient with invoice #{dischargeInvoice.InvoiceId} totaling {dischargeInvoice.TotalAmount:C}");

    var outstandingInvoices = service.GetOutstandingInvoices().ToList();
    var invoiceForPatient = outstandingInvoices.FirstOrDefault(i => i.InvoiceId == dischargeInvoice.InvoiceId);
    Console.WriteLine(invoiceForPatient is null
        ? "Expected outstanding invoice for discharge was not found."
        : $"Outstanding invoice located for patient #{invoiceForPatient.PatientId}");

    var refreshedPatient = service.GetAccountById(patientAccount.UserId) ?? throw new InvalidOperationException("Unable to refresh patient account after discharge.");
    Console.WriteLine($"Patient has unpaid bills after discharge: {refreshedPatient.PatientProfile?.HasUnpaidBills}");

    service.ReactivatePatient(refreshedPatient);
    var reactivatedPatient = service.GetAccountById(patientAccount.UserId) ?? throw new InvalidOperationException("Unable to refresh patient account after reactivation.");
    Console.WriteLine($"Patient reactivated. Outstanding flag cleared: {!reactivatedPatient.PatientProfile?.HasUnpaidBills}");

    var readmittedPatient = service.ReadmitExistingPatient(new ExistingPatientAdmissionRequest
    {
        UserId = patientAccount.UserId,
        AssignedDoctorUserId = doctorAccount.UserId,
        AssignedNurseUserId = nurseAccount.UserId,
        RoomAssignment = "Room 102",
        AdmitDateOverride = DateTime.Now.AddMinutes(5),
        ContactNumber = "+63 912 000 0004",
        Address = "Smoke Test Ward B",
        EmergencyContact = "Smoke Contact B",
        EmergencyRelationship = "Friend",
        InsuranceProvider = "SmokeHealth"
    });

    Console.WriteLine($"Readmitted patient room assignment: {readmittedPatient.PatientProfile?.RoomAssignment}");

    var patientOptions = service.GetExistingPatientOptions();
    var optionFound = patientOptions.Any(option => option.UserId == patientAccount.UserId);
    Console.WriteLine($"Existing patient option available: {optionFound}");

    var outstandingAfterReactivate = service.GetOutstandingInvoices().ToList();
    var invoiceCleared = outstandingAfterReactivate.All(i => i.InvoiceId != dischargeInvoice.InvoiceId);
    Console.WriteLine($"Outstanding invoice cleared after reactivation: {invoiceCleared}");

    var updatedAccounts = service.GetAllAccounts().Count();
    var updatedAppointments = service.GetAllAppointments().Count();
    Console.WriteLine($"Accounts observed post-operations: {updatedAccounts}");
    Console.WriteLine($"Appointments observed post-operations: {updatedAppointments}");

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
                ContactNumber = "639120000001",
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
                ContactNumber = "639120000002",
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
