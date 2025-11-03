using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Patient_Information_System_CS.Data;
using Patient_Information_System_CS.Models;
using EntityBill = Patient_Information_System_CS.Models.Entities.Bill;
using EntityDepartment = Patient_Information_System_CS.Models.Entities.Department;
using EntityDoctor = Patient_Information_System_CS.Models.Entities.Doctor;
using EntityInsurance = Patient_Information_System_CS.Models.Entities.Insurance;
using EntityNurse = Patient_Information_System_CS.Models.Entities.Nurse;
using EntityPatient = Patient_Information_System_CS.Models.Entities.Patient;
using EntityPerson = Patient_Information_System_CS.Models.Entities.Person;
using EntityRoom = Patient_Information_System_CS.Models.Entities.Room;

namespace Patient_Information_System_CS.Services
{
    public sealed partial class HospitalDataService
    {
        private const string DefaultSexCode = "U";
        private const string DefaultBloodType = "UNK";

        private static string EncodePassword(string plainText)
        {
            var bytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(bytes);
        }

        private static string GenerateUniqueUsername(HospitalDbContext context, string fullName, string prefix)
        {
            var slug = new string(fullName.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(slug))
            {
                slug = prefix;
            }

            slug = slug.Length > 12 ? slug[..12] : slug;

            var candidate = slug;
            var counter = 1;

            while (context.Users.Any(u => u.Username == candidate))
            {
                candidate = $"{slug}{counter++}";
                if (candidate.Length > 20)
                {
                    candidate = candidate[..20];
                }
            }

            return candidate;
        }

        private static string GeneratePatientUsername(HospitalDbContext context, string fullName) =>
            GenerateUniqueUsername(context, fullName, "patient");

        private static string GeneratePatientNumber(HospitalDbContext context)
        {
            var existingNumbers = context.Patients
                .Select(p => p.PatientIdNumber)
                .Where(number => number != null)
                .ToList();

            var next = existingNumbers
                .Select(ParsePatientNumber)
                .DefaultIfEmpty(1000)
                .Max() + 1;

            return $"P-{next:D5}";
        }

        private static int ParsePatientNumber(string? patientNumber)
        {
            if (string.IsNullOrWhiteSpace(patientNumber))
            {
                return 1000;
            }

            var digits = new string(patientNumber.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric)
                ? numeric
                : 1000;
        }

        private static string GenerateEmailAddress(string fullName)
        {
            var slug = fullName.Replace(' ', '.').ToLowerInvariant();
            return $"{slug}@example.com";
        }

        private static (string GivenName, string LastName, string? MiddleName) ParseName(string fullName)
        {
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return ("Unknown", "User", null);
            }

            if (parts.Length == 1)
            {
                return (parts[0], parts[0], null);
            }

            var given = parts[0];
            var last = parts[^1];
            var middle = parts.Length > 2 ? string.Join(' ', parts.Skip(1).Take(parts.Length - 2)) : null;
            return (given, last, middle);
        }

        private static long ParseContactNumber(string contactNumber)
        {
            var digits = new string(contactNumber.Where(char.IsDigit).ToArray());
            return long.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric)
                ? numeric
                : 0;
        }

        private static string FormatContact(long contactNumber) =>
            contactNumber == 0 ? "Not provided" : contactNumber.ToString(CultureInfo.InvariantCulture);

        private static string FormatFullName(EntityPerson person)
        {
            var builder = new StringBuilder();
            builder.Append(person.GivenName);

            if (!string.IsNullOrWhiteSpace(person.MiddleName))
            {
                builder.Append(' ').Append(person.MiddleName);
            }

            builder.Append(' ').Append(person.LastName);

            if (!string.IsNullOrWhiteSpace(person.Suffix))
            {
                builder.Append(' ').Append(person.Suffix);
            }

            return builder.ToString();
        }

        private static string BuildEmergencyContact(EntityPerson person)
        {
            if (string.IsNullOrWhiteSpace(person.EmergencyContact))
            {
                return "Not provided";
            }

            return string.IsNullOrWhiteSpace(person.RelationshipToEmergencyContact)
                ? person.EmergencyContact
                : $"{person.EmergencyContact} ({person.RelationshipToEmergencyContact})";
        }

        private static string BuildRoomAssignment(EntityRoom? room)
        {
            if (room is null)
            {
                return string.Empty;
            }

            return room.RoomNumber == 0
                ? room.RoomType
                : $"Room {room.RoomNumber} - {room.RoomType}";
        }

        private static string BuildInsuranceSummary(IEnumerable<EntityInsurance> insurances)
        {
            var list = insurances
                .Select(i => string.IsNullOrWhiteSpace(i.ProviderName)
                    ? "Policy"
                    : string.IsNullOrWhiteSpace(i.PolicyNumber)
                        ? i.ProviderName
                        : $"{i.ProviderName} ({i.PolicyNumber})")
                .ToList();

            return list.Count == 0 ? "Not Provided" : string.Join(", ", list);
        }

        private static DoctorStatus MapDoctorStatus(byte value) => value switch
        {
            1 => DoctorStatus.Available,
            2 => DoctorStatus.NotAvailable,
            _ => DoctorStatus.OnHold
        };

        private static byte MapDoctorStatusToByte(DoctorStatus status) => status switch
        {
            DoctorStatus.Available => 1,
            DoctorStatus.NotAvailable => 2,
            _ => 0
        };

        private static AppointmentStatus MapAppointmentStatus(byte value) => value switch
        {
            1 => AppointmentStatus.Accepted,
            2 => AppointmentStatus.Completed,
            3 => AppointmentStatus.Rejected,
            _ => AppointmentStatus.Pending
        };

        private static byte MapAppointmentStatusToByte(AppointmentStatus status) => status switch
        {
            AppointmentStatus.Accepted => 1,
            AppointmentStatus.Completed => 2,
            AppointmentStatus.Rejected => 3,
            _ => 0
        };

        private static UserRole MapRole(string? role) => role?.ToLowerInvariant() switch
        {
            "admin" => UserRole.Admin,
            "doctor" => UserRole.Doctor,
            "staff" => UserRole.Staff,
            _ => UserRole.Patient
        };

        private static bool IsBillPaid(EntityBill bill) => bill.Status == 1;

        private static int NextPersonId(HospitalDbContext context) =>
            context.People.Any() ? context.People.Max(p => p.PersonId) + 1 : 1;

        private static int NextUserId(HospitalDbContext context) =>
            context.Users.Any() ? context.Users.Max(u => u.UserId) + 1 : 1;

        private static int NextStaffId(HospitalDbContext context) =>
            context.Staff.Any() ? context.Staff.Max(s => s.StaffId) + 1 : 1;

        private static int NextDoctorId(HospitalDbContext context) =>
            context.Doctors.Any() ? context.Doctors.Max(d => d.DoctorId) + 1 : 1;

        private static int NextNurseId(HospitalDbContext context) =>
            context.Nurses.Any() ? context.Nurses.Max(n => n.NurseId) + 1 : 1;

        private static int NextPatientId(HospitalDbContext context) =>
            context.Patients.Any() ? context.Patients.Max(p => p.PatientId) + 1 : 1;

        private static int NextAppointmentId(HospitalDbContext context) =>
            context.Appointments.Any() ? context.Appointments.Max(a => a.AppointmentId) + 1 : 1;

        private static int NextBillId(HospitalDbContext context) =>
            context.Bills.Any() ? context.Bills.Max(b => b.BillId) + 1 : 1;

        private static int NextRoomId(HospitalDbContext context) =>
            context.Rooms.Any() ? context.Rooms.Max(r => r.RoomId) + 1 : 1;

        private static int NextDepartmentId(HospitalDbContext context) =>
            context.Departments.Any() ? context.Departments.Max(d => d.DepartmentId) + 1 : 1;

        private static int ResolveFallbackDoctor(HospitalDbContext context) =>
            context.Doctors.Select(d => d.DoctorId).OrderBy(id => id).FirstOrDefault();

        private static int ResolveFallbackPatient(HospitalDbContext context) =>
            context.Patients.Select(p => p.PatientId).OrderBy(id => id).FirstOrDefault();

        private static int? ResolveDoctorId(HospitalDbContext context, int? userId)
        {
            if (userId is null)
            {
                return null;
            }

            var doctor = context.Users
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Doctors)
                .AsNoTracking()
                .SingleOrDefault(u => u.UserId == userId);

            return doctor?.AssociatedPerson?.Doctors.FirstOrDefault()?.DoctorId;
        }

        private static int? ResolveFallbackNurse(HospitalDbContext context) =>
            context.Nurses.Select(n => n.NurseId).OrderBy(id => id).FirstOrDefault();

        private static EntityRoom ResolveRoom(HospitalDbContext context, string roomAssignment)
        {
            if (!string.IsNullOrWhiteSpace(roomAssignment))
            {
                var roomNumberDigits = new string(roomAssignment.Where(char.IsDigit).ToArray());
                if (int.TryParse(roomNumberDigits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var roomNumber))
                {
                    var roomMatch = context.Rooms.FirstOrDefault(r => r.RoomNumber == roomNumber);
                    if (roomMatch is not null)
                    {
                        return roomMatch;
                    }
                }
            }

            var fallback = context.Rooms.OrderBy(r => r.RoomId).FirstOrDefault();
            if (fallback is null)
            {
                throw new InvalidOperationException("No rooms are defined in the database. Please seed the Room table before admitting patients.");
            }

            return fallback;
        }
    }
}
