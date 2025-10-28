using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Patient_Information_System_CS.Models;

namespace Patient_Information_System_CS.Services
{
    public sealed class HospitalDataService
    {
        private static readonly Lazy<HospitalDataService> _instance = new(() => new HospitalDataService());
        private readonly List<UserAccount> _accounts;
        private readonly List<Appointment> _appointments;
        private readonly List<BillingRecord> _billingRecords;
        private int _nextUserId;
        private int _nextPatientSequence;
        private int _nextAppointmentId;
        private int _nextInvoiceId;

        private HospitalDataService()
        {
            _accounts = SeedUsers();
            _nextUserId = _accounts.Max(a => a.UserId) + 1;
            _nextPatientSequence = CalculateNextPatientSequence();
            _appointments = SeedAppointments();
            _billingRecords = SeedBillingRecords();
            _nextAppointmentId = _appointments.Count == 0 ? 1 : _appointments.Max(a => a.AppointmentId) + 1;
            _nextInvoiceId = _billingRecords.Count == 0 ? 1 : _billingRecords.Max(b => b.InvoiceId) + 1;

            foreach (var record in _billingRecords)
            {
                UpdatePatientBillingStatus(record.PatientId);
            }
        }

        public static HospitalDataService Instance => _instance.Value;

        public IReadOnlyList<UserAccount> Accounts => _accounts;

        public IEnumerable<UserAccount> GetApprovedStaff() =>
            _accounts.Where(account => account.Role is UserRole.Staff || account.Role is UserRole.Admin)
                     .Where(account => account.AdminProfile?.IsApproved == true || account.StaffProfile?.IsApproved == true)
                     .ToList();

        public IEnumerable<UserAccount> GetPendingStaff() =>
            _accounts.Where(account => account.Role is UserRole.Staff || account.Role is UserRole.Admin)
                     .Where(account => account.AdminProfile?.IsApproved == false || account.StaffProfile?.IsApproved == false)
                     .ToList();

        public void ApproveStaff(UserAccount account)
        {
            if (account.AdminProfile is not null)
            {
                account.AdminProfile.IsApproved = true;
            }

            if (account.StaffProfile is not null)
            {
                account.StaffProfile.IsApproved = true;
            }
        }

        public void RejectStaff(UserAccount account)
        {
            _accounts.Remove(account);
        }

        public IEnumerable<UserAccount> GetActiveDoctors() =>
            _accounts.Where(account => account.Role == UserRole.Doctor && account.DoctorProfile?.Status == DoctorStatus.Available)
                     .ToList();

        public IEnumerable<UserAccount> GetPendingDoctors() =>
            _accounts.Where(account => account.Role == UserRole.Doctor && account.DoctorProfile?.Status == DoctorStatus.OnHold)
                     .ToList();

        public IEnumerable<UserAccount> GetUnavailableDoctors() =>
            _accounts.Where(account => account.Role == UserRole.Doctor && account.DoctorProfile?.Status == DoctorStatus.NotAvailable)
                     .ToList();

        public void ApproveDoctor(UserAccount doctor)
        {
            if (doctor.DoctorProfile is null)
            {
                return;
            }

            doctor.DoctorProfile.Status = DoctorStatus.Available;
        }

        public void RejectDoctor(UserAccount doctor)
        {
            _accounts.Remove(doctor);
        }

        public void ToggleDoctorAvailability(UserAccount doctor)
        {
            if (doctor.DoctorProfile is null)
            {
                return;
            }

            doctor.DoctorProfile.Status = doctor.DoctorProfile.Status == DoctorStatus.Available
                ? DoctorStatus.NotAvailable
                : DoctorStatus.Available;
        }

        public IEnumerable<UserAccount> GetApprovedPatients() =>
            _accounts.Where(account => account.Role == UserRole.Patient && account.PatientProfile?.IsApproved == true)
                     .ToList();

        public IEnumerable<UserAccount> GetPendingPatients() =>
            _accounts.Where(account => account.Role == UserRole.Patient && account.PatientProfile?.IsApproved == false)
                     .ToList();

        public IEnumerable<UserAccount> GetPatientsWithPendingBills() =>
            _accounts.Where(account => account.Role == UserRole.Patient && account.PatientProfile?.HasUnpaidBills == true)
                     .ToList();

        public void ApprovePatient(UserAccount patient)
        {
            if (patient.PatientProfile is null)
            {
                return;
            }

            patient.PatientProfile.IsApproved = true;
            patient.IsActive = true;
        }

        public void RejectPatient(UserAccount patient)
        {
            _accounts.Remove(patient);
        }

        public IEnumerable<UserAccount> GetCurrentAdmissions() =>
            _accounts.Where(account => account.Role == UserRole.Patient)
                     .Where(account => account.PatientProfile?.IsCurrentlyAdmitted == true)
                     .ToList();

        public IEnumerable<UserAccount> GetDeactivatedPatients() =>
            _accounts.Where(account => account.Role == UserRole.Patient)
                     .Where(account => account.IsActive == false || account.PatientProfile?.HasUnpaidBills == true)
                     .ToList();

        public void DischargePatient(UserAccount patient)
        {
            if (patient.PatientProfile is null)
            {
                return;
            }

            CreateInvoiceForDischarge(patient);
            patient.PatientProfile.IsCurrentlyAdmitted = false;
            patient.PatientProfile.RoomAssignment = string.Empty;
            patient.PatientProfile.AssignedDoctorId = null;
            patient.PatientProfile.AssignedDoctorName = "Unassigned";
            patient.IsActive = false;
        }

        public void ReactivatePatient(UserAccount patient)
        {
            if (patient.PatientProfile is null)
            {
                return;
            }

            patient.IsActive = true;
            patient.PatientProfile.HasUnpaidBills = false;
            patient.PatientProfile.IsCurrentlyAdmitted = true;
            patient.PatientProfile.AdmitDate = DateTime.Today;

            MarkInvoicesPaidForPatient(patient.UserId);

            if (patient.PatientProfile.AssignedDoctorId is null)
            {
                var fallbackDoctor = GetActiveDoctors().FirstOrDefault();
                if (fallbackDoctor is not null)
                {
                    patient.PatientProfile.AssignedDoctorId = fallbackDoctor.UserId;
                    patient.PatientProfile.AssignedDoctorName = fallbackDoctor.DisplayName;
                }
            }

            if (string.IsNullOrWhiteSpace(patient.PatientProfile.RoomAssignment))
            {
                patient.PatientProfile.RoomAssignment = "Room 101 - General Ward";
            }
        }

        public IEnumerable<Appointment> GetPendingAppointments() =>
            _appointments.Where(appointment => appointment.Status == AppointmentStatus.Pending)
                         .OrderBy(appointment => appointment.ScheduledFor)
                         .ToList();

        public IEnumerable<Appointment> GetManagedAppointments() =>
            _appointments.Where(appointment => appointment.Status != AppointmentStatus.Pending)
                         .OrderBy(appointment => appointment.ScheduledFor)
                         .ToList();

        public Appointment ScheduleAppointment(int? patientId,
                                               int? doctorId,
                                               DateTime scheduledFor,
                                               string description)
        {
            var patientName = patientId is int pid
                ? _accounts.FirstOrDefault(account => account.UserId == pid)?.DisplayName ?? "Unknown Patient"
                : "Walk-in Patient";

            var doctorName = doctorId is int did
                ? _accounts.FirstOrDefault(account => account.UserId == did)?.DisplayName ?? "Unassigned"
                : "Unassigned";

            var appointment = new Appointment
            {
                AppointmentId = _nextAppointmentId++,
                PatientId = patientId,
                DoctorId = doctorId,
                PatientName = patientName,
                DoctorName = doctorName,
                ScheduledFor = scheduledFor,
                Description = description,
                Status = AppointmentStatus.Pending
            };

            _appointments.Add(appointment);
            return appointment;
        }

        public void AcceptAppointment(Appointment appointment)
        {
            if (appointment.Status != AppointmentStatus.Pending)
            {
                return;
            }

            appointment.Status = AppointmentStatus.Accepted;
        }

        public void RejectAppointment(Appointment appointment)
        {
            if (appointment.Status == AppointmentStatus.Completed)
            {
                return;
            }

            appointment.Status = AppointmentStatus.Rejected;
        }

        public void CompleteAppointment(Appointment appointment)
        {
            if (appointment.Status != AppointmentStatus.Accepted)
            {
                return;
            }

            appointment.Status = AppointmentStatus.Completed;
        }

        public IEnumerable<BillingRecord> GetOutstandingInvoices() =>
            _billingRecords.Where(record => !record.IsPaid)
                           .OrderByDescending(record => record.ReleaseDate)
                           .ToList();

        public IEnumerable<BillingRecord> GetPaidInvoices() =>
            _billingRecords.Where(record => record.IsPaid)
                           .OrderByDescending(record => record.ReleaseDate)
                           .ToList();

        public BillingRecord? GetInvoiceById(int invoiceId) =>
            _billingRecords.FirstOrDefault(record => record.InvoiceId == invoiceId);

        public void MarkInvoicePaid(BillingRecord invoice)
        {
            if (invoice.IsPaid)
            {
                return;
            }

            invoice.IsPaid = true;
            invoice.PaidDate = DateTime.Today;
            UpdatePatientBillingStatus(invoice.PatientId);
        }

        public UserAccount AdmitNewPatient(string fullName,
                                           DateTime dateOfBirth,
                                           string contactNumber,
                                           string address,
                                           string emergencyContact,
                                           string insuranceProvider,
                                           UserAccount? doctor,
                                           string roomAssignment)
        {
            var patientNumber = GeneratePatientNumber();
            var username = GeneratePatientUsername(fullName);
            var email = GenerateEmailAddress(fullName);

            var newPatient = new UserAccount
            {
                UserId = _nextUserId++,
                Username = username,
                Password = "patientpass",
                DisplayName = fullName,
                Email = email,
                Role = UserRole.Patient,
                IsActive = true,
                PatientProfile = new PatientProfile
                {
                    IsApproved = true,
                    HasUnpaidBills = false,
                    PatientNumber = patientNumber,
                    AdmitDate = DateTime.Today,
                    RoomAssignment = roomAssignment,
                    ContactNumber = contactNumber,
                    Address = address,
                    EmergencyContact = emergencyContact,
                    InsuranceProvider = insuranceProvider,
                    AssignedDoctorId = doctor?.UserId,
                    AssignedDoctorName = doctor?.DisplayName ?? "Unassigned",
                    DateOfBirth = dateOfBirth,
                    IsCurrentlyAdmitted = true
                }
            };

            _accounts.Add(newPatient);
            return newPatient;
        }

        public UserAccount? GetDoctorById(int? doctorId)
        {
            if (doctorId is null)
            {
                return null;
            }

            return _accounts.FirstOrDefault(account => account.UserId == doctorId && account.Role == UserRole.Doctor);
        }

        private string GeneratePatientNumber()
        {
            var number = _nextPatientSequence++;
            return $"P-{number:D5}";
        }

        private string GeneratePatientUsername(string fullName)
        {
            var slug = fullName.Replace(" ", string.Empty, StringComparison.Ordinal)
                               .ToLowerInvariant();
            var username = $"{slug}{_nextPatientSequence}";
            while (_accounts.Any(account => string.Equals(account.Username, username, StringComparison.OrdinalIgnoreCase)))
            {
                username = $"patient{_nextPatientSequence++}";
            }

            return username;
        }

        private static string GenerateEmailAddress(string fullName)
        {
            var slug = fullName.Replace(" ", ".", StringComparison.Ordinal).ToLowerInvariant();
            return $"{slug}@example.com";
        }

        private int CalculateNextPatientSequence()
        {
            var max = _accounts.Where(account => account.PatientProfile is not null)
                               .Select(account => account.PatientProfile!.PatientNumber)
                               .Select(ParsePatientNumber)
                               .DefaultIfEmpty(1000)
                               .Max();

            return max + 1;
        }

        private static int ParsePatientNumber(string patientNumber)
        {
            if (string.IsNullOrWhiteSpace(patientNumber))
            {
                return 1000;
            }

            var span = patientNumber.AsSpan();
            var dashIndex = span.IndexOf('-');
            if (dashIndex >= 0)
            {
                span = span[(dashIndex + 1)..];
            }

            return int.TryParse(span, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
                ? result
                : 1000;
        }

        private void CreateInvoiceForDischarge(UserAccount patient)
        {
            if (patient.PatientProfile is null)
            {
                return;
            }

            var hasOutstanding = _billingRecords.Any(record => record.PatientId == patient.UserId && !record.IsPaid);
            if (!hasOutstanding)
            {
                var admitDate = patient.PatientProfile.AdmitDate ?? DateTime.Today.AddDays(-1);
                var releaseDate = DateTime.Today;
                var daysStayed = Math.Max((releaseDate - admitDate).Days, 1);
                var roomCharge = CalculateRoomCharge(patient.PatientProfile.RoomAssignment, daysStayed);

                var invoice = new BillingRecord
                {
                    InvoiceId = _nextInvoiceId++,
                    PatientId = patient.UserId,
                    PatientName = patient.DisplayName,
                    ContactNumber = patient.PatientProfile.ContactNumber,
                    Address = patient.PatientProfile.Address,
                    DoctorId = patient.PatientProfile.AssignedDoctorId,
                    DoctorName = string.IsNullOrWhiteSpace(patient.PatientProfile.AssignedDoctorName)
                        ? "Unassigned"
                        : patient.PatientProfile.AssignedDoctorName,
                    AdmitDate = admitDate,
                    ReleaseDate = releaseDate,
                    DaysStayed = daysStayed,
                    RoomCharge = roomCharge,
                    DoctorFee = 500,
                    MedicineCost = 250,
                    OtherCharge = 150,
                    IsPaid = false
                };

                _billingRecords.Add(invoice);
            }

            UpdatePatientBillingStatus(patient.UserId);
        }

        private void MarkInvoicesPaidForPatient(int patientId)
        {
            foreach (var invoice in _billingRecords.Where(record => record.PatientId == patientId && !record.IsPaid))
            {
                invoice.IsPaid = true;
                invoice.PaidDate = DateTime.Today;
            }

            UpdatePatientBillingStatus(patientId);
        }

        private void UpdatePatientBillingStatus(int patientId)
        {
            var account = _accounts.FirstOrDefault(user => user.UserId == patientId);
            if (account?.PatientProfile is null)
            {
                return;
            }

            var hasOutstanding = _billingRecords.Any(record => record.PatientId == patientId && !record.IsPaid);
            account.PatientProfile.HasUnpaidBills = hasOutstanding;
        }

        private static int CalculateRoomCharge(string roomAssignment, int daysStayed)
        {
            var rate = 250;

            if (!string.IsNullOrWhiteSpace(roomAssignment))
            {
                if (roomAssignment.Contains("ICU", StringComparison.OrdinalIgnoreCase))
                {
                    rate = 550;
                }
                else if (roomAssignment.Contains("Private", StringComparison.OrdinalIgnoreCase))
                {
                    rate = 400;
                }
            }

            return rate * Math.Max(daysStayed, 1);
        }

        private List<Appointment> SeedAppointments()
        {
            return new List<Appointment>
            {
                new()
                {
                    AppointmentId = 1,
                    PatientId = 20,
                    DoctorId = 10,
                    PatientName = "Mary Collins",
                    DoctorName = "Dr. Emma Stone",
                    ScheduledFor = DateTime.Today.AddDays(1).AddHours(9),
                    Description = "Post-operative follow-up", 
                    Status = AppointmentStatus.Accepted
                },
                new()
                {
                    AppointmentId = 2,
                    PatientId = 21,
                    DoctorId = 10,
                    PatientName = "John Fields",
                    DoctorName = "Dr. Emma Stone",
                    ScheduledFor = DateTime.Today.AddDays(2).AddHours(11),
                    Description = "Initial consultation for recurring headaches",
                    Status = AppointmentStatus.Pending
                },
                new()
                {
                    AppointmentId = 3,
                    PatientId = 22,
                    DoctorId = 11,
                    PatientName = "Cathy Moss",
                    DoctorName = "Dr. Liam Turner",
                    ScheduledFor = DateTime.Today.AddDays(-3).AddHours(14),
                    Description = "Routine check-up after discharge",
                    Status = AppointmentStatus.Completed
                }
            };
        }

        private List<BillingRecord> SeedBillingRecords()
        {
            return new List<BillingRecord>
            {
                new()
                {
                    InvoiceId = 1,
                    PatientId = 22,
                    PatientName = "Cathy Moss",
                    ContactNumber = "+1 555-3003",
                    Address = "304 Sunset Blvd",
                    DoctorId = 10,
                    DoctorName = "Dr. Emma Stone",
                    AdmitDate = DateTime.Today.AddDays(-5),
                    ReleaseDate = DateTime.Today.AddDays(-2),
                    DaysStayed = 4,
                    RoomCharge = CalculateRoomCharge("Room 101 - General Ward", 4),
                    DoctorFee = 550,
                    MedicineCost = 320,
                    OtherCharge = 150,
                    IsPaid = false
                },
                new()
                {
                    InvoiceId = 2,
                    PatientId = 20,
                    PatientName = "Mary Collins",
                    ContactNumber = "+1 555-3001",
                    Address = "89 River Road",
                    DoctorId = 10,
                    DoctorName = "Dr. Emma Stone",
                    AdmitDate = DateTime.Today.AddDays(-14),
                    ReleaseDate = DateTime.Today.AddDays(-12),
                    DaysStayed = 3,
                    RoomCharge = CalculateRoomCharge("Room 201 - ICU", 3),
                    DoctorFee = 600,
                    MedicineCost = 425,
                    OtherCharge = 200,
                    IsPaid = true,
                    PaidDate = DateTime.Today.AddDays(-10)
                }
            };
        }

        private static List<UserAccount> SeedUsers()
        {
            return new List<UserAccount>
            {
                new()
                {
                    UserId = 1,
                    Username = "admin1",
                    Password = "adminpass",
                    DisplayName = "Alice Johnson",
                    Email = "alice.admin@example.com",
                    Role = UserRole.Admin,
                    AdminProfile = new AdminProfile
                    {
                        IsApproved = true,
                        ContactNumber = "+1 555-1001"
                    }
                },
                new()
                {
                    UserId = 2,
                    Username = "admin2",
                    Password = "pending",
                    DisplayName = "Bob Carter",
                    Email = "bob.admin@example.com",
                    Role = UserRole.Admin,
                    AdminProfile = new AdminProfile
                    {
                        IsApproved = false,
                        ContactNumber = "+1 555-1002"
                    }
                },
                new()
                {
                    UserId = 10,
                    Username = "doc1",
                    Password = "doctorpass",
                    DisplayName = "Dr. Emma Stone",
                    Email = "emma.doctor@example.com",
                    Role = UserRole.Doctor,
                    DoctorProfile = new DoctorProfile
                    {
                        Status = DoctorStatus.Available,
                        Department = "Cardiology",
                        ContactNumber = "+1 555-2001",
                        LicenseNumber = "LIC-450012",
                        Address = "12 Health Ave",
                        ApplicationDate = DateTime.Today.AddMonths(-6)
                    }
                },
                new()
                {
                    UserId = 11,
                    Username = "doc2",
                    Password = "hold",
                    DisplayName = "Dr. Liam Turner",
                    Email = "liam.doctor@example.com",
                    Role = UserRole.Doctor,
                    DoctorProfile = new DoctorProfile
                    {
                        Status = DoctorStatus.OnHold,
                        Department = "General Medicine",
                        ContactNumber = "+1 555-2002",
                        LicenseNumber = "LIC-450108",
                        Address = "34 Wellness St",
                        ApplicationDate = DateTime.Today.AddDays(-12)
                    }
                },
                new()
                {
                    UserId = 20,
                    Username = "patient1",
                    Password = "patientpass",
                    DisplayName = "Mary Collins",
                    Email = "mary.patient@example.com",
                    Role = UserRole.Patient,
                    PatientProfile = new PatientProfile
                    {
                        IsApproved = true,
                        HasUnpaidBills = false,
                        PatientNumber = "P-01001",
                        AdmitDate = DateTime.Today.AddDays(-2),
                        RoomAssignment = "Room 201 - ICU",
                        ContactNumber = "+1 555-3001",
                        Address = "89 River Road",
                        EmergencyContact = "Kate Collins (+1 555-9001)",
                        InsuranceProvider = "ABC Medical INC - Policy #215858472",
                        AssignedDoctorId = 10,
                        AssignedDoctorName = "Dr. Emma Stone",
                        DateOfBirth = new DateTime(1992, 5, 14),
                        IsCurrentlyAdmitted = true
                    }
                },
                new()
                {
                    UserId = 21,
                    Username = "patient2",
                    Password = "await",
                    DisplayName = "John Fields",
                    Email = "john.patient@example.com",
                    Role = UserRole.Patient,
                    PatientProfile = new PatientProfile
                    {
                        IsApproved = false,
                        HasUnpaidBills = false,
                        PatientNumber = "P-01002",
                        ContactNumber = "+1 555-3002",
                        Address = "12 Pine Street",
                        EmergencyContact = "Laura Fields (+1 555-9002)",
                        InsuranceProvider = "XYZ Healthcare - Policy #987654321",
                        AssignedDoctorId = null,
                        AssignedDoctorName = "Unassigned",
                        DateOfBirth = new DateTime(1988, 10, 5),
                        IsCurrentlyAdmitted = false
                    }
                },
                new()
                {
                    UserId = 22,
                    Username = "patient3",
                    Password = "billdue",
                    DisplayName = "Cathy Moss",
                    Email = "cathy.patient@example.com",
                    Role = UserRole.Patient,
                    PatientProfile = new PatientProfile
                    {
                        IsApproved = true,
                        HasUnpaidBills = true,
                        PatientNumber = "P-01003",
                        AdmitDate = DateTime.Today.AddDays(-20),
                        RoomAssignment = "Room 101 - General Ward",
                        ContactNumber = "+1 555-3003",
                        Address = "304 Sunset Blvd",
                        EmergencyContact = "Evan Moss (+1 555-9003)",
                        InsuranceProvider = "No Insurance",
                        AssignedDoctorId = 10,
                        AssignedDoctorName = "Dr. Emma Stone",
                        DateOfBirth = new DateTime(1990, 1, 2),
                        IsCurrentlyAdmitted = false
                    },
                    IsActive = false
                },
                new()
                {
                    UserId = 30,
                    Username = "staff1",
                    Password = "staffpass",
                    DisplayName = "Ruth Adams",
                    Email = "ruth.staff@example.com",
                    Role = UserRole.Staff,
                    StaffProfile = new StaffProfile
                    {
                        IsApproved = true,
                        ContactNumber = "+1 555-4001"
                    }
                },
                new()
                {
                    UserId = 31,
                    Username = "staff2",
                    Password = "awaitstaff",
                    DisplayName = "Glenn Bryd",
                    Email = "glenn.staff@example.com",
                    Role = UserRole.Staff,
                    StaffProfile = new StaffProfile
                    {
                        IsApproved = false,
                        ContactNumber = "+1 555-4002"
                    }
                }
            };
        }
    }
}
