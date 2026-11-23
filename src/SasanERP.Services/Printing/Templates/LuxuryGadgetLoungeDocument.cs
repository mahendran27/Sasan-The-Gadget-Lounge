using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SasanERP.Core.Models;
using SasanERP.Services.Barcode;

namespace SasanERP.Services.Printing.Templates;

/// <summary>
/// Choice 2: The Luxury Gadget Lounge Invoice Template (A4 Vector)
/// Designed for flagship smartphone sales (iPhone Pro Max, Samsung Ultra, Foldables) and VIP buyers.
/// Features deep obsidian slate accents (#0B192C), electric cyan (#00E5FF) highlights,
/// gold luxury accents (#D4AF37), device health badges, and an authenticity QR certificate.
/// </summary>
public class LuxuryGadgetLoungeDocument : IDocument
{
    private readonly Invoice _invoice;
    private readonly CompanyProfile _company;
    private readonly BarcodeGeneratorService _barcodeService;

    public LuxuryGadgetLoungeDocument(Invoice invoice, CompanyProfile company, BarcodeGeneratorService barcodeService)
    {
        _invoice = invoice;
        _company = company;
        _barcodeService = barcodeService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Luxury_Tax_Invoice_{_invoice.InvoiceNumber}",
        Author = _company.BusinessName
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30, Unit.Point);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Segoe UI").FontColor("#0F172A"));

            // Header Banner
            page.Header().Column(col =>
            {
                bool isBw = _company.IsMonochromePrintMode;
                byte[]? logoBytes = LogoProvider.GetLogoBytes(isBw, _company.LogoPath);

                // Dark Slate Luxury Banner
                col.Item().Background(isBw ? "#1E293B" : "#0B192C").Padding(14).Row(row =>
                {
                    row.RelativeItem(7).Row(r =>
                    {
                        if (logoBytes != null)
                        {
                            r.ConstantItem(52).PaddingRight(12).Image(logoBytes).FitArea();
                        }
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text(t =>
                            {
                                t.Span("SASAN ").FontSize(18).Bold().FontColor(isBw ? "#FFFFFF" : "#00E5FF");
                                t.Span("THE GADGET LOUNGE").FontSize(18).Bold().FontColor("#FFFFFF");
                            });
                            c.Item().PaddingTop(2).Text(_company.Tagline).FontSize(8.5f).Italic().FontColor(isBw ? "#E2E8F0" : "#D4AF37");
                            c.Item().PaddingTop(4).Text(_company.FormattedSingleLineAddress)
                                .FontSize(7.8f).FontColor("#CBD5E1");
                            c.Item().Text($"Phone: {_company.PhoneNumber} | Email: {_company.Email}")
                                .FontSize(7.8f).FontColor("#CBD5E1");
                            if (!string.IsNullOrWhiteSpace(_company.GSTIN))
                            {
                                c.Item().Text($"GSTIN: {_company.GSTIN} | State Code: {_company.StateCode} (Tamil Nadu)")
                                    .FontSize(8).Bold().FontColor(isBw ? "#FFFFFF" : "#38BDF8");
                            }
                        });
                    });

                    row.RelativeItem(5).AlignRight().Column(c =>
                    {
                        string docType = !_invoice.IsGstEnabled ? "RETAIL INVOICE / CASH MEMO" : "TAX INVOICE";
                        c.Item().AlignRight().Container()
                            .Background(isBw ? "#0F172A" : "#10223D")
                            .Border(1).BorderColor(isBw ? "#334155" : "#00E5FF")
                            .PaddingVertical(4).PaddingHorizontal(14)
                            .Text(docType).FontSize(11).Bold().FontColor(isBw ? "#FFFFFF" : "#00E5FF");

                        c.Item().PaddingTop(6).AlignRight().Text($"INVOICE #: {_invoice.InvoiceNumber}").FontSize(10).Bold().FontColor("#FFFFFF");
                        c.Item().AlignRight().Text($"Date: {_invoice.InvoiceDate:dd MMM yyyy, hh:mm tt}").FontSize(8.5f).FontColor("#E2E8F0");
                        c.Item().AlignRight().Text("Payment Status: PAID").FontSize(8.5f).Bold().FontColor(isBw ? "#FFFFFF" : "#10B981");
                    });
                });

                // Accent Line
                col.Item().Height(3).Background(isBw ? "#475569" : "#00E5FF");
            });

            // Content Area
            page.Content().Column(col =>
            {
                // Customer & VIP Banner - Unified Single Card (Zero Stray Vertical Lines)
                col.Item().PaddingTop(12).Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Row(row =>
                {
                    row.RelativeItem(7).Padding(10).Column(c =>
                    {
                        c.Item().Text("CLIENT / BUYER DETAILS").FontSize(8).Bold().FontColor("#64748B");
                        c.Item().PaddingTop(2).Text(_invoice.Customer.FullName).FontSize(11).Bold().FontColor("#0B192C");
                        c.Item().Text($"Contact: {_invoice.Customer.PhoneNumber}").FontSize(8.5f);
                        if (!string.IsNullOrWhiteSpace(_invoice.Customer.Email))
                            c.Item().Text($"Email: {_invoice.Customer.Email}").FontSize(8.5f);
                        if (!string.IsNullOrWhiteSpace(_invoice.Customer.Address))
                            c.Item().Text($"Address: {_invoice.Customer.Address}").FontSize(8.5f);
                        if (!string.IsNullOrWhiteSpace(_invoice.Customer.GSTIN))
                            c.Item().Text($"Buyer GSTIN: {_invoice.Customer.GSTIN}").FontSize(8.5f).Bold().FontColor("#0284C7");
                    });

                    row.ConstantItem(1).Background("#E2E8F0");

                    row.RelativeItem(5).Padding(10).Column(c =>
                    {
                        bool isInterState = _invoice.TotalIGST > 0;
                        c.Item().Text("TRANSACTION SUMMARY").FontSize(8).Bold().FontColor("#64748B");
                        c.Item().PaddingTop(2).Row(r => { r.RelativeItem().Text("Supply Type:"); r.AutoItem().Text(isInterState ? "Inter-State (IGST)" : "Intra-State (CGST+SGST)").Bold(); });
                        c.Item().Row(r => { r.RelativeItem().Text("GST Rule:"); r.AutoItem().Text(_invoice.InvoiceType == Core.Enums.InvoiceType.MarginSchemeInvoice ? "Rule 32(5) Margin" : (_invoice.IsGstEnabled ? "Section 9 Standard" : "Non-GST Retail")); });
                        c.Item().Row(r => { r.RelativeItem().Text("Total Units:"); r.AutoItem().Text($"{_invoice.Items.Sum(i => i.Quantity):0} Device(s)").Bold(); });
                    });
                });

                // Gadgets & Products Grid
                col.Item().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(24);
                        cols.RelativeColumn(7);
                        cols.ConstantColumn(50);
                        cols.ConstantColumn(30);
                        cols.ConstantColumn(65);
                        cols.ConstantColumn(80);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Background("#0B192C").Padding(5).Text("#").FontSize(8).Bold().FontColor("#FFFFFF");
                        h.Cell().Background("#0B192C").Padding(5).Text("DEVICE SPECIFICATION & SERIAL REPUTATION").FontSize(8).Bold().FontColor("#FFFFFF");
                        h.Cell().Background("#0B192C").Padding(5).AlignCenter().Text("HSN").FontSize(8).Bold().FontColor("#FFFFFF");
                        h.Cell().Background("#0B192C").Padding(5).AlignCenter().Text("QTY").FontSize(8).Bold().FontColor("#FFFFFF");
                        h.Cell().Background("#0B192C").Padding(5).AlignRight().Text("RATE (₹)").FontSize(8).Bold().FontColor("#FFFFFF");
                        h.Cell().Background("#0B192C").Padding(5).AlignRight().Text("AMOUNT (₹)").FontSize(8).Bold().FontColor("#FFFFFF");
                    });

                    int idx = 1;
                    foreach (var itm in _invoice.Items)
                    {
                        string bg = (idx % 2 == 0) ? "#F8FAFC" : "#FFFFFF";

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(5).Text(idx.ToString()).FontSize(8);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(5).Column(c =>
                        {
                            c.Item().Text(itm.ItemDescription).FontSize(9.5f).Bold().FontColor("#0B192C");

                            // Device Health & Spec Badges
                            if (!string.IsNullOrEmpty(itm.IMEI1) || !string.IsNullOrEmpty(itm.SerialNumber))
                            {
                                string idText = !string.IsNullOrEmpty(itm.IMEI1) ? $"IMEI: {itm.IMEI1}" : $"S/N: {itm.SerialNumber}";
                                if (!string.IsNullOrEmpty(itm.IMEI2)) idText += $" | IMEI 2: {itm.IMEI2}";
                                c.Item().PaddingTop(2).Text(idText).FontSize(7.5f).Bold().FontColor("#0369A1");
                            }

                            var badgeDetails = new List<string>();
                            if (itm.ConditionGrade.HasValue) badgeDetails.Add($"Grade: {itm.ConditionGrade}");
                            if (itm.BatteryHealthPercentage.HasValue) badgeDetails.Add($"Battery: {itm.BatteryHealthPercentage}%");
                            if (itm.WarrantyStatus.HasValue) badgeDetails.Add($"Warranty: {itm.WarrantyStatus}");
                            if (badgeDetails.Any())
                            {
                                c.Item().PaddingTop(1).Text(string.Join(" • ", badgeDetails)).FontSize(7.5f).FontColor("#475569");
                            }
                        });

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(5).AlignCenter().Text(itm.HSNCode).FontSize(8);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(5).AlignCenter().Text(itm.Quantity.ToString("0")).FontSize(8);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(5).AlignRight().Text($"{itm.UnitPrice:N2}").FontSize(8.5f);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(5).AlignRight().Text($"{itm.LineTotal:N2}").FontSize(9).Bold();

                        idx++;
                    }
                });

                // Financial Calculation & Payment Split
                col.Item().PaddingTop(12).Row(row =>
                {
                    // Payment Modes & QR Code
                    row.RelativeItem(6).Column(c =>
                    {
                        c.Item().Text("PAYMENT METHOD & TENDER BREAKDOWN").FontSize(8).Bold().FontColor("#64748B");
                        foreach (var pay in _invoice.Payments)
                        {
                            c.Item().PaddingTop(2).Row(pr =>
                            {
                                pr.AutoItem().Background("#F1F5F9").Padding(2).Text(pay.Mode.ToString()).FontSize(8).Bold();
                                pr.RelativeItem().PaddingLeft(6).Text($"Ref: {pay.ReferenceNumber ?? "N/A"}").FontSize(7.5f).FontColor("#64748B");
                                pr.AutoItem().Text($"₹{pay.Amount:N2}").FontSize(8.5f).Bold();
                            });
                        }

                        // UPI QR Code
                        string upiString = $"upi://pay?pa={_company.UpiId}&pn=SasanGadgetLounge&am={_invoice.GrandTotal:0.00}&cu=INR";
                        byte[] qrBytes = _barcodeService.GenerateQrCode(upiString, 110);
                        if (qrBytes.Length > 0)
                        {
                            c.Item().PaddingTop(8).Row(qrRow =>
                            {
                                qrRow.AutoItem().Height(46, Unit.Point).Image(qrBytes);
                                qrRow.RelativeItem().PaddingLeft(8).Column(qc =>
                                {
                                    qc.Item().Text("PAY WITH ANY UPI APP").FontSize(7.5f).Bold().FontColor("#0B192C");
                                    qc.Item().Text($"VPA: {_company.UpiId}").FontSize(7f).FontColor("#64748B");
                                    qc.Item().Text("Google Pay • PhonePe • Paytm • BHIM").FontSize(6.5f).FontColor("#888888");
                                });
                            });
                        }
                    });

                    // Totals & Tax Box
                    row.RelativeItem(6).PaddingLeft(12).Column(c =>
                    {
                        c.Item().Background("#F8FAFC").Border(1).BorderColor("#CBD5E1").Padding(8).Column(tc =>
                        {
                            if (_invoice.IsGstEnabled)
                            {
                                tc.Item().Row(r => { r.RelativeItem().Text("Gross Taxable Value:").FontSize(8); r.AutoItem().Text($"₹{_invoice.StandardTaxableValue:N2}").FontSize(8); });
                                tc.Item().Row(r => { r.RelativeItem().Text("CGST:").FontSize(8); r.AutoItem().Text($"₹{_invoice.TotalCGST:N2}").FontSize(8); });
                                tc.Item().Row(r => { r.RelativeItem().Text("SGST:").FontSize(8); r.AutoItem().Text($"₹{_invoice.TotalSGST:N2}").FontSize(8); });
                                if (_invoice.TotalIGST > 0)
                                    tc.Item().Row(r => { r.RelativeItem().Text("IGST:").FontSize(8); r.AutoItem().Text($"₹{_invoice.TotalIGST:N2}").FontSize(8); });
                            }
                            else
                            {
                                tc.Item().Row(r => { r.RelativeItem().Text("Gross Total:").FontSize(8); r.AutoItem().Text($"₹{_invoice.GrossTotal:N2}").FontSize(8); });
                                if (_invoice.InvoiceDiscount > 0)
                                    tc.Item().Row(r => { r.RelativeItem().Text("Discount:").FontSize(8); r.AutoItem().Text($"- ₹{_invoice.InvoiceDiscount:N2}").FontSize(8).FontColor("#DC2626"); });
                            }

                            tc.Item().PaddingVertical(4).LineHorizontal(1).LineColor("#CBD5E1");

                            tc.Item().Background("#0B192C").Padding(6).Row(r =>
                            {
                                r.RelativeItem().Text("GRAND TOTAL:").FontSize(11).Bold().FontColor("#00E5FF");
                                r.AutoItem().Text($"₹{_invoice.GrandTotal:N2}").FontSize(13).Bold().FontColor("#FFFFFF");
                            });
                        });
                    });
                });

                // Terms of Warranty
                col.Item().PaddingTop(12).Column(c =>
                {
                    c.Item().Text("PREMIUM GADGET WARRANTY & STORE POLICIES").FontSize(7.5f).Bold().FontColor("#0B192C");
                    c.Item().Text(_company.TermsAndConditions).FontSize(6.8f).FontColor("#64748B");
                });
            });

            // Luxury Footer with Stamp
            page.Footer().Column(col =>
            {
                col.Item().LineHorizontal(0.5f).LineColor("#E2E8F0");
                col.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem(7).Column(c =>
                    {
                        c.Item().Text("Thank you for choosing Sasan The Gadget Lounge — Where Tech Meets Elegance").FontSize(7.5f).Bold().FontColor("#0B192C");
                        c.Item().Text($"Authenticity Guaranteed • Theni Branch • Hotline: {_company.PhoneNumber}").FontSize(7f).FontColor("#64748B");
                    });

                    row.RelativeItem(5).AlignRight().Column(c =>
                    {
                        c.Item().Text($"For {_company.BusinessName.ToUpper()}").FontSize(7.5f).Bold();
                        c.Item().PaddingTop(14).Text("Authorised Signatory / Store Stamp").FontSize(7f).FontColor("#475569");
                    });
                });
            });
        });
    }
}
