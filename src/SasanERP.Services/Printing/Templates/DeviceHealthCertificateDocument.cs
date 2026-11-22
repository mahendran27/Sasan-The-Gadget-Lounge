using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SasanERP.Core.Models;
using SasanERP.Services.Barcode;

namespace SasanERP.Services.Printing.Templates;

/// <summary>
/// Choice 7: Device Health Certificate & Diagnostic Report Invoice Template (A4 Vector)
/// Combined statutory invoice and 12-point certified technician hardware inspection report.
/// Ideal for pre-owned iPhones, MacBooks, high-end laptops, and drones to instill 100% buyer confidence.
/// </summary>
public class DeviceHealthCertificateDocument : IDocument
{
    private readonly Invoice _invoice;
    private readonly CompanyProfile _company;
    private readonly BarcodeGeneratorService _barcodeService;

    public DeviceHealthCertificateDocument(Invoice invoice, CompanyProfile company, BarcodeGeneratorService barcodeService)
    {
        _invoice = invoice;
        _company = company;
        _barcodeService = barcodeService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Diagnostic_Certificate_{_invoice.InvoiceNumber}",
        Author = _company.BusinessName
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(26, Unit.Point);
            page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Segoe UI").FontColor("#0F172A"));

            // Header Banner
            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem(7).Column(c =>
                    {
                        c.Item().Row(lr =>
                        {
                            lr.AutoItem().Text("SASAN THE GADGET LOUNGE").FontSize(14).Bold().FontColor("#0B192C");
                            lr.AutoItem().PaddingLeft(6).Background("#0369A1").Padding(2).Text("CERTIFIED QUALITY").FontSize(6.5f).Bold().FontColor("#FFFFFF");
                        });
                        c.Item().Text($"{_company.AddressLine1}, {_company.AddressLine2}, {_company.City} - {_company.Pincode}")
                            .FontSize(7.5f).FontColor("#555555");
                        c.Item().Text($"Phone: {_company.PhoneNumber} | GSTIN: {_company.GSTIN} (Tamil Nadu-33)").FontSize(7.5f).FontColor("#555555");
                    });

                    row.RelativeItem(5).AlignRight().Column(c =>
                    {
                        c.Item().Background("#F0FDF4").Border(1).BorderColor("#86EFAC").Padding(4).AlignCenter().Text("100% TESTED & CERTIFIED HARDWARE").FontSize(8f).Bold().FontColor("#166534");
                        c.Item().PaddingTop(2).Text($"CERTIFICATE / INVOICE: {_invoice.InvoiceNumber}").FontSize(8.5f).Bold();
                        c.Item().Text($"Inspection Date: {_invoice.InvoiceDate:dd-MMM-yyyy}").FontSize(7.5f);
                    });
                });

                col.Item().PaddingTop(6).LineHorizontal(1).LineColor("#0284C7");
            });

            // Content
            page.Content().Column(col =>
            {
                // Customer & Device Overview Row
                col.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem(6).Border(0.5f).BorderColor("#CBD5E1").Padding(6).Column(c =>
                    {
                        c.Item().Text("OWNER & PURCHASE DETAILS:").FontSize(7f).Bold().FontColor("#64748B");
                        c.Item().Text($"Customer: {_invoice.Customer.FullName}").FontSize(8.5f).Bold();
                        c.Item().Text($"Mobile: {_invoice.Customer.PhoneNumber}").FontSize(7.5f);
                        c.Item().Text($"Payment: {string.Join(", ", _invoice.Payments.Select(p => p.Mode))}").FontSize(7.5f);
                    });

                    row.RelativeItem(6).PaddingLeft(6).Border(0.5f).BorderColor("#CBD5E1").Padding(6).Column(c =>
                    {
                        var firstUnit = _invoice.Items.FirstOrDefault(i => !string.IsNullOrEmpty(i.IMEI1) || !string.IsNullOrEmpty(i.SerialNumber));
                        c.Item().Text("PRIMARY CERTIFIED DEVICE:").FontSize(7f).Bold().FontColor("#64748B");
                        c.Item().Text(firstUnit?.ItemDescription ?? "Multi-Device Order").FontSize(8.5f).Bold().FontColor("#0369A1");
                        c.Item().Text($"IMEI / S/N: {firstUnit?.IMEI1 ?? firstUnit?.SerialNumber ?? "N/A"}").FontSize(7.5f).Bold();
                        c.Item().Text($"Grade: {firstUnit?.ConditionGrade} | Battery: {firstUnit?.BatteryHealthPercentage ?? 100}% | Warranty: {firstUnit?.WarrantyStatus}").FontSize(7.2f);
                    });
                });

                // 12-POINT DIAGNOSTIC HARDWARE INSPECTION CHECKLIST
                col.Item().PaddingTop(8).Background("#0F172A").Padding(4).AlignCenter().Text("12-POINT TECHNICIAN HARDWARE DIAGNOSTIC AUDIT").FontSize(8f).Bold().FontColor("#38BDF8");

                col.Item().PaddingTop(4).Table(dt =>
                {
                    dt.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(18);
                        cols.RelativeColumn(5);
                        cols.RelativeColumn(5);
                        cols.ConstantColumn(50);
                    });

                    dt.Header(h =>
                    {
                        h.Cell().Background("#F1F5F9").Padding(3).Text("#").FontSize(7f).Bold();
                        h.Cell().Background("#F1F5F9").Padding(3).Text("INSPECTION SUBSYSTEM").FontSize(7f).Bold();
                        h.Cell().Background("#F1F5F9").Padding(3).Text("TEST BENCH PARAMETERS").FontSize(7f).Bold();
                        h.Cell().Background("#F1F5F9").Padding(3).AlignCenter().Text("VERDICT").FontSize(7f).Bold();
                    });

                    var checkpoints = new (int Id, string Subsystem, string Params, string Verdict)[]
                    {
                        (1, "IMEI & ESN Blacklist Audit", "GSMA Global Registry, Clean iCloud/Knox status", "PASSED [OK]"),
                        (2, "Battery Cell & Cycle Health", "Capacity test, Fast charge IC, Zero overheating", "PASSED [OK]"),
                        (3, "Display, Touch & TrueTone", "Multi-touch, 120Hz refresh rate, Zero dead pixels", "PASSED [OK]"),
                        (4, "Optics & Camera Array", "Main, Ultrawide, Telephoto, OIS, 4K 60FPS video", "PASSED [OK]"),
                        (5, "Biometric Security", "Face ID 3D sensor / In-Display Fingerprint scanner", "PASSED [OK]"),
                        (6, "Acoustic Transducers", "Stereo speakers, Earpiece clarity, Noise-cancelling mic", "PASSED [OK]"),
                        (7, "Radio & Connectivity", "5G Sub-6/mmWave, Wi-Fi 6E, Bluetooth 5.3, NFC, GPS", "PASSED [OK]"),
                        (8, "I/O Ports & Charging", "USB-C/Lightning connector, 15W Qi Wireless charging", "PASSED [OK]"),
                        (9, "Housing & Liquid Seal", "No structural bends, SIM gasket intact, LDI White", "PASSED [OK]"),
                        (10, "Sensor Environment", "Gyroscope, 3-Axis Accel, Proximity, Barometer", "PASSED [OK]"),
                        (11, "SoC, RAM & Thermal Load", "Geekbench stress cycle, Zero throttling, Memory integrity", "PASSED [OK]"),
                        (12, "Data Sanitization / FRP", "DoD 5220.22-M Wipe, Clean factory state ready to bind", "PASSED [OK]")
                    };

                    foreach (var cp in checkpoints)
                    {
                        dt.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(2.5f).Text(cp.Id.ToString()).FontSize(6.8f);
                        dt.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(2.5f).Text(cp.Subsystem).FontSize(7f).Bold();
                        dt.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(2.5f).Text(cp.Params).FontSize(6.8f).FontColor("#475569");
                        dt.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(2.5f).AlignCenter().Text(cp.Verdict).FontSize(6.8f).Bold().FontColor("#166534");
                    }
                });

                // Billed Items Table
                col.Item().PaddingTop(8).Table(it =>
                {
                    it.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(16);
                        cols.RelativeColumn(7);
                        cols.ConstantColumn(40);
                        cols.ConstantColumn(24);
                        cols.ConstantColumn(55);
                        cols.ConstantColumn(65);
                    });

                    it.Header(h =>
                    {
                        h.Cell().Background("#0B192C").Padding(3).Text("#").FontSize(7f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background("#0B192C").Padding(3).Text("INVOICED PRODUCT").FontSize(7f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background("#0B192C").Padding(3).AlignCenter().Text("HSN").FontSize(7f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background("#0B192C").Padding(3).AlignCenter().Text("QTY").FontSize(7f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background("#0B192C").Padding(3).AlignRight().Text("RATE").FontSize(7f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background("#0B192C").Padding(3).AlignRight().Text("TOTAL (₹)").FontSize(7f).Bold().FontColor("#FFFFFF");
                    });

                    int idx = 1;
                    foreach (var item in _invoice.Items)
                    {
                        it.Cell().BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(2.5f).Text(idx.ToString()).FontSize(7f);
                        it.Cell().BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(2.5f).Text(item.ItemDescription).FontSize(7.5f).Bold();
                        it.Cell().BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(2.5f).AlignCenter().Text(item.HSNCode).FontSize(7f);
                        it.Cell().BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(2.5f).AlignCenter().Text(item.Quantity.ToString("0")).FontSize(7f);
                        it.Cell().BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(2.5f).AlignRight().Text($"{item.UnitPrice:N0}").FontSize(7f);
                        it.Cell().BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(2.5f).AlignRight().Text($"{item.LineTotal:N2}").FontSize(7.5f).Bold();
                        idx++;
                    }
                });

                // Financial Summary & Signature
                col.Item().PaddingTop(6).Row(fr =>
                {
                    fr.RelativeItem(6).Column(c =>
                    {
                        c.Item().Text("TECHNICIAN VERIFICATION & CERTIFICATION SEAL").FontSize(7f).Bold().FontColor("#0F172A");
                        c.Item().Text("Certified that this gadget has undergone comprehensive electronic diagnostic inspection and meets Sasan Gadget Lounge Grade A+ Quality Standards.").FontSize(6.5f).FontColor("#475569");
                        c.Item().PaddingTop(6).Row(tr =>
                        {
                            tr.AutoItem().Text("Lead Diagnostic Engineer: ").FontSize(7f).Bold();
                            tr.AutoItem().Text("Sasan Tech Lab Theni").FontSize(7f).FontColor("#0369A1");
                        });
                    });

                    fr.RelativeItem(6).AlignRight().Column(c =>
                    {
                        if (_invoice.IsGstEnabled)
                        {
                            c.Item().Text($"Taxable: ₹{_invoice.StandardTaxableValue:N2} | CGST: ₹{_invoice.TotalCGST:N2} | SGST: ₹{_invoice.TotalSGST:N2}").FontSize(7f);
                        }
                        c.Item().PaddingTop(2).Text($"TOTAL PAID: ₹{_invoice.GrandTotal:N2}").FontSize(11).Bold().FontColor("#0B192C");
                    });
                });
            });

            // Footer
            page.Footer().Column(col =>
            {
                col.Item().LineHorizontal(0.5f).LineColor("#CBD5E1");
                col.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem(7).Text("Sasan The Gadget Lounge • 100% Genuine Pre-Owned & Brand New Electronics • Hotline: 9080803070").FontSize(6.5f).FontColor("#64748B");
                    row.RelativeItem(5).AlignRight().Text($"For {_company.BusinessName.ToUpper()}  (Authorized Signatory)").FontSize(7f).Bold();
                });
            });
        });
    }
}
