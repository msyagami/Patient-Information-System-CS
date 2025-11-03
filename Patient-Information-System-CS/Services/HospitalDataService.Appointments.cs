using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Patient_Information_System_CS.Data;
using Patient_Information_System_CS.Models;
using EntityAppointment = Patient_Information_System_CS.Models.Entities.Appointment;
using EntityPerson = Patient_Information_System_CS.Models.Entities.Person;

namespace Patient_Information_System_CS.Services
{
    public sealed partial class HospitalDataService
    {
        public IEnumerable<Appointment> GetPendingAppointments() =>
            LoadAppointments().Where(appointment => appointment.Status == AppointmentStatus.Pending);

        public IEnumerable<Appointment> GetManagedAppointments() =>
            LoadAppointments().Where(appointment => appointment.Status != AppointmentStatus.Pending);

        public IEnumerable<Appointment> GetAppointmentsForDoctor(int doctorId)
        {
            using var context = CreateContext();
            var normalizedDoctorId = NormalizeDoctorId(context, doctorId);

            if (!normalizedDoctorId.HasValue)
            {
                return Array.Empty<Appointment>();
            }

            return LoadAppointments().Where(appointment => appointment.DoctorId == normalizedDoctorId.Value);
        }

        public IEnumerable<Appointment> GetAppointmentsForPatient(int patientId)
        {
            using var context = CreateContext();
            var normalizedPatientId = NormalizePatientId(context, patientId);

            if (!normalizedPatientId.HasValue)
            {
                return Array.Empty<Appointment>();
            }

            return LoadAppointments().Where(appointment => appointment.PatientId == normalizedPatientId.Value);
        }

        public IEnumerable<Appointment> GetAllAppointments() => LoadAppointments();

        public Appointment ScheduleAppointment(int? patientId, int? doctorId, DateTime scheduledFor, string description)
        {
            using var context = CreateContext(tracking: true);

            var appointmentId = NextAppointmentId(context);

            var resolvedPatientId = NormalizePatientId(context, patientId);
            var resolvedDoctorId = NormalizeDoctorId(context, doctorId);

            var assignedPatientId = resolvedPatientId ?? ResolveFallbackPatient(context);
            var assignedDoctorId = resolvedDoctorId ?? ResolveFallbackDoctor(context);

            var appointmentEntity = new EntityAppointment
            {
                AppointmentId = appointmentId,
                AppointmentIdNumber = $"APT-{appointmentId:D6}",
                AssignedPatientId = assignedPatientId,
                AssignedDoctorId = assignedDoctorId,
                AppointmentSchedule = scheduledFor,
                AppointmentPurpose = string.IsNullOrWhiteSpace(description) ? "General consultation" : description,
                AppointmentStatus = MapAppointmentStatusToByte(AppointmentStatus.Pending)
            };

            context.Appointments.Add(appointmentEntity);
            context.SaveChanges();

            RaiseAppointmentsChanged();

            context.Entry(appointmentEntity).State = EntityState.Detached;

            var reloaded = context.Appointments
                .Include(a => a.AssignedPatient)
                    .ThenInclude(p => p.Person)
                .Include(a => a.AssignedDoctor)
                    .ThenInclude(d => d.Person)
                .AsNoTracking()
                .Single(a => a.AppointmentId == appointmentId);

            return MapAppointment(reloaded);
        }

        public void AcceptAppointment(Appointment appointment) =>
            UpdateAppointmentStatus(appointment.AppointmentId, AppointmentStatus.Accepted);

        public void RejectAppointment(Appointment appointment) =>
            UpdateAppointmentStatus(appointment.AppointmentId, AppointmentStatus.Rejected);

        public void CompleteAppointment(Appointment appointment) =>
            UpdateAppointmentStatus(appointment.AppointmentId, AppointmentStatus.Completed);

        public void CancelAppointment(Appointment appointment) =>
            UpdateAppointmentStatus(appointment.AppointmentId, AppointmentStatus.Rejected);

        private IReadOnlyList<Appointment> LoadAppointments()
        {
            using var context = CreateContext();

            var appointmentEntities = context.Appointments
                .Include(a => a.AssignedPatient)
                    .ThenInclude(p => p.Person)
                .Include(a => a.AssignedDoctor)
                    .ThenInclude(d => d.Person)
                .AsNoTracking()
                .OrderBy(a => a.AppointmentSchedule)
                .ToList();

            return appointmentEntities.Select(MapAppointment).ToList();
        }

        internal static Patient_Information_System_CS.Models.Appointment MapAppointment(EntityAppointment entity)
        {
            var patientName = entity.AssignedPatient?.Person is EntityPerson patientPerson
                ? FormatFullName(patientPerson)
                : entity.AssignedPatient?.PatientIdNumber ?? "Unassigned";

            var doctorName = entity.AssignedDoctor?.Person is EntityPerson doctorPerson
                ? FormatFullName(doctorPerson)
                : "Unassigned";

            return new Patient_Information_System_CS.Models.Appointment
            {
                AppointmentId = entity.AppointmentId,
                PatientId = entity.AssignedPatientId,
                DoctorId = entity.AssignedDoctorId,
                PatientName = patientName,
                DoctorName = doctorName,
                ScheduledFor = entity.AppointmentSchedule,
                Description = string.IsNullOrWhiteSpace(entity.AppointmentPurpose)
                    ? entity.MedicalRecordsText ?? string.Empty
                    : entity.AppointmentPurpose,
                Status = MapAppointmentStatus(entity.AppointmentStatus)
            };
        }

        private void UpdateAppointmentStatus(int appointmentId, AppointmentStatus status)
        {
            using var context = CreateContext(tracking: true);

            var appointment = context.Appointments.SingleOrDefault(a => a.AppointmentId == appointmentId);
            if (appointment is null)
            {
                return;
            }

            appointment.AppointmentStatus = MapAppointmentStatusToByte(status);
            context.SaveChanges();
            RaiseAppointmentsChanged();
        }
    }
}
