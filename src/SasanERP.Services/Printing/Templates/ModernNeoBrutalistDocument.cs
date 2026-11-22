using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SasanERP.Core.Models;
using SasanERP.Services.Barcode;

namespace SasanERP.Services.Printing.Templates;

/// <summary>
/// A4 Style 6: Modern Neo-Brutalist Invoice (High-Contrast Geometric Layout)
/// Features bold 1.5pt borders, high-contrast typography, and sharp geometric compartments.
/// Flawlessly optimized for both vibrant Full Color and razor-sharp Black & White laser printing.
/// </summary>
public class ModernNeoBrutalistDocument : IDocument
{
    private readonly Invoice _invoice;
    private readonly CompanyProfile _company;
    private readonly BarcodeGeneratorService _barcodeService;

    public ModernNeoBrutalistDocument(Invoice invoice, CompanyProfile company, BarcodeGeneratorService barcodeService)
    {
        _invoice = invoice;
        _company = company;
        _barcodeService = barcodeService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Invoice_{_invoice.InvoiceNumber}_NeoBrutalist",
        Author = _company.BusinessName
    };

    public void Compose(IDocumentContainer container)
    {
        bool isBw = _company.IsMonochromePrintMode;
        string borderColor = "#000000";
        string headerBg = isBw ? "#000000" : "#0A1128";
        string accentColor = isBw ? "#000000" : "#00E5FF";
        string subAccent = isBw ? "#333333" : "#FF9100";
        string lightFill = "#F8FAFC";
        byte[]? logoBytes = LogoProvider.GetLogoBytes(isBw, _company.LogoPath);

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(28, Unit.Point);
            page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Segoe UI").FontColor("#0F172A"));

            // 1. HEADER: Bold Geometric Box
            page.Header().Column(col =>
            {
                col.Item().Border(1.5f).BorderColor(borderColor).Row(row =>
                {
                    // Left: Logo & Company details
                    row.RelativeItem(7).Padding(10).Row(r =>
                    {
                        if (logoBytes != null)
                        {
                            r.ConstantItem(48).PaddingRight(10).Image(logoBytes).FitArea();
                        }
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text(_company.BusinessName.ToUpper()).FontSize(15).Bold().FontColor(borderColor);
                            c.Item().Text(_company.Tagline).FontSize(8).Italic().FontColor("#475569");
                            c.Item().PaddingTop(2).Text($"{_company.AddressLine1}, {_company.AddressLine2}, {_company.City} - {_company.Pincode}")
                                .FontSize(7.5f).FontColor("#334155");
                            c.Item().Text($"Phone: {_company.PhoneNumber} | Email: {_company.Email}")
                                .FontSize(7.5f).FontColor("#334155");
                            if (!string.IsNullOrWhiteSpace(_company.GSTIN))
                            {
                                c.Item().Text($"GSTIN: {_company.GSTIN} | State Code: {_company.StateCode} (Tamil Nadu)")
                                    .FontSize(8).Bold().FontColor(borderColor);
                            }
                        });
                    });

                    // Right: Tax Invoice Stamp
                    row.RelativeItem(5).BorderLeft(1.5f).BorderColor(borderColor).Background(headerBg).Padding(10).Column(c =>
                    {
                        string docType = !_invoice.IsGstEnabled ? "RETAIL BILL / CASH MEMO" : "GST TAX INVOICE";
                        c.Item().AlignCenter().Text(docType).FontSize(11).Bold().FontColor("#FFFFFF");
                        c.Item().PaddingTop(4).Text($"INVOICE NO: {_invoice.InvoiceNumber}").FontSize(9.5f).Bold().FontColor(accentColor);
                        c.Item().Text($"DATE: {_invoice.InvoiceDate:dd-MMM-yyyy, hh:mm tt}").FontSize(8).FontColor("#E2E8F0");
                        c.Item().Text($"STATUS: FULLY PAID").FontSize(8).Bold().FontColor(isBw ? "#FFFFFF" : "#10B981");
                    });
                });
            });

            // 2. CONTENT
            page.Content().Column(col =>
            {
                // Customer Details Box
                col.Item().PaddingTop(8).Border(1.5f).BorderColor(borderColor).Background(lightFill).Padding(8).Row(r =>
                {
                    r.RelativeItem(7).Column(c =>
                    {
                        c.Item().Text("BUYER / RECIPIENT INFORMATION").FontSize(7.5f).Bold().FontColor("#64748B");
                        c.Item().Text(_invoice.Customer.FullName.ToUpper()).FontSize(10).Bold().FontColor(borderColor);
                        c.Item().Text($"Mobile: {_invoice.Customer.PhoneNumber}").FontSize(8);
                        if (!string.IsNullOrWhiteSpace(_invoice.Customer.Email))
                            c.Item().Text($"Email: {_invoice.Customer.Email}").FontSize(7.5f);
                        if (!string.IsNullOrWhiteSpace(_invoice.Customer.Address))
                            c.Item().Text($"Address: {_invoice.Customer.Address}").FontSize(7.5f);
                    });

                    r.RelativeItem(5).BorderLeft(1).BorderColor("#CBD5E1").PaddingLeft(8).Column(c =>
                    {
                        c.Item().Text("TAX & DELIVERY DETAILS").FontSize(7.5f).Bold().FontColor("#64748B");
                        c.Item().Text($"Customer GSTIN: {(!string.IsNullOrWhiteSpace(_invoice.Customer.GSTIN) ? _invoice.Customer.GSTIN : "UNREGISTERED / B2C")}").FontSize(8).Bold();
                        c.Item().Text($"Place of Supply: State 33 (Tamil Nadu)").FontSize(7.5f);
                        c.Item().Text($"Reverse Charge (RCM): No").FontSize(7.5f);
                    });
                });

                // Items Table
                col.Item().PaddingTop(8).Border(1.5f).BorderColor(borderColor).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(20);  // S.No
                        cols.RelativeColumn(6);  // Description & Telemetry
                        cols.ConstantColumn(42);  // HSN
                        cols.ConstantColumn(24);  // Qty
                        cols.ConstantColumn(52);  // Rate
                        cols.ConstantColumn(50);  // Taxable
                        cols.ConstantColumn(42);  // CGST
                        cols.ConstantColumn(42);  // SGST
                        cols.ConstantColumn(60);  // Total
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

                        table.Cell().Background(rowBg).BorderBottom(1).BorderColor("#E2E8F0").Padding(4).AlignCenter().Text(idx.ToString()).FontSize(7.5f);
                        
                        table.Cell().Background(rowBg).BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Column(c =>
                        {
                            c.Item().Text(item.ItemDescription).FontSize(8.5f).Bold().FontColor(borderColor);

                            if (item.IsSerialized)
                            {
                                string imeiText = $"IMEI 1: {item.IMEI1}";
                                if (!string.IsNullOrWhiteSpace(item.IMEI2)) imeiText += $" | IMEI 2: {item.IMEI2}";
                                if (!string.IsNullOrWhiteSpace(item.SerialNumber)) imeiText += $" | S/N: {item.SerialNumber}";
                                c.Item().Text(imeiText).FontSize(7).FontFamily("Consolas").FontColor(isBw ? "#000000" : "#0284C7");

                                string healthText = $"Battery: {item.BatteryHealthPercentage ?? 100}% | Grade: {item.ConditionGrade} | Warranty: {item.WarrantyStatus}";
                                if (item.IsMarginScheme) healthText += " | [Rule 32(5) Pre-Owned Margin Scheme]";
                                c.Item().Text(healthText).FontSize(6.8f).FontColor("#475569");
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

                // Summary & Payment Section
                col.Item().PaddingTop(8).Row(row =>
                {
                    // Left: Split Payment Tender & Bank Info
                    row.RelativeItem(6).Border(1.5f).BorderColor(borderColor).Padding(8).Column(c =>
                    {
                        c.Item().Text("PAYMENT METHOD & TENDER BREAKDOWN").FontSize(7.5f).Bold().FontColor("#64748B");
                        c.Item().PaddingTop(2).Row(pr =>
                        {
                            pr.RelativeItem(7).Column(pc =>
                            {
                                if (_invoice.Payments != null && _invoice.Payments.Any())
                                {
                                    foreach (var p in _invoice.Payments)
                                    {
                                        string refText = !string.IsNullOrWhiteSpace(p.ReferenceNumber) ? $" (Ref: {p.ReferenceNumber})" : "";
                                        pc.Item().Text($"• {p.Mode}: ₹{p.Amount:N0}{refText}").FontSize(7.5f).Bold();
                                    }
                                }
                                else
                                {
                                    pc.Item().Text("• Cash / Full Counter Payment").FontSize(7.5f);
                                }

                                pc.Item().PaddingTop(4).Text("BANK / UPI REMITTANCE:").FontSize(7.5f).Bold();
                                pc.Item().Text($"Bank: {_company.BankName} | A/C: {_company.BankAccountNumber}").FontSize(7);
                                pc.Item().Text($"IFSC: {_company.BankIFSC} | Branch: {_company.BankBranch}").FontSize(7);
                                pc.Item().Text($"UPI ID: {_company.UpiId}").FontSize(7.5f).Bold().FontColor(isBw ? "#000000" : "#0284C7");
                            });

                            // Dynamic UPI QR Code
                            pr.RelativeItem(5).AlignRight().Column(qrc =>
                            {
                                string upiUri = BarcodeGeneratorService.BuildUpiPaymentUri(
                                    _company.UpiId ?? "9080803070@upi",
                                    _company.BusinessName,
                                    _invoice.GrandTotal,
                                    _invoice.InvoiceNumber);
                                byte[] qrBytes = _barcodeService.GenerateQrCode(upiUri, 80);
                                qrc.Item().AlignCenter().Image(qrBytes).FitArea();
                                qrc.Item().AlignCenter().Text("SCAN & PAY UPI").FontSize(6.5f).Bold();
                            });
                        });
                    });

                    // Right: Financial Totals Box
                    row.RelativeItem(6).PaddingLeft(8).Border(1.5f).BorderColor(borderColor).Background(lightFill).Padding(8).Column(tc =>
                    {
                        tc.Item().Row(r => { r.RelativeItem().Text("SUBTOTAL:"); r.RelativeItem().AlignRight().Text($"₹{_invoice.GrossTotal:N2}").Bold(); });
                        if (_invoice.InvoiceDiscount > 0)
                            tc.Item().Row(r => { r.RelativeItem().Text("DISCOUNT:"); r.RelativeItem().AlignRight().Text($"-₹{_invoice.InvoiceDiscount:N2}").FontColor("#DC2626"); });

                        tc.Item().Row(r => { r.RelativeItem().Text("TOTAL TAXABLE VALUE:"); r.RelativeItem().AlignRight().Text($"₹{_invoice.TotalTaxableValue:N2}"); });
                        tc.Item().Row(r => { r.RelativeItem().Text("CENTRAL GST (CGST):"); r.RelativeItem().AlignRight().Text($"₹{_invoice.TotalCGST:N2}"); });
                        tc.Item().Row(r => { r.RelativeItem().Text("STATE GST (SGST):"); r.RelativeItem().AlignRight().Text($"₹{_invoice.TotalSGST:N2}"); });
                        
                        if (_invoice.HasMarginSchemeItems)
                            tc.Item().Row(r => { r.RelativeItem().Text("MARGIN TAX (Rule 32(5)):"); r.RelativeItem().AlignRight().Text($"₹{_invoice.TotalMarginTax:N2}").Bold(); });

                        if (_invoice.RoundOff != 0)
                            tc.Item().Row(r => { r.RelativeItem().Text("ROUND OFF:"); r.RelativeItem().AlignRight().Text($"₹{_invoice.RoundOff:N2}"); });

                        // Grand Total Box
                        tc.Item().PaddingTop(4).BorderTop(1.5f).BorderColor(borderColor).Background(isBw ? "#000000" : "#0A1128").Padding(6).Row(r =>
                        {
                            r.RelativeItem().Text("NET PAYABLE:").FontSize(11).Bold().FontColor("#FFFFFF");
                            r.RelativeItem().AlignRight().Text($"₹{_invoice.GrandTotal:N0}").FontSize(13).Bold().FontColor(accentColor);
                        });
                    });
                });
            });

            // 3. FOOTER
            page.Footer().Column(col =>
            {
                col.Item().PaddingTop(6).Border(1).BorderColor(borderColor).Padding(6).Row(r =>
                {
                    r.RelativeItem(8).Column(c =>
                    {
                        c.Item().Text("TERMS & CONDITIONS:").FontSize(7).Bold();
                        c.Item().Text(_company.TermsAndConditions).FontSize(6.2f).FontColor("#475569");
                    });

                    r.RelativeItem(4).AlignRight().Column(c =>
                    {
                        c.Item().AlignCenter().Text($"For {_company.BusinessName.ToUpper()}").FontSize(7.5f).Bold();
                        c.Item().Height(24); // Signature space
                        c.Item().AlignCenter().Text("AUTHORIZED SIGNATORY").FontSize(7).Bold().FontColor(borderColor);
                    });
                });

                col.Item().PaddingTop(2).Row(r =>
                {
                    r.RelativeItem().Text($"Generated by SasanERP • Official Invoice • Page 1 of 1").FontSize(6.5f).FontColor("#94A3B8");
                    r.RelativeItem().AlignRight().Text("Computer Generated Tax Invoice. No Physical Signature Required.").FontSize(6.5f).FontColor("#94A3B8");
                });
            });
        });
    }
}
