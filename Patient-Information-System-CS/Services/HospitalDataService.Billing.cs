using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Patient_Information_System_CS.Data;
using Patient_Information_System_CS.Models;
using EntityBill = Patient_Information_System_CS.Models.Entities.Bill;
using EntityPerson = Patient_Information_System_CS.Models.Entities.Person;

namespace Patient_Information_System_CS.Services
{
    public sealed partial class HospitalDataService
    {
        public IEnumerable<BillingRecord> GetOutstandingInvoices() =>
            LoadInvoices().Where(invoice => !invoice.IsPaid);

        public IEnumerable<BillingRecord> GetPaidInvoices() =>
            LoadInvoices().Where(invoice => invoice.IsPaid);

        public BillingRecord? GetInvoiceById(int invoiceId) =>
            LoadInvoices().FirstOrDefault(invoice => invoice.InvoiceId == invoiceId);

        public IEnumerable<BillingRecord> GetInvoicesForPatient(int userId)
        {
            using var context = CreateContext();
            var patientId = ResolvePatientId(context, userId);
            if (patientId is null)
            {
                return Array.Empty<BillingRecord>();
            }

            return LoadInvoices().Where(invoice => invoice.PatientId == patientId.Value);
        }

        public void MarkInvoicePaid(BillingRecord invoice)
        {
            using var context = CreateContext(tracking: true);

            var bill = context.Bills.SingleOrDefault(b => b.BillId == invoice.InvoiceId);
            if (bill is null)
            {
                return;
            }

            bill.Status = 1;
            bill.PaymentMethod ??= "Manual";
            context.SaveChanges();
        }

        private IReadOnlyList<BillingRecord> LoadInvoices()
        {
            using var context = CreateContext();

            var bills = context.Bills
                .Include(b => b.AssignedPatient)
                    .ThenInclude(p => p.Person)
                .Include(b => b.AssignedPatient)
                    .ThenInclude(p => p.AssignedDoctor)
                        .ThenInclude(d => d.Person)
                .AsNoTracking()
                .OrderByDescending(b => b.DateBilled)
                .ToList();

            return bills.Select(MapBill).ToList();
        }

        internal static Patient_Information_System_CS.Models.BillingRecord MapBill(EntityBill bill)
        {
            var patientName = bill.AssignedPatient?.Person is EntityPerson patientPerson
                ? FormatFullName(patientPerson)
                : $"Patient #{bill.AssignedPatientId}";

            var doctorName = bill.AssignedPatient?.AssignedDoctor?.Person is EntityPerson doctorPerson
                ? FormatFullName(doctorPerson)
                : "Unassigned";

            return new Patient_Information_System_CS.Models.BillingRecord
            {
                InvoiceId = bill.BillId,
                PatientId = bill.AssignedPatientId,
                PatientName = patientName,
                ContactNumber = bill.AssignedPatient?.Person is EntityPerson person
                    ? FormatContact(person.ContactNumber)
                    : string.Empty,
                Address = bill.AssignedPatient?.Person?.Address ?? string.Empty,
                DoctorId = bill.AssignedPatient?.AssignedDoctorId,
                DoctorName = doctorName,
                AdmitDate = bill.DateBilled,
                ReleaseDate = bill.DateBilled,
                DaysStayed = 0,
                RoomCharge = (int)Math.Round(bill.Amount),
                DoctorFee = 0,
                MedicineCost = 0,
                OtherCharge = 0,
                IsPaid = IsBillPaid(bill),
                PaidDate = IsBillPaid(bill) ? bill.DateBilled : null
            };
        }

        private static int? ResolvePatientId(HospitalDbContext context, int userId)
        {
            var patient = context.Users
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Patients)
                .AsNoTracking()
                .SingleOrDefault(u => u.UserId == userId);

            return patient?.AssociatedPerson?.Patients.FirstOrDefault()?.PatientId;
        }

        private static void MarkInvoicesPaidForPatient(HospitalDbContext context, int patientId)
        {
            var pendingBills = context.Bills
                .Where(b => b.AssignedPatientId == patientId && b.Status != 1)
                .ToList();

            foreach (var bill in pendingBills)
            {
                bill.Status = 1;
                bill.PaymentMethod ??= "Manual";
            }
        }
    }
}
