using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SasanERP.Core.Models;
using SasanERP.Services.Barcode;

namespace SasanERP.Services.Printing.Templates;

/// <summary>
/// A4 Style 9: Compact Tech Docket Invoice (High-Density Spatial Layout)
/// Optimized for dense tech merchandise, accessories, serialized hardware dockets, and instant laser printing.
/// Fully compatible with both Full Color and Black & White / Grayscale laser printers.
/// </summary>
public class CompactTechDocketDocument : IDocument
{
    private readonly Invoice _invoice;
    private readonly CompanyProfile _company;
    private readonly BarcodeGeneratorService _barcodeService;

    public CompactTechDocketDocument(Invoice invoice, CompanyProfile company, BarcodeGeneratorService barcodeService)
    {
        _invoice = invoice;
        _company = company;
        _barcodeService = barcodeService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Invoice_{_invoice.InvoiceNumber}_TechDocket",
        Author = _company.BusinessName
    };

    public void Compose(IDocumentContainer container)
    {
        bool isBw = _company.IsMonochromePrintMode;
        string borderColor = "#334155";
        string headerBg = isBw ? "#1E293B" : "#0F172A";
        string accentColor = isBw ? "#000000" : "#0284C7";
        byte[]? logoBytes = LogoProvider.GetLogoBytes(isBw, _company.LogoPath);

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(26, Unit.Point);
            page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Segoe UI").FontColor("#0F172A"));

            // 1. DOCKET HEADER
            page.Header().Column(col =>
            {
                col.Item().Border(1.2f).BorderColor(borderColor).Row(row =>
                {
                    row.RelativeItem(7).Padding(8).Row(r =>
                    {
                        if (logoBytes != null)
                        {
                            r.ConstantItem(46).PaddingRight(8).Image(logoBytes).FitArea();
                        }
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text(_company.BusinessName.ToUpper()).FontSize(14).Bold().FontColor("#0F172A");
                            c.Item().Text(_company.Tagline).FontSize(7.5f).Italic().FontColor("#475569");
                            c.Item().Text($"{_company.AddressLine1}, {_company.AddressLine2}, {_company.City} - {_company.Pincode}")
                                .FontSize(7.2f);
                            c.Item().Text($"GSTIN: {_company.GSTIN} | Ph: {_company.PhoneNumber} | Email: {_company.Email}")
                                .FontSize(7.2f).Bold();
                        });
                    });

                    row.RelativeItem(5).BorderLeft(1.2f).BorderColor(borderColor).Background("#F8FAFC").Padding(8).Column(c =>
                    {
                        string docTitle = !_invoice.IsGstEnabled ? "RETAIL BILL / CASH DOCKET" : "GST TAX DOCKET";
                        c.Item().Text(docTitle).FontSize(11).Bold().FontColor(accentColor);
                        c.Item().Text($"DOCKET #: {_invoice.InvoiceNumber}").FontSize(9).Bold();
                        c.Item().Text($"DATE: {_invoice.InvoiceDate:dd-MMM-yyyy, hh:mm tt}").FontSize(7.5f);
                        c.Item().Text($"STATE: 33-Tamil Nadu (Place of Supply)").FontSize(7.2f);
                    });
                });

                // Customer Line
                col.Item().PaddingTop(4).Border(1.2f).BorderColor(borderColor).Background("#FFFFFF").Padding(6).Row(r =>
                {
                    r.RelativeItem(8).Column(c =>
                    {
                        c.Item().Text(t =>
                        {
                            t.Span("CLIENT: ").Bold().FontSize(8);
                            t.Span($"{_invoice.Customer.FullName} ").Bold().FontSize(8.5f);
                            t.Span($"| Ph: {_invoice.Customer.PhoneNumber} ").FontSize(8);
                            if (!string.IsNullOrWhiteSpace(_invoice.Customer.GSTIN))
                                t.Span($"| GSTIN: {_invoice.Customer.GSTIN}").Bold().FontSize(8);
                        });
                        if (!string.IsNullOrWhiteSpace(_invoice.Customer.Address))
                            c.Item().Text($"Address: {_invoice.Customer.Address}").FontSize(7.2f);
                    });

                    r.RelativeItem(4).AlignRight().Text("PAYMENT: VERIFIED & CLEARED").FontSize(8).Bold().FontColor(isBw ? "#000000" : "#10B981");
                });
            });

            // 2. ITEMS GRID
            page.Content().Column(col =>
            {
                col.Item().PaddingTop(6).Border(1.2f).BorderColor(borderColor).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(18);
                        cols.RelativeColumn(6.5f);
                        cols.ConstantColumn(40);
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
                        h.Cell().Background(headerBg).Padding(4).Text("ITEM & SERIAL TELEMETRY").FontSize(7.5f).Bold().FontColor("#FFFFFF");
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
                        table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).AlignCenter().Text(idx.ToString()).FontSize(7.5f);
                        
                        table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Column(c =>
                        {
                            c.Item().Text(item.ItemDescription).FontSize(8.5f).Bold();

                            if (item.IsSerialized)
                            {
                                string imeiText = $"IMEI: {item.IMEI1}";
                                if (!string.IsNullOrWhiteSpace(item.IMEI2)) imeiText += $" / {item.IMEI2}";
                                if (!string.IsNullOrWhiteSpace(item.SerialNumber)) imeiText += $" | S/N: {item.SerialNumber}";
                                c.Item().Text(imeiText).FontSize(7).FontFamily("Consolas");

                                string meta = $"Battery: {item.BatteryHealthPercentage ?? 100}% | Grade: {item.ConditionGrade} | Warranty: {item.WarrantyStatus}";
                                if (item.IsMarginScheme) meta += " | [Rule 32(5) Margin Scheme]";
                                c.Item().Text(meta).FontSize(6.8f).FontColor("#475569");
                            }
                        });

                        table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).AlignCenter().Text(item.HSNCode ?? "-").FontSize(7.5f);
                        table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).AlignCenter().Text(item.Quantity.ToString()).FontSize(7.5f).Bold();
                        table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).AlignRight().Text($"₹{item.UnitPrice:N0}").FontSize(7.5f);
                        table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).AlignRight().Text($"₹{item.TaxableValue:N0}").FontSize(7.5f);
                        table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).AlignRight().Text($"₹{item.CGSTAmount:N0}").FontSize(7.5f);
                        table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).AlignRight().Text($"₹{item.SGSTAmount:N0}").FontSize(7.5f);
                        table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).AlignRight().Text($"₹{item.LineTotal:N0}").FontSize(8).Bold();

                        idx++;
                    }
                });

                // Docket Summary & Vouchers
                col.Item().PaddingTop(6).Row(row =>
                {
                    // Left Column: Split Tender & Bank Mandate
                    row.RelativeItem(7).Border(1.2f).BorderColor(borderColor).Padding(6).Row(r =>
                    {
                        r.RelativeItem(7).Column(c =>
                        {
                            c.Item().Text("PAYMENT SETTLEMENT:").FontSize(7.5f).Bold();
                            if (_invoice.Payments != null && _invoice.Payments.Any())
                            {
                                foreach (var p in _invoice.Payments)
                                {
                                    c.Item().Text($"• {p.Mode}: ₹{p.Amount:N0}").FontSize(7.5f).Bold();
                                }
                            }
                            else
                            {
                                c.Item().Text("• Cash / Full Settlement").FontSize(7.5f);
                            }

                            c.Item().PaddingTop(4).Text("BANK DETAILS:").FontSize(7.5f).Bold();
                            c.Item().Text($"A/C: {_company.BankAccountNumber} | IFSC: {_company.BankIFSC}").FontSize(7);
                            c.Item().Text($"Bank: {_company.BankName} | Branch: {_company.BankBranch}").FontSize(7);
                            c.Item().Text($"UPI ID: {_company.UpiId}").FontSize(7.5f).Bold();
                        });

                        r.RelativeItem(5).AlignRight().Column(qc =>
                        {
                            string upiUri = BarcodeGeneratorService.BuildUpiPaymentUri(
                                _company.UpiId ?? "9080803070@upi",
                                _company.BusinessName,
                                _invoice.GrandTotal,
                                _invoice.InvoiceNumber);
                            byte[] qrBytes = _barcodeService.GenerateQrCode(upiUri, 70);
                            qc.Item().AlignCenter().Image(qrBytes).FitArea();
                            qc.Item().AlignCenter().Text("UPI QR").FontSize(6.5f).Bold();
                        });
                    });

                    // Right Column: Financial Math
                    row.RelativeItem(5).PaddingLeft(6).Border(1.2f).BorderColor(borderColor).Background("#F8FAFC").Padding(6).Column(tc =>
                    {
                        tc.Item().Row(r => { r.RelativeItem().Text("SUBTOTAL:"); r.RelativeItem().AlignRight().Text($"₹{_invoice.GrossTotal:N2}").Bold(); });
                        if (_invoice.InvoiceDiscount > 0)
                            tc.Item().Row(r => { r.RelativeItem().Text("DISCOUNT:"); r.RelativeItem().AlignRight().Text($"-₹{_invoice.InvoiceDiscount:N2}").FontColor("#DC2626"); });

                        tc.Item().Row(r => { r.RelativeItem().Text("TAXABLE VALUE:"); r.RelativeItem().AlignRight().Text($"₹{_invoice.TotalTaxableValue:N2}"); });
                        tc.Item().Row(r => { r.RelativeItem().Text("CGST:"); r.RelativeItem().AlignRight().Text($"₹{_invoice.TotalCGST:N2}"); });
                        tc.Item().Row(r => { r.RelativeItem().Text("SGST:"); r.RelativeItem().AlignRight().Text($"₹{_invoice.TotalSGST:N2}"); });

                        if (_invoice.HasMarginSchemeItems)
                            tc.Item().Row(r => { r.RelativeItem().Text("MARGIN TAX:"); r.RelativeItem().AlignRight().Text($"₹{_invoice.TotalMarginTax:N2}").Bold(); });

                        if (_invoice.RoundOff != 0)
                            tc.Item().Row(r => { r.RelativeItem().Text("ROUND OFF:"); r.RelativeItem().AlignRight().Text($"₹{_invoice.RoundOff:N2}"); });

                        tc.Item().PaddingTop(3).BorderTop(1.2f).BorderColor(borderColor).Background(headerBg).Padding(5).Row(r =>
                        {
                            r.RelativeItem().Text("NET PAYABLE:").FontSize(9.5f).Bold().FontColor("#FFFFFF");
                            r.RelativeItem().AlignRight().Text($"₹{_invoice.GrandTotal:N0}").FontSize(11.5f).Bold().FontColor("#FFFFFF");
                        });
                    });
                });
            });

            // 3. DOCKET FOOTER
            page.Footer().Column(col =>
            {
                col.Item().PaddingTop(4).Border(1).BorderColor(borderColor).Padding(5).Row(r =>
                {
                    r.RelativeItem(8).Column(c =>
                    {
                        c.Item().Text("TERMS & SHOP CONDITIONS:").FontSize(7).Bold();
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
