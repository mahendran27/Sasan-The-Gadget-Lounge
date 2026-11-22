using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SasanERP.Core.Models;
using SasanERP.Services.Barcode;

namespace SasanERP.Services.Printing.Templates;

/// <summary>
/// Choice 5: Dual-Copy Counterfoil Invoice Template (A4 Vector)
/// Designed for retail compliance, buyback exchanges, and pre-owned device warranty verification.
/// Top half: Original Customer Tax Invoice.
/// Perforation cut-line.
/// Bottom half: Signed Store Warranty Counterfoil with customer legal declaration and device inspection acknowledgment.
/// </summary>
public class DualCopyCounterfoilDocument : IDocument
{
    private readonly Invoice _invoice;
    private readonly CompanyProfile _company;
    private readonly BarcodeGeneratorService _barcodeService;

    public DualCopyCounterfoilDocument(Invoice invoice, CompanyProfile company, BarcodeGeneratorService barcodeService)
    {
        _invoice = invoice;
        _company = company;
        _barcodeService = barcodeService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"DualCopy_Counterfoil_{_invoice.InvoiceNumber}",
        Author = _company.BusinessName
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(20, Unit.Point);
            page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Segoe UI").FontColor("#0F172A"));

            bool isBw = _company.IsMonochromePrintMode;
            byte[]? logoBytes = LogoProvider.GetLogoBytes(isBw, _company.LogoPath);

            page.Content().Column(col =>
            {
                // ==========================================
                // SECTION 1: TOP HALF - BUYER ORIGINAL COPY
                // ==========================================
                col.Item().Border(1).BorderColor("#CBD5E1").Padding(10).Column(top =>
                {
                    // Header
                    top.Item().Row(r =>
                    {
                        if (logoBytes != null)
                        {
                            r.ConstantItem(40).PaddingRight(8).Image(logoBytes).FitArea();
                        }
                        r.RelativeItem(7).Column(c =>
                        {
                            c.Item().Row(logo =>
                            {
                                logo.AutoItem().Text("SASAN THE GADGET LOUNGE").FontSize(13).Bold().FontColor("#0B192C");
                                logo.AutoItem().PaddingLeft(6).Background("#0B192C").Padding(2).Text("ORIGINAL FOR BUYER").FontSize(6.5f).Bold().FontColor("#FFFFFF");
                            });
                            c.Item().Text($"{_company.AddressLine1}, {_company.AddressLine2}, {_company.City} | Tel: {_company.PhoneNumber}").FontSize(7.2f).FontColor("#555555");
                            if (!string.IsNullOrWhiteSpace(_company.GSTIN) && _invoice.IsGstEnabled)
                                c.Item().Text($"GSTIN: {_company.GSTIN} | State: 33-Tamil Nadu").FontSize(7.2f).FontColor("#555555");
                        });

                        r.RelativeItem(5).AlignRight().Column(c =>
                        {
                            string billType = !_invoice.IsGstEnabled ? "RETAIL BILL" : "TAX INVOICE";
                            c.Item().Text(billType).FontSize(11).Bold();
                            c.Item().Text($"Bill #: {_invoice.InvoiceNumber}").FontSize(8.5f).Bold();
                            c.Item().Text($"Date: {_invoice.InvoiceDate:dd-MMM-yyyy}").FontSize(8f);
                        });
                    });

                    top.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor("#E2E8F0");

                    // Customer Row
                    top.Item().PaddingTop(4).Row(cr =>
                    {
                        cr.RelativeItem().Text($"Buyer: {_invoice.Customer.FullName} | Phone: {_invoice.Customer.PhoneNumber}").Bold();
                        if (!string.IsNullOrWhiteSpace(_invoice.Customer.GSTIN))
                            cr.AutoItem().Text($"GSTIN: {_invoice.Customer.GSTIN}").Bold();
                    });

                    // Item Table
                    top.Item().PaddingTop(4).Table(tbl =>
                    {
                        tbl.ColumnsDefinition(cd =>
                        {
                            cd.ConstantColumn(16);
                            cd.RelativeColumn(7);
                            cd.ConstantColumn(40);
                            cd.ConstantColumn(24);
                            cd.ConstantColumn(55);
                            cd.ConstantColumn(65);
                        });

                        tbl.Header(h =>
                        {
                            h.Cell().Background("#F1F5F9").Padding(2).Text("#").FontSize(7f).Bold();
                            h.Cell().Background("#F1F5F9").Padding(2).Text("ITEM / IMEI").FontSize(7f).Bold();
                            h.Cell().Background("#F1F5F9").Padding(2).AlignCenter().Text("HSN").FontSize(7f).Bold();
                            h.Cell().Background("#F1F5F9").Padding(2).AlignCenter().Text("QTY").FontSize(7f).Bold();
                            h.Cell().Background("#F1F5F9").Padding(2).AlignRight().Text("RATE").FontSize(7f).Bold();
                            h.Cell().Background("#F1F5F9").Padding(2).AlignRight().Text("TOTAL (₹)").FontSize(7f).Bold();
                        });

                        int idx = 1;
                        foreach (var itm in _invoice.Items)
                        {
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(2).Text(idx.ToString()).FontSize(7f);
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(2).Column(c =>
                            {
                                c.Item().Text(itm.ItemDescription).FontSize(7.5f).Bold();
                                if (!string.IsNullOrEmpty(itm.IMEI1) || !string.IsNullOrEmpty(itm.SerialNumber))
                                {
                                    c.Item().Text($"IMEI: {itm.IMEI1 ?? itm.SerialNumber} | Grade: {itm.ConditionGrade} | Bat: {itm.BatteryHealthPercentage}% | War: {itm.WarrantyStatus}")
                                        .FontSize(6.5f).FontColor("#475569");
                                }
                            });
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(2).AlignCenter().Text(itm.HSNCode).FontSize(7f);
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(2).AlignCenter().Text(itm.Quantity.ToString("0")).FontSize(7f);
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(2).AlignRight().Text($"{itm.UnitPrice:N0}").FontSize(7f);
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(2).AlignRight().Text($"{itm.LineTotal:N2}").FontSize(7.5f).Bold();
                            idx++;
                        }
                    });

                    // Summary Row
                    top.Item().PaddingTop(4).Row(sr =>
                    {
                        sr.RelativeItem(6).Column(c =>
                        {
                            c.Item().Text($"Payment: {string.Join(", ", _invoice.Payments.Select(p => $"{p.Mode}: ₹{p.Amount:N0}"))}").FontSize(7f);
                            c.Item().Text("Warranty valid upon presenting this bill. Physical damage void.").FontSize(6.5f).FontColor("#64748B");
                        });

                        sr.RelativeItem(6).AlignRight().Column(c =>
                        {
                            if (_invoice.IsGstEnabled)
                            {
                                c.Item().Text($"Taxable: ₹{_invoice.StandardTaxableValue:N2} | CGST: ₹{_invoice.TotalCGST:N2} | SGST: ₹{_invoice.TotalSGST:N2}").FontSize(6.8f);
                            }
                            c.Item().Text($"NET TOTAL: ₹{_invoice.GrandTotal:N2}").FontSize(10).Bold().FontColor("#0B192C");
                        });
                    });
                });

                // ==========================================
                // PERFORATION CUT LINE
                // ==========================================
                col.Item().PaddingVertical(8).Row(pr =>
                {
                    pr.RelativeItem().LineHorizontal(0.5f).LineColor("#94A3B8");
                    pr.AutoItem().PaddingHorizontal(6).Text("- - - - - - - - - ✂  TEAR HERE (SHOP WARRANTY & UNDERTAKING COUNTERFOIL)  ✂ - - - - - - - - -").FontSize(6.5f).Bold().FontColor("#64748B");
                    pr.RelativeItem().LineHorizontal(0.5f).LineColor("#94A3B8");
                });

                // ==========================================
                // SECTION 2: BOTTOM HALF - STORE COUNTERFOIL
                // ==========================================
                col.Item().Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(10).Column(bot =>
                {
                    bot.Item().Row(r =>
                    {
                        r.RelativeItem(8).Column(c =>
                        {
                            c.Item().Row(mr =>
                            {
                                mr.AutoItem().Text("SASAN THE GADGET LOUNGE").FontSize(11).Bold().FontColor("#0B192C");
                                mr.AutoItem().PaddingLeft(6).Background("#B45309").Padding(2).Text("STORE RETENTION COUNTERFOIL").FontSize(6.5f).Bold().FontColor("#FFFFFF");
                            });
                            c.Item().Text($"Reference Invoice: {_invoice.InvoiceNumber} | Date: {_invoice.InvoiceDate:dd-MMM-yyyy}").FontSize(7.5f).Bold();
                        });

                        r.RelativeItem(4).AlignRight().Column(c =>
                        {
                            c.Item().Text($"Amount Paid: ₹{_invoice.GrandTotal:N2}").FontSize(9).Bold().FontColor("#0B192C");
                        });
                    });

                    bot.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor("#CBD5E1");

                    // Customer and Unit Details Grid
                    bot.Item().PaddingTop(6).Row(r =>
                    {
                        r.RelativeItem(6).Column(c =>
                        {
                            c.Item().Text("CUSTOMER VERIFICATION:").FontSize(7.5f).Bold().FontColor("#334155");
                            c.Item().Text($"Name: {_invoice.Customer.FullName}").FontSize(7.5f);
                            c.Item().Text($"Mobile: {_invoice.Customer.PhoneNumber}").FontSize(7.5f);
                            c.Item().Text($"Address: {_invoice.Customer.Address ?? "Theni Branch Walk-in"}").FontSize(7.5f);
                        });

                        r.RelativeItem(6).Column(c =>
                        {
                            c.Item().Text("REGISTERED SERIAL / IMEI UNITS:").FontSize(7.5f).Bold().FontColor("#334155");
                            foreach (var itm in _invoice.Items.Where(i => !string.IsNullOrEmpty(i.IMEI1) || !string.IsNullOrEmpty(i.SerialNumber)))
                            {
                                c.Item().Text($"• {itm.ItemDescription}").FontSize(7.5f).Bold();
                                c.Item().Text($"  IMEI: {itm.IMEI1 ?? itm.SerialNumber} | Grade: {itm.ConditionGrade} | Bat: {itm.BatteryHealthPercentage}%").FontSize(7f);
                            }
                        });
                    });

                    // Customer Undertaking Legal Declaration Box
                    bot.Item().PaddingTop(6).Background("#FFFFFF").Border(0.5f).BorderColor("#CBD5E1").Padding(6).Column(uc =>
                    {
                        uc.Item().Text("CUSTOMER DECLARATION & WARRANTY UNDERTAKING:").FontSize(7f).Bold().FontColor("#0F172A");
                        uc.Item().Text(
                            "1. I have physically inspected the above gadget(s), confirmed display touch, camera, speakers, and battery health in operating condition at the counter.\n" +
                            "2. In case of pre-owned devices, I verify that iCloud, Google Account, and locks have been signed out in my presence.\n" +
                            "3. For exchange buybacks, I solemnly affirm that I am the sole and lawful owner of the traded gadget and have provided valid ID proof.\n" +
                            "4. I understand that physical, liquid, and high-voltage surge damages are strictly excluded from shop warranty claims.").FontSize(6.2f).FontColor("#475569");
                    });

                    // Signatures
                    bot.Item().PaddingTop(12).Row(sr =>
                    {
                        sr.RelativeItem(6).Column(c =>
                        {
                            c.Item().LineHorizontal(0.5f).LineColor("#64748B");
                            c.Item().PaddingTop(2).Text("Customer Signature & Date").FontSize(7f).Bold();
                        });

                        sr.RelativeItem(6).AlignRight().Column(c =>
                        {
                            c.Item().LineHorizontal(0.5f).LineColor("#64748B");
                            c.Item().PaddingTop(2).Text($"Store Representative / Manager ({_company.BusinessName})").FontSize(7f).Bold();
                        });
                    });
                });
            });
        });
    }
}
