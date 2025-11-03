using System;
using System.Globalization;
using Patient_Information_System_CS.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Patient_Information_System_CS.Services;

public sealed class PdfExportService
{
    public static PdfExportService Instance { get; } = new();

    static PdfExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private PdfExportService()
    {
    }

    public void ExportInvoice(BillingRecord invoice, string filePath)
    {
        if (invoice is null)
        {
            throw new ArgumentNullException(nameof(invoice));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A valid file path is required.", nameof(filePath));
        }

        var currency = CultureInfo.CurrentCulture;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2.5f, Unit.Centimetre);
                page.Header().Column(header =>
                {
                    header.Spacing(4);
                    header.Item().Text("Patient Information System")
                        .FontSize(20)
                        .Bold();
                    header.Item().Text($"Invoice #{invoice.InvoiceId}")
                        .FontSize(16)
                        .SemiBold()
                        .FontColor(Colors.BlueGrey.Darken2);
                    header.Item().Text($"Generated: {DateTime.Now:MMMM dd, yyyy HH:mm}")
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken1);
                });

                page.Content().Column(content =>
                {
                    content.Spacing(16);

                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell().Element(SectionLabel).Text("Patient").Bold();
                        table.Cell().Element(SectionValue).Text(invoice.PatientName);

                        table.Cell().Element(SectionLabel).Text("Doctor").Bold();
                        table.Cell().Element(SectionValue).Text(string.IsNullOrWhiteSpace(invoice.DoctorName) ? "Unassigned" : invoice.DoctorName);

                        table.Cell().Element(SectionLabel).Text("Contact").Bold();
                        table.Cell().Element(SectionValue).Text(JoinLines(invoice.ContactNumber, invoice.Address));

                        table.Cell().Element(SectionLabel).Text("Admit Date").Bold();
                        table.Cell().Element(SectionValue).Text(invoice.AdmitDate.ToString("MMM dd, yyyy"));

                        table.Cell().Element(SectionLabel).Text("Release Date").Bold();
                        table.Cell().Element(SectionValue).Text(invoice.ReleaseDate.ToString("MMM dd, yyyy"));

                        table.Cell().Element(SectionLabel).Text("Length of Stay").Bold();
                        table.Cell().Element(SectionValue).Text(invoice.DaysStayed == 1 ? "1 day" : $"{invoice.DaysStayed} days");

                        table.Cell().Element(SectionLabel).Text("Status").Bold();
                        var status = invoice.IsPaid ? "Paid" : "Outstanding";
                        table.Cell().Element(SectionValue).Text(invoice.IsPaid && invoice.PaidDate.HasValue
                            ? $"{status} ({invoice.PaidDate.Value:MMM dd, yyyy})"
                            : status);
                    });

                    content.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten3);

                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Charge");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Amount");
                        });

                        AddChargeRow(table, "Room Charge", invoice.RoomCharge, currency);
                        AddChargeRow(table, "Doctor Fee", invoice.DoctorFee, currency);
                        AddChargeRow(table, "Medicine Cost", invoice.MedicineCost, currency);
                        AddChargeRow(table, "Other Charges", invoice.OtherCharge, currency);

                        table.Cell().ColumnSpan(2).PaddingVertical(4);
                        table.Cell().Element(FooterCell).Text("Total").Bold();
                        table.Cell().Element(FooterCell).AlignRight().Text(invoice.Total.ToString("C", currency)).Bold();
                    });

                    if (!string.IsNullOrWhiteSpace(invoice.Notes))
                    {
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                            });

                            table.Cell().Element(SectionLabel).Text("Notes").Bold();
                            table.Cell().Element(SectionValue).Text(invoice.Notes);
                        });
                    }
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf(filePath);
    }

    public void ExportMedicalRecord(MedicalRecordEntry record, string filePath)
    {
        if (record is null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A valid file path is required.", nameof(filePath));
        }

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2.5f, Unit.Centimetre);
                page.Header().Column(header =>
                {
                    header.Spacing(4);
                    header.Item().Text("Patient Information System")
                        .FontSize(20)
                        .Bold();
                    header.Item().Text("Medical Record")
                        .FontSize(16)
                        .SemiBold()
                        .FontColor(Colors.BlueGrey.Darken2);
                    header.Item().Text($"Record #: {record.RecordNumber}")
                        .FontSize(12)
                        .FontColor(Colors.Grey.Darken1);
                    header.Item().Text($"Generated: {DateTime.Now:MMMM dd, yyyy HH:mm}")
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken1);
                });

                page.Content().Column(content =>
                {
                    content.Spacing(16);

                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell().Element(SectionLabel).Text("Patient").Bold();
                        table.Cell().Element(SectionValue).Text(record.PatientName);

                        table.Cell().Element(SectionLabel).Text("Doctor").Bold();
                        table.Cell().Element(SectionValue).Text(record.DoctorName);

                        table.Cell().Element(SectionLabel).Text("Recorded On").Bold();
                        table.Cell().Element(SectionValue).Text(record.RecordedOn.ToString("MMM dd, yyyy"));

                        table.Cell().Element(SectionLabel).Text("Record ID").Bold();
                        table.Cell().Element(SectionValue).Text(record.RecordId.ToString(CultureInfo.InvariantCulture));
                    });

                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                        });

                        table.Cell().Element(SectionLabel).Text("Diagnosis").Bold();
                        table.Cell().Element(SectionValue).Text(record.Diagnosis);

                        table.Cell().Element(SectionLabel).PaddingTop(12).Text("Treatment Plan").Bold();
                        table.Cell().Element(SectionValue).Text(record.Treatment);

                        table.Cell().Element(SectionLabel).PaddingTop(12).Text("Prescriptions").Bold();
                        table.Cell().Element(SectionValue).Text(string.IsNullOrWhiteSpace(record.Prescriptions) ? "None provided" : record.Prescriptions);
                    });
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf(filePath);
    }

    private static void AddChargeRow(TableDescriptor table, string label, decimal amount, CultureInfo culture)
    {
        table.Cell().Element(RowCell).Text(label);
        table.Cell().Element(RowCell).AlignRight().Text(amount.ToString("C", culture));
    }

    private static IContainer SectionLabel(IContainer container) => container
        .PaddingVertical(2)
        .DefaultTextStyle(TextStyle.Default.FontSize(11).SemiBold());

    private static IContainer SectionValue(IContainer container) => container
        .PaddingVertical(2)
        .DefaultTextStyle(TextStyle.Default.FontSize(11));

    private static IContainer HeaderCell(IContainer container) => container
        .PaddingVertical(4)
        .DefaultTextStyle(TextStyle.Default.FontSize(12).SemiBold())
        .BorderBottom(1)
        .BorderColor(Colors.Grey.Lighten2);

    private static IContainer RowCell(IContainer container) => container
        .PaddingVertical(4)
        .DefaultTextStyle(TextStyle.Default.FontSize(11));

    private static IContainer FooterCell(IContainer container) => container
        .PaddingVertical(4)
        .BorderTop(1)
        .BorderColor(Colors.Grey.Lighten2)
        .DefaultTextStyle(TextStyle.Default.FontSize(12).SemiBold());

    private static string JoinLines(params string?[] lines)
    {
        return string.Join(Environment.NewLine, lines ?? Array.Empty<string?>()).Trim();
    }
}
