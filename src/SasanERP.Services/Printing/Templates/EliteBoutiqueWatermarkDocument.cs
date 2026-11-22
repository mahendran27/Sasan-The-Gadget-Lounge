using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SasanERP.Core.Models;
using SasanERP.Services.Barcode;

namespace SasanERP.Services.Printing.Templates;

/// <summary>
/// A4 Style 10: Elite Boutique Watermark Invoice (High-End Luxury Gadget Retail)
/// Features double-hairline ornamental borders, prestige serif/sans-serif typography, and refined diagnostic cards.
/// Delivers an ultra-premium aesthetic in full color and impeccable contrast on black-and-white laser printers.
/// </summary>
public class EliteBoutiqueWatermarkDocument : IDocument
{
    private readonly Invoice _invoice;
    private readonly CompanyProfile _company;
    private readonly BarcodeGeneratorService _barcodeService;

    public EliteBoutiqueWatermarkDocument(Invoice invoice, CompanyProfile company, BarcodeGeneratorService barcodeService)
    {
        _invoice = invoice;
        _company = company;
        _barcodeService = barcodeService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Invoice_{_invoice.InvoiceNumber}_EliteBoutique",
        Author = _company.BusinessName
    };

    public void Compose(IDocumentContainer container)
    {
        bool isBw = _company.IsMonochromePrintMode;
        string frameColor = isBw ? "#000000" : "#D4AF37"; // Gold or Black
        string primaryText = "#0F172A";
        string headerBg = isBw ? "#1E293B" : "#0F172A";
        byte[]? logoBytes = LogoProvider.GetLogoBytes(isBw, _company.LogoPath);

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(28, Unit.Point);
            page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Segoe UI").FontColor(primaryText));

            // Outer Boutique Double Border Frame
            page.Header().Column(col =>
            {
                col.Item().Border(1.5f).BorderColor(frameColor).Padding(3).Border(0.5f).BorderColor(frameColor).Padding(8).Row(row =>
                {
                    row.RelativeItem(7).Row(r =>
                    {
                        if (logoBytes != null)
                        {
                            r.ConstantItem(50).PaddingRight(10).Image(logoBytes).FitArea();
                        }
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text(_company.BusinessName.ToUpper()).FontSize(15).Bold().FontColor(primaryText);
                            c.Item().Text(_company.Tagline).FontSize(8).Italic().FontColor(isBw ? "#475569" : "#B45309");
                            c.Item().PaddingTop(2).Text($"{_company.AddressLine1}, {_company.AddressLine2}, {_company.City} - {_company.Pincode}")
                                .FontSize(7.5f).FontColor("#475569");
                            c.Item().Text($"GSTIN: {_company.GSTIN} | Tel: {_company.PhoneNumber}")
                                .FontSize(7.5f).Bold();
                        });
                    });

                    row.RelativeItem(5).AlignRight().Column(c =>
                    {
                        string docTitle = !_invoice.IsGstEnabled ? "RETAIL CERTIFICATE / BILL" : "TAX INVOICE";
                        c.Item().Background(headerBg).Padding(4).AlignCenter().Text(docTitle).FontSize(10.5f).Bold().FontColor("#FFFFFF");
                        c.Item().PaddingTop(4).Text($"INVOICE #: {_invoice.InvoiceNumber}").FontSize(9).Bold();
                        c.Item().Text($"DATE: {_invoice.InvoiceDate:dd-MMM-yyyy, hh:mm tt}").FontSize(7.8f);
                        c.Item().Text("BOUTIQUE PRESTIGE EDITION").FontSize(7).Italic().FontColor(isBw ? "#64748B" : "#D4AF37");
                    });
                });
            });

            // CONTENT
            page.Content().Column(col =>
            {
                // Customer & VIP Banner
                col.Item().PaddingTop(8).Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(8).Row(r =>
                {
                    r.RelativeItem(7).Column(c =>
                    {
                        c.Item().Text("PATRON / CLIENT DETAILS:").FontSize(7.5f).Bold().FontColor("#64748B");
                        c.Item().Text(_invoice.Customer.FullName).FontSize(10).Bold();
                        c.Item().Text($"Mobile: {_invoice.Customer.PhoneNumber}").FontSize(8);
                        if (!string.IsNullOrWhiteSpace(_invoice.Customer.Address))
                            c.Item().Text($"Address: {_invoice.Customer.Address}").FontSize(7.5f);
                    });

                    r.RelativeItem(5).Column(c =>
                    {
                        c.Item().Text("STATUTORY META:").FontSize(7.5f).Bold().FontColor("#64748B");
                        c.Item().Text($"Customer GSTIN: {(!string.IsNullOrWhiteSpace(_invoice.Customer.GSTIN) ? _invoice.Customer.GSTIN : "Consumer / Unregistered")}").FontSize(8).Bold();
                        c.Item().Text("Jurisdiction: 33-Tamil Nadu").FontSize(7.5f);
                    });
                });

                // Items Table
                col.Item().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(20);
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
                        h.Cell().Background(headerBg).Padding(4).AlignCenter().Text("#").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background(headerBg).Padding(4).Text("DESCRIPTION & GADGET TELEMETRY").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background(headerBg).Padding(4).AlignCenter().Text("HSN").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background(headerBg).Padding(4).AlignCenter().Text("QTY").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background(headerBg).Padding(4).AlignRight().Text("RATE").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background(headerBg).Padding(4).AlignRight().Text("TAXABLE").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background(headerBg).Padding(4).AlignRight().Text("CGST").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background(headerBg).Padding(4).AlignRight().Text("SGST").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background(headerBg).Padding(4).AlignRight().Text("TOTAL (₹)").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                    });

                    int idx = 1;
                    foreach (var item in _invoice.Items)
                    {
                        string rowBg = idx % 2 == 1 ? "#FFFFFF" : "#F8FAFC";

                        table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(4).AlignCenter().Text(idx.ToString()).FontSize(7.5f);
                        
                        table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(4).Column(c =>
                        {
                            c.Item().Text(item.ItemDescription).FontSize(8.5f).Bold();

                            if (item.IsSerialized)
                            {
                                string imeiStr = $"IMEI: {item.IMEI1}";
                                if (!string.IsNullOrWhiteSpace(item.IMEI2)) imeiStr += $" / {item.IMEI2}";
                                if (!string.IsNullOrWhiteSpace(item.SerialNumber)) imeiStr += $" | S/N: {item.SerialNumber}";
                                c.Item().Text(imeiStr).FontSize(7).FontFamily("Consolas").FontColor(isBw ? "#000000" : "#0284C7");

                                string diag = $"Diagnostic Health: {item.BatteryHealthPercentage ?? 100}% Battery | Grade: {item.ConditionGrade} | Warranty: {item.WarrantyStatus}";
                                if (item.IsMarginScheme) diag += " | [Margin Scheme Rule 32(5)]";
                                c.Item().Text(diag).FontSize(6.8f).FontColor("#475569");
                            }
                        });

                        table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(4).AlignCenter().Text(item.HSNCode ?? "-").FontSize(7.5f);
                        table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(4).AlignCenter().Text(item.Quantity.ToString()).FontSize(7.5f).Bold();
                        table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(4).AlignRight().Text($"₹{item.UnitPrice:N0}").FontSize(7.5f);
                        table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(4).AlignRight().Text($"₹{item.TaxableValue:N0}").FontSize(7.5f);
                        table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(4).AlignRight().Text($"₹{item.CGSTAmount:N0}").FontSize(7.5f);
                        table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(4).AlignRight().Text($"₹{item.SGSTAmount:N0}").FontSize(7.5f);
                        table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(4).AlignRight().Text($"₹{item.LineTotal:N0}").FontSize(8).Bold();

                        idx++;
                    }
                });

                // Summary & Settlement
                col.Item().PaddingTop(8).Row(row =>
                {
                    // Left Column: Settlement & UPI QR
                    row.RelativeItem(7).Border(1).BorderColor("#CBD5E1").Padding(8).Row(pr =>
                    {
                        pr.RelativeItem(7).Column(c =>
                        {
                            c.Item().Text("BOUTIQUE SETTLEMENT RECORD:").FontSize(7.5f).Bold().FontColor("#475569");
                            if (_invoice.Payments != null && _invoice.Payments.Any())
                            {
                                foreach (var p in _invoice.Payments)
                                {
                                    c.Item().Text($"• {p.Mode}: ₹{p.Amount:N0}").FontSize(7.5f).Bold();
                                }
                            }
                            else
                            {
                                c.Item().Text("• Cash / Direct Counter Payment").FontSize(7.5f);
                            }

                            c.Item().PaddingTop(4).Text("ELECTRONIC REMITTANCE:").FontSize(7.5f).Bold();
                            c.Item().Text($"Bank: {_company.BankName} | A/C: {_company.BankAccountNumber}").FontSize(7);
                            c.Item().Text($"IFSC: {_company.BankIFSC} | Branch: {_company.BankBranch}").FontSize(7);
                            c.Item().Text($"UPI VPA: {_company.UpiId}").FontSize(7.5f).Bold();
                        });

                        pr.RelativeItem(5).AlignRight().Column(qc =>
                        {
                            string upiUri = BarcodeGeneratorService.BuildUpiPaymentUri(
                                _company.UpiId ?? "9080803070@upi",
                                _company.BusinessName,
                                _invoice.GrandTotal,
                                _invoice.InvoiceNumber);
                            byte[] qrBytes = _barcodeService.GenerateQrCode(upiUri, 72);
                            qc.Item().AlignCenter().Image(qrBytes).FitArea();
                            qc.Item().AlignCenter().Text("SCAN & PAY").FontSize(6.5f).Bold();
                        });
                    });

                    // Right Column: Summary
                    row.RelativeItem(5).PaddingLeft(8).Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(8).Column(tc =>
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

                        tc.Item().PaddingTop(4).BorderTop(1.5f).BorderColor(frameColor).Background(headerBg).Padding(6).Row(r =>
                        {
                            r.RelativeItem().Text("NET TOTAL:").FontSize(10).Bold().FontColor("#FFFFFF");
                            r.RelativeItem().AlignRight().Text($"₹{_invoice.GrandTotal:N0}").FontSize(12).Bold().FontColor(isBw ? "#FFFFFF" : "#D4AF37");
                        });
                    });
                });
            });

            // FOOTER
            page.Footer().Column(col =>
            {
                col.Item().PaddingTop(6).Border(1).BorderColor(frameColor).Padding(6).Row(r =>
                {
                    r.RelativeItem(8).Column(c =>
                    {
                        c.Item().Text("BOUTIQUE TERMS & WARRANTY:").FontSize(7).Bold();
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
