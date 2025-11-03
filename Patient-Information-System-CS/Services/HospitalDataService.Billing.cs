using System;
using System.Collections.Generic;
using System.Globalization;
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

            var breakdown = ParseBillDescription(bill.Description, bill.Amount);
            var isPaid = IsBillPaid(bill);

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
                RoomCharge = breakdown.RoomCharge,
                DoctorFee = breakdown.DoctorFee,
                MedicineCost = breakdown.MedicineCost,
                OtherCharge = breakdown.OtherCharges,
                Notes = breakdown.Notes,
                IsPaid = isPaid,
                PaidDate = isPaid ? bill.DateBilled : null
            };
        }

        private static BillBreakdown ParseBillDescription(string? description, decimal totalAmount)
        {
            var breakdown = new BillBreakdown();

            if (!string.IsNullOrWhiteSpace(description))
            {
                var segments = description.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var noteSegments = new List<string>();

                foreach (var segment in segments)
                {
                    if (TryExtractCharge(segment, "Room", out var room))
                    {
                        breakdown.RoomCharge += room;
                        continue;
                    }

                    if (TryExtractCharge(segment, "Doctor", out var doctor))
                    {
                        breakdown.DoctorFee += doctor;
                        continue;
                    }

                    if (TryExtractCharge(segment, "Medicine", out var medicine))
                    {
                        breakdown.MedicineCost += medicine;
                        continue;
                    }

                    if (TryExtractCharge(segment, "Other", out var other))
                    {
                        breakdown.OtherCharges += other;
                        continue;
                    }

                    noteSegments.Add(segment.Trim());
                }

                if (noteSegments.Count > 0)
                {
                    breakdown.Notes = string.Join("; ", noteSegments).Trim();
                }
            }

            if (!breakdown.HasCharges && totalAmount > 0m)
            {
                breakdown.RoomCharge = totalAmount;
            }

            if (!breakdown.HasCharges && string.IsNullOrWhiteSpace(breakdown.Notes) && !string.IsNullOrWhiteSpace(description))
            {
                breakdown.Notes = description.Trim();
            }

            return breakdown;
        }

        private static bool TryExtractCharge(string segment, string label, out decimal value)
        {
            var prefix = $"{label}:";
            if (segment.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var amountPart = segment[prefix.Length..].Trim();
                if (decimal.TryParse(amountPart, NumberStyles.Currency | NumberStyles.Number, CultureInfo.CurrentCulture, out value))
                {
                    return true;
                }

                if (decimal.TryParse(amountPart, NumberStyles.Currency | NumberStyles.Number, CultureInfo.InvariantCulture, out value))
                {
                    return true;
                }
            }

            value = 0m;
            return false;
        }

        private sealed class BillBreakdown
        {
            public decimal RoomCharge { get; set; }
            public decimal DoctorFee { get; set; }
            public decimal MedicineCost { get; set; }
            public decimal OtherCharges { get; set; }
            public string Notes { get; set; } = string.Empty;
            public bool HasCharges => RoomCharge > 0m || DoctorFee > 0m || MedicineCost > 0m || OtherCharges > 0m;
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
