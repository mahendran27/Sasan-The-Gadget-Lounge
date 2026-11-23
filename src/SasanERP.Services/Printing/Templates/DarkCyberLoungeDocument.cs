using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SasanERP.Core.Models;
using SasanERP.Services.Barcode;

namespace SasanERP.Services.Printing.Templates;

/// <summary>
/// A4 Style 8: Dark Cyber Lounge Invoice (Signature Sasan Tech Aesthetics)
/// Features sleek obsidian & electric cyan headers, neon telemetry badges, and laser-optimized contrast.
/// Delivers vibrant tech-lounge impact in full color, while printing crystal-clear on monochrome lasers.
/// </summary>
public class DarkCyberLoungeDocument : IDocument
{
    private readonly Invoice _invoice;
    private readonly CompanyProfile _company;
    private readonly BarcodeGeneratorService _barcodeService;

    public DarkCyberLoungeDocument(Invoice invoice, CompanyProfile company, BarcodeGeneratorService barcodeService)
    {
        _invoice = invoice;
        _company = company;
        _barcodeService = barcodeService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Invoice_{_invoice.InvoiceNumber}_CyberLounge",
        Author = _company.BusinessName
    };

    public void Compose(IDocumentContainer container)
    {
        bool isBw = _company.IsMonochromePrintMode;
        string darkBannerBg = isBw ? "#1E293B" : "#08101E";
        string cyanAccent = isBw ? "#FFFFFF" : "#00E5FF";
        string amberAccent = isBw ? "#E2E8F0" : "#FF9100";
        byte[]? logoBytes = LogoProvider.GetLogoBytes(isBw, _company.LogoPath);

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(28, Unit.Point);
            page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Segoe UI").FontColor("#0F172A"));

            // 1. CYBER HEADER BANNER
            page.Header().Column(col =>
            {
                col.Item().Background(darkBannerBg).Padding(12).Row(row =>
                {
                    row.RelativeItem(7).Row(r =>
                    {
                        if (logoBytes != null)
                        {
                            r.ConstantItem(50).PaddingRight(12).Image(logoBytes).FitArea();
                        }
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text(t =>
                            {
                                t.Span("SASAN ").FontSize(17).Bold().FontColor(cyanAccent);
                                t.Span("THE GADGET LOUNGE").FontSize(17).Bold().FontColor("#FFFFFF");
                            });
                            c.Item().Text(_company.Tagline).FontSize(8).Italic().FontColor(amberAccent);
                            c.Item().PaddingTop(2).Text($"{_company.AddressLine1}, {_company.AddressLine2}, {_company.City} - {_company.Pincode}")
                                .FontSize(7.5f).FontColor("#CBD5E1");
                            c.Item().Text($"Phone: {_company.PhoneNumber} | Email: {_company.Email}")
                                .FontSize(7.5f).FontColor("#CBD5E1");
                            if (!string.IsNullOrWhiteSpace(_company.GSTIN))
                            {
                                c.Item().Text($"GSTIN: {_company.GSTIN} | State Code: {_company.StateCode} (Tamil Nadu)")
                                    .FontSize(8).Bold().FontColor(cyanAccent);
                            }
                        });
                    });

                    row.RelativeItem(5).AlignRight().Column(c =>
                    {
                        string docType = !_invoice.IsGstEnabled ? "RETAIL CASH MEMO" : "TAX INVOICE";
                        c.Item().Border(1).BorderColor(cyanAccent).Padding(4).AlignCenter().Text(docType).FontSize(11).Bold().FontColor(cyanAccent);
                        c.Item().PaddingTop(4).Text($"INVOICE #: {_invoice.InvoiceNumber}").FontSize(9.5f).Bold().FontColor("#FFFFFF");
                        c.Item().Text($"Date: {_invoice.InvoiceDate:dd MMM yyyy, hh:mm tt}").FontSize(8).FontColor("#CBD5E1");
                        c.Item().Text($"Payment: 100% SETTLED").FontSize(8).Bold().FontColor(isBw ? "#FFFFFF" : "#10B981");
                    });
                });

                col.Item().Height(2.5f).Background(cyanAccent);
            });

            // 2. CONTENT
            page.Content().Column(col =>
            {
                // Customer & VIP Banner
                col.Item().PaddingTop(8).Border(1).BorderColor("#CBD5E1").Padding(8).Row(r =>
                {
                    r.RelativeItem(7).Column(c =>
                    {
                        c.Item().Text("CLIENT INFORMATION").FontSize(7.5f).Bold().FontColor("#64748B");
                        c.Item().Text(_invoice.Customer.FullName).FontSize(10).Bold().FontColor("#0F172A");
                        c.Item().Text($"Mobile: {_invoice.Customer.PhoneNumber}").FontSize(8);
                        if (!string.IsNullOrWhiteSpace(_invoice.Customer.Address))
                            c.Item().Text($"Address: {_invoice.Customer.Address}").FontSize(7.5f);
                    });

                    r.RelativeItem(5).Column(c =>
                    {
                        c.Item().Text("GST & TRANSACTION META").FontSize(7.5f).Bold().FontColor("#64748B");
                        c.Item().Text($"Customer GSTIN: {(!string.IsNullOrWhiteSpace(_invoice.Customer.GSTIN) ? _invoice.Customer.GSTIN : "B2C Consumer")}").FontSize(8).Bold();
                        c.Item().Text($"Place of Supply: State 33 (Tamil Nadu)").FontSize(7.5f);
                    });
                });

                // Items Grid
                col.Item().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(22);
                        cols.RelativeColumn(6.5f);
                        cols.ConstantColumn(42);
                        cols.ConstantColumn(22);
                        cols.ConstantColumn(52);
                        cols.ConstantColumn(50);
                        cols.ConstantColumn(40);
                        cols.ConstantColumn(40);
                        cols.ConstantColumn(62);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Background(darkBannerBg).Padding(4).AlignCenter().Text("#").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background(darkBannerBg).Padding(4).Text("GADGET SPECIFICATION & SERIALS").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background(darkBannerBg).Padding(4).AlignCenter().Text("HSN").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background(darkBannerBg).Padding(4).AlignCenter().Text("QTY").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background(darkBannerBg).Padding(4).AlignRight().Text("RATE").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background(darkBannerBg).Padding(4).AlignRight().Text("TAXABLE").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background(darkBannerBg).Padding(4).AlignRight().Text("CGST").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background(darkBannerBg).Padding(4).AlignRight().Text("SGST").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background(darkBannerBg).Padding(4).AlignRight().Text("TOTAL (₹)").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                    });

                    int idx = 1;
                    foreach (var item in _invoice.Items)
                    {
                        string rowBg = idx % 2 == 1 ? "#FFFFFF" : "#F8FAFC";

                        table.Cell().Background(rowBg).BorderBottom(1).BorderColor("#E2E8F0").Padding(4).AlignCenter().Text(idx.ToString()).FontSize(7.5f);
                        
                        table.Cell().Background(rowBg).BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Column(c =>
                        {
                            c.Item().Text(item.ItemDescription).FontSize(8.5f).Bold();

                            if (item.IsSerialized)
                            {
                                string imeiStr = $"IMEI 1: {item.IMEI1}";
                                if (!string.IsNullOrWhiteSpace(item.IMEI2)) imeiStr += $" | IMEI 2: {item.IMEI2}";
                                if (!string.IsNullOrWhiteSpace(item.SerialNumber)) imeiStr += $" | S/N: {item.SerialNumber}";
                                c.Item().Text(imeiStr).FontSize(7).FontFamily("Consolas").FontColor(isBw ? "#000000" : "#0284C7");

                                string statStr = $"Battery: {item.BatteryHealthPercentage ?? 100}% | Grade: {item.ConditionGrade} | Warranty: {item.WarrantyStatus}";
                                if (item.IsMarginScheme) statStr += " | [Rule 32(5) Margin Scheme]";
                                c.Item().Text(statStr).FontSize(6.8f).FontColor("#475569");
                            }
                        });

                        table.Cell().Background(rowBg).BorderBottom(1).BorderColor("#E2E8F0").Padding(4).AlignCenter().Text(item.HSNCode ?? "-").FontSize(7.5f);
                        table.Cell().Background(rowBg).BorderBottom(1).BorderColor("#E2E8F0").Padding(4).AlignCenter().Text(item.Quantity.ToString()).FontSize(7.5f).Bold();
                        table.Cell().Background(rowBg).BorderBottom(1).BorderColor("#E2E8F0").Padding(4).AlignRight().Text($"₹{item.UnitPrice:N0}").FontSize(7.5f);
                        table.Cell().Background(rowBg).BorderBottom(1).BorderColor("#E2E8F0").Padding(4).AlignRight().Text($"₹{item.TaxableValue:N0}").FontSize(7.5f);
                        table.Cell().Background(rowBg).BorderBottom(1).BorderColor("#E2E8F0").Padding(4).AlignRight().Text($"₹{item.CGSTAmount:N0}").FontSize(7.5f);
                        table.Cell().Background(rowBg).BorderBottom(1).BorderColor("#E2E8F0").Padding(4).AlignRight().Text($"₹{item.SGSTAmount:N0}").FontSize(7.5f);
                        table.Cell().Background(rowBg).BorderBottom(1).BorderColor("#E2E8F0").Padding(4).AlignRight().Text($"₹{item.LineTotal:N0}").FontSize(8).Bold();

                        idx++;
                    }
                });

                // Split Payment & Totals - Unified Card (Zero Stray Vertical Lines)
                col.Item().PaddingTop(8).Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Row(row =>
                {
                    // Left Column: Payment Tender Split & UPI QR
                    row.RelativeItem(7).Padding(8).Row(pr =>
                    {
                        pr.RelativeItem(7).Column(c =>
                        {
                            c.Item().Text("MULTI-MODE SETTLEMENT:").FontSize(7.5f).Bold().FontColor("#475569");
                            if (_invoice.Payments != null && _invoice.Payments.Any())
                            {
                                foreach (var p in _invoice.Payments)
                                {
                                    c.Item().Text($"• {p.Mode}: ₹{p.Amount:N0}").FontSize(7.5f).Bold();
                                }
                            }
                            else
                            {
                                c.Item().Text("• Cash / Direct Payment").FontSize(7.5f);
                            }

                            c.Item().PaddingTop(4).Text("ELECTRONIC REMITTANCE:").FontSize(7.5f).Bold();
                            c.Item().Text($"A/C: {_company.BankAccountNumber} ({_company.BankName})").FontSize(7);
                            c.Item().Text($"IFSC: {_company.BankIFSC} | Branch: {_company.BankBranch}").FontSize(7);
                            c.Item().Text($"UPI ID: {_company.UpiId}").FontSize(7.5f).Bold().FontColor(isBw ? "#000000" : "#0284C7");
                        });

                        pr.RelativeItem(5).AlignRight().Column(qc =>
                        {
                            string upiUri = BarcodeGeneratorService.BuildUpiPaymentUri(
                                _company.UpiId ?? "9080803070@upi",
                                _company.BusinessName,
                                _invoice.GrandTotal,
                                _invoice.InvoiceNumber);
                            byte[] qrBytes = _barcodeService.GenerateQrCode(upiUri, 75);
                            qc.Item().AlignCenter().Image(qrBytes).FitArea();
                            qc.Item().AlignCenter().Text("UPI INSTANT PAY").FontSize(6.5f).Bold();
                        });
                    });

                    row.ConstantItem(1).Background("#E2E8F0");

                    // Right Column: Summary
                    row.RelativeItem(5).Padding(8).Column(tc =>
                    {
                        tc.Item().Row(r => { r.RelativeItem().Text("SUBTOTAL:"); r.RelativeItem().AlignRight().Text($"₹{_invoice.GrossTotal:N2}").Bold(); });
                        if (_invoice.InvoiceDiscount > 0)
                            tc.Item().Row(r => { r.RelativeItem().Text("DISCOUNT:"); r.RelativeItem().AlignRight().Text($"-₹{_invoice.InvoiceDiscount:N2}").FontColor("#DC2626"); });

                        tc.Item().Row(r => { r.RelativeItem().Text("TAXABLE VALUE:"); r.RelativeItem().AlignRight().Text($"₹{_invoice.TotalTaxableValue:N2}"); });
                        tc.Item().Row(r => { r.RelativeItem().Text("CGST:"); r.RelativeItem().AlignRight().Text($"₹{_invoice.TotalCGST:N2}"); });
                        tc.Item().Row(r => { r.RelativeItem().Text("SGST:"); r.RelativeItem().AlignRight().Text($"₹{_invoice.TotalSGST:N2}"); });

                        if (_invoice.HasMarginSchemeItems)
                            tc.Item().Row(r => { r.RelativeItem().Text("RULE 32(5) TAX:").Bold(); r.RelativeItem().AlignRight().Text($"₹{_invoice.TotalMarginTax:N2}").Bold(); });

                        if (_invoice.RoundOff != 0)
                            tc.Item().Row(r => { r.RelativeItem().Text("ROUND OFF:"); r.RelativeItem().AlignRight().Text($"₹{_invoice.RoundOff:N2}"); });

                        tc.Item().PaddingTop(4).BorderTop(1.5f).BorderColor(darkBannerBg).Background(darkBannerBg).Padding(6).Row(r =>
                        {
                            r.RelativeItem().Text("GRAND TOTAL:").FontSize(10).Bold().FontColor("#FFFFFF");
                            r.RelativeItem().AlignRight().Text($"₹{_invoice.GrandTotal:N0}").FontSize(12).Bold().FontColor(cyanAccent);
                        });
                    });
                });
            });

            // 3. FOOTER
            page.Footer().Column(col =>
            {
                col.Item().PaddingTop(6).Border(1).BorderColor("#CBD5E1").Padding(6).Row(r =>
                {
                    r.RelativeItem(8).Column(c =>
                    {
                        c.Item().Text("WARRANTY TERMS & CONDITIONS:").FontSize(7).Bold();
                        c.Item().Text(_company.TermsAndConditions).FontSize(6.2f).FontColor("#475569");
                    });

                    r.RelativeItem(4).AlignRight().Column(c =>
                    {
                        c.Item().AlignCenter().Text($"For {_company.BusinessName.ToUpper()}").FontSize(7.5f).Bold();
                        c.Item().Height(24);
                        c.Item().AlignCenter().Text("AUTHORIZED SIGNATORY").FontSize(7).Bold();
                    });
                });
            });
        });
    }
}
