using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SasanERP.Core.Models;
using SasanERP.Services.Barcode;

namespace SasanERP.Services.Printing.Templates;

/// <summary>
/// Choice 6: Eco-Digital WhatsApp Story Invoice Template (Mobile 9:16 Portrait)
/// Optimized for paperless digital delivery via WhatsApp, Email, and mobile viewing.
/// Features smartphone aspect ratio (360x640pt), interactive store links, dynamic UPI QR, and eco-friendly paperless badge.
/// </summary>
public class EcoDigitalWhatsAppDocument : IDocument
{
    private readonly Invoice _invoice;
    private readonly CompanyProfile _company;
    private readonly BarcodeGeneratorService _barcodeService;

    public EcoDigitalWhatsAppDocument(Invoice invoice, CompanyProfile company, BarcodeGeneratorService barcodeService)
    {
        _invoice = invoice;
        _company = company;
        _barcodeService = barcodeService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"E_Invoice_{_invoice.InvoiceNumber}",
        Author = _company.BusinessName
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            // Mobile smartphone aspect ratio: 360 x 640 points (~9:16)
            page.Size(360, 640, Unit.Point);
            page.Margin(12, Unit.Point);
            page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Segoe UI").FontColor("#0F172A"));

            page.Content().Column(col =>
            {
                byte[]? logoBytes = LogoProvider.GetLogoBytes(isMonochrome: false, _company.LogoPath);

                // Top Header Card
                col.Item().Background("#0B192C").Padding(10).Column(c =>
                {
                    c.Item().Row(r =>
                    {
                        if (logoBytes != null)
                        {
                            r.ConstantItem(36).PaddingRight(8).Image(logoBytes).FitArea();
                        }
                        r.RelativeItem().Column(hc =>
                        {
                            hc.Item().Row(lr =>
                            {
                                lr.AutoItem().Text("SASAN").FontSize(14).Bold().FontColor("#00E5FF");
                                lr.AutoItem().PaddingLeft(4).Text("GADGET LOUNGE").FontSize(14).Bold().FontColor("#FFFFFF");
                            });
                            hc.Item().Text($"{_company.FormattedSingleLineAddress} • {_company.PhoneNumber}").FontSize(6.8f).FontColor("#CBD5E1");
                        });

                        r.AutoItem().Background("#059669").Padding(3).Column(ec =>
                        {
                            ec.Item().AlignCenter().Text("🌱 ECO-INVOICE").FontSize(6f).Bold().FontColor("#FFFFFF");
                            ec.Item().AlignCenter().Text("PAPERLESS").FontSize(5f).FontColor("#FFFFFF");
                        });
                    });

                    c.Item().PaddingTop(6).Row(br =>
                    {
                        string billType = !_invoice.IsGstEnabled ? "RETAIL BILL" : "GST TAX INVOICE";
                        br.RelativeItem().Text($"{billType}  #{_invoice.InvoiceNumber}").FontSize(8f).Bold().FontColor("#38BDF8");
                        br.AutoItem().Text($"{_invoice.InvoiceDate:dd MMM yyyy, hh:mm tt}").FontSize(7f).FontColor("#E2E8F0");
                    });
                });

                // Customer Bar
                col.Item().PaddingTop(6).Background("#F1F5F9").Padding(6).Row(cr =>
                {
                    cr.RelativeItem().Column(c =>
                    {
                        c.Item().Text($"Billed To: {_invoice.Customer.FullName}").FontSize(8f).Bold();
                        c.Item().Text($"Phone: {_invoice.Customer.PhoneNumber}").FontSize(7f).FontColor("#475569");
                    });
                    cr.AutoItem().Column(c =>
                    {
                        c.Item().AlignRight().Text("STATUS: COMPLETED").FontSize(6.8f).Bold().FontColor("#059669");
                        if (!string.IsNullOrWhiteSpace(_invoice.Customer.GSTIN))
                            c.Item().AlignRight().Text($"GST: {_invoice.Customer.GSTIN}").FontSize(6.5f).Bold();
                    });
                });

                // Purchased Items Cards
                col.Item().PaddingTop(6).Column(ic =>
                {
                    foreach (var itm in _invoice.Items)
                    {
                        ic.Item().PaddingBottom(4).Background("#FFFFFF").Border(1).BorderColor("#E2E8F0").Padding(6).Column(tc =>
                        {
                            tc.Item().Row(r =>
                            {
                                r.RelativeItem().Text(itm.ItemDescription).FontSize(8.5f).Bold().FontColor("#0B192C");
                                r.AutoItem().Text($"₹{itm.LineTotal:N0}").FontSize(9.5f).Bold().FontColor("#0284C7");
                            });

                            if (!string.IsNullOrEmpty(itm.IMEI1) || !string.IsNullOrEmpty(itm.SerialNumber))
                            {
                                string imeiStr = itm.IMEI1 ?? itm.SerialNumber ?? string.Empty;
                                tc.Item().PaddingTop(2).Text($"IMEI: {imeiStr} | Bat: {itm.BatteryHealthPercentage ?? 100}% | {itm.ConditionGrade}").FontSize(6.5f).Bold().FontColor("#0369A1");
                            }

                            tc.Item().PaddingTop(2).Row(qr =>
                            {
                                qr.RelativeItem().Text($"Qty: {itm.Quantity:0} @ ₹{itm.UnitPrice:N0}").FontSize(7f).FontColor("#64748B");
                                if (itm.WarrantyStatus.HasValue)
                                    qr.AutoItem().Text($"Warranty: {itm.WarrantyStatus}").FontSize(7f).Bold().FontColor("#D97706");
                            });
                        });
                    }
                });

                // Total & UPI Box
                col.Item().PaddingTop(4).Background("#0B192C").Padding(8).Row(tr =>
                {
                    tr.RelativeItem(7).Column(c =>
                    {
                        if (_invoice.IsGstEnabled)
                        {
                            c.Item().Text($"Taxable: ₹{_invoice.StandardTaxableValue:N0} | GST: ₹{(_invoice.TotalCGST + _invoice.TotalSGST):N0}").FontSize(6.8f).FontColor("#CBD5E1");
                        }
                        c.Item().Text($"Total Items: {_invoice.Items.Sum(i => i.Quantity):0}").FontSize(6.8f).FontColor("#CBD5E1");
                        c.Item().PaddingTop(2).Row(gt =>
                        {
                            gt.AutoItem().Text("AMOUNT PAID: ").FontSize(8f).Bold().FontColor("#00E5FF");
                            gt.AutoItem().Text($"₹{_invoice.GrandTotal:N0}").FontSize(12).Bold().FontColor("#FFFFFF");
                        });
                    });

                    // Dynamic UPI QR Code
                    string upiString = $"upi://pay?pa={_company.UpiId}&pn=SasanGadgetLounge&am={_invoice.GrandTotal:0.00}&cu=INR";
                    byte[] qrBytes = _barcodeService.GenerateQrCode(upiString, 80);
                    if (qrBytes.Length > 0)
                    {
                        tr.RelativeItem(5).AlignRight().Column(c =>
                        {
                            c.Item().Height(40, Unit.Point).Image(qrBytes);
                            c.Item().AlignCenter().Text("SCAN UPI / VERIFY").FontSize(5.5f).FontColor("#00E5FF");
                        });
                    }
                });

                // Digital Store Contact Hub
                col.Item().PaddingTop(6).Background("#F8FAFC").Border(0.5f).BorderColor("#CBD5E1").Padding(6).Column(sc =>
                {
                    sc.Item().AlignCenter().Text("CONNECT WITH SASAN GADGET LOUNGE").FontSize(7f).Bold().FontColor("#0B192C");
                    sc.Item().PaddingTop(2).Row(r =>
                    {
                        r.RelativeItem().AlignCenter().Text("📞 Call: 9080803070").FontSize(6.5f).Bold();
                        r.RelativeItem().AlignCenter().Text("💬 WhatsApp: 9080803070").FontSize(6.5f).Bold();
                    });
                    sc.Item().PaddingTop(2).Row(r =>
                    {
                        r.RelativeItem().AlignCenter().Text("📍 Opp. Suppan Ragavan Colony, Theni").FontSize(6.2f).FontColor("#64748B");
                    });
                });

                // Environmental Statement
                col.Item().PaddingTop(6).AlignCenter().Text("Thank you for saving paper! Sasan The Gadget Lounge promotes sustainable tech.").FontSize(5.8f).Italic().FontColor("#64748B");
            });
        });
    }
}
