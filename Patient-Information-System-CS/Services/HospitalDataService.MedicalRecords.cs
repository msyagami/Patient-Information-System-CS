using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Patient_Information_System_CS.Data;
using Patient_Information_System_CS.Models;
using EntityMedicalRecord = Patient_Information_System_CS.Models.Entities.MedicalRecord;
using EntityPerson = Patient_Information_System_CS.Models.Entities.Person;

namespace Patient_Information_System_CS.Services
{
    public sealed partial class HospitalDataService
    {
        public IEnumerable<MedicalRecordEntry> GetAllMedicalRecords()
        {
            using var context = CreateContext();
            return QueryMedicalRecords(context).ToList();
        }

        public IEnumerable<MedicalRecordEntry> GetMedicalRecordsForPatient(int patientIdentifier)
        {
            using var context = CreateContext();
            var patientId = NormalizePatientId(context, patientIdentifier);
            if (!patientId.HasValue)
            {
                return Array.Empty<MedicalRecordEntry>();
            }

            return QueryMedicalRecords(context)
                .Where(record => record.PatientId == patientId.Value)
                .ToList();
        }

        public IEnumerable<MedicalRecordEntry> GetMedicalRecordsForDoctor(int doctorIdentifier)
        {
            using var context = CreateContext();
            var doctorId = NormalizeDoctorId(context, doctorIdentifier);
            if (!doctorId.HasValue)
            {
                return Array.Empty<MedicalRecordEntry>();
            }

            return QueryMedicalRecords(context)
                .Where(record => record.DoctorId == doctorId.Value)
                .ToList();
        }

        public MedicalRecordEntry? GetMedicalRecordById(int recordId)
        {
            using var context = CreateContext();

            var entity = context.MedicalRecords
                .Include(r => r.AssignedPatient)
                    .ThenInclude(p => p.Person)
                        .ThenInclude(person => person.Users)
                .Include(r => r.AssignedDoctor)
                    .ThenInclude(d => d.Person)
                        .ThenInclude(person => person.Users)
                .AsNoTracking()
                .SingleOrDefault(r => r.RecordId == recordId);

            return entity is null ? null : MapMedicalRecord(entity);
        }

        public MedicalRecordEntry CreateMedicalRecord(MedicalRecordRequest request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Diagnosis))
            {
                throw new ArgumentException("Diagnosis is required.", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Treatment))
            {
                throw new ArgumentException("Treatment is required.", nameof(request));
            }

            using var context = CreateContext(tracking: true);

            var patientId = NormalizePatientId(context, request.PatientIdentifier);
            if (!patientId.HasValue)
            {
                throw new InvalidOperationException("Unable to resolve the selected patient. Please ensure the patient account still exists.");
            }

            var doctorId = NormalizeDoctorId(context, request.DoctorIdentifier);
            if (!doctorId.HasValue)
            {
                throw new InvalidOperationException("Unable to resolve the selected doctor. Please ensure the doctor account still exists.");
            }

            var recordId = NextMedicalRecordId(context);
            var recordNumber = GenerateMedicalRecordNumber(context);

            var diagnosis = request.Diagnosis.Trim();
            var treatment = request.Treatment.Trim();
            var prescriptions = request.Prescriptions?.Trim() ?? string.Empty;

            var record = new EntityMedicalRecord
            {
                RecordId = recordId,
                RecordIdNumber = recordNumber,
                RecordDate = DateOnly.FromDateTime(request.RecordDate.Date),
                AssignedPatientId = patientId.Value,
                AssignedDoctorId = doctorId.Value,
                Diagnosis = Truncate(diagnosis, 500),
                Treatment = Truncate(treatment, 500),
                Prescriptions = Truncate(prescriptions, 500)
            };

            context.MedicalRecords.Add(record);
            context.SaveChanges();

            var createdRecord = context.MedicalRecords
                .Include(r => r.AssignedPatient)
                    .ThenInclude(p => p.Person)
                        .ThenInclude(person => person.Users)
                .Include(r => r.AssignedDoctor)
                    .ThenInclude(d => d.Person)
                        .ThenInclude(person => person.Users)
                .AsNoTracking()
                .Single(r => r.RecordId == recordId);

            var entry = MapMedicalRecord(createdRecord);
            RaiseMedicalRecordsChanged();
            return entry;
        }

        private IEnumerable<MedicalRecordEntry> QueryMedicalRecords(HospitalDbContext context)
        {
            var records = context.MedicalRecords
                .Include(r => r.AssignedPatient)
                    .ThenInclude(p => p.Person)
                        .ThenInclude(person => person.Users)
                .Include(r => r.AssignedDoctor)
                    .ThenInclude(d => d.Person)
                        .ThenInclude(person => person.Users)
                .AsNoTracking()
                .OrderByDescending(r => r.RecordDate)
                .ThenByDescending(r => r.RecordId)
                .ToList();

            return records.Select(MapMedicalRecord).ToList();
        }

        private static MedicalRecordEntry MapMedicalRecord(EntityMedicalRecord entity)
        {
            var patientName = entity.AssignedPatient?.Person is EntityPerson patientPerson
                ? FormatFullName(patientPerson)
                : $"Patient #{entity.AssignedPatientId}";

            var doctorName = entity.AssignedDoctor?.Person is EntityPerson doctorPerson
                ? FormatFullName(doctorPerson)
                : $"Doctor #{entity.AssignedDoctorId}";

            var patientUserId = entity.AssignedPatient?.Person?.Users.FirstOrDefault()?.UserId ?? 0;
            var doctorUserId = entity.AssignedDoctor?.Person?.Users.FirstOrDefault()?.UserId ?? 0;

            return new MedicalRecordEntry
            {
                RecordId = entity.RecordId,
                RecordNumber = string.IsNullOrWhiteSpace(entity.RecordIdNumber)
                    ? $"MR-{entity.RecordId:D5}"
                    : entity.RecordIdNumber,
                RecordedOn = entity.RecordDate.ToDateTime(TimeOnly.MinValue),
                PatientId = entity.AssignedPatientId,
                PatientUserId = patientUserId,
                PatientName = patientName,
                DoctorId = entity.AssignedDoctorId,
                DoctorUserId = doctorUserId,
                DoctorName = doctorName,
                Diagnosis = entity.Diagnosis ?? string.Empty,
                Treatment = entity.Treatment ?? string.Empty,
                Prescriptions = entity.Prescriptions ?? string.Empty
            };
        }

        private static string Truncate(string value, int maxLength)
        {
            if (value.Length <= maxLength)
            {
                return value;
            }

            return value[..maxLength];
        }
    }
}
