using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SasanERP.Core.Models;
using SasanERP.Services.Barcode;

namespace SasanERP.Services.Printing.Templates;

/// <summary>
/// Choice 4: 80mm Continuous Thermal Receipt Roll Template
/// Optimized for POS thermal slip printers (Epson, TVS, Citizen, Rugtek 80mm thermal rolls).
/// High-density monochrome layout, dashed separators, compact IMEI specs, and bottom dynamic UPI QR code.
/// </summary>
public class ThermalRoll80mmDocument : IDocument
{
    private readonly Invoice _invoice;
    private readonly CompanyProfile _company;
    private readonly BarcodeGeneratorService _barcodeService;

    public ThermalRoll80mmDocument(Invoice invoice, CompanyProfile company, BarcodeGeneratorService barcodeService)
    {
        _invoice = invoice;
        _company = company;
        _barcodeService = barcodeService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Thermal_Receipt_{_invoice.InvoiceNumber}",
        Author = _company.BusinessName
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            // 80mm continuous thermal slip width
            page.ContinuousSize(80, Unit.Millimetre);
            page.Margin(3, Unit.Millimetre);
            page.DefaultTextStyle(x => x.FontSize(7.5f).FontFamily("Consolas").FontColor("#000000"));

            page.Content().Column(col =>
            {
                byte[]? logoBytes = LogoProvider.GetLogoBytes(isMonochrome: true, _company.LogoPath);
                if (logoBytes != null)
                {
                    col.Item().AlignCenter().Width(38).Image(logoBytes).FitArea();
                }

                // Shop Header
                col.Item().AlignCenter().Text(_company.BusinessName.ToUpper()).FontSize(10).Bold();
                col.Item().AlignCenter().Text("PREMIUM NEW & PRE-OWNED GADGETS").FontSize(6.5f);
                col.Item().AlignCenter().Text(_company.FormattedSingleLineAddress).FontSize(6.5f);
                col.Item().AlignCenter().Text($"Tel: {_company.PhoneNumber}").FontSize(6.8f).Bold();
                if (!string.IsNullOrWhiteSpace(_company.GSTIN) && _invoice.IsGstEnabled)
                {
                    col.Item().AlignCenter().Text($"GSTIN: {_company.GSTIN} (TN-33)").FontSize(6.5f);
                }

                // Divider
                col.Item().PaddingVertical(2).AlignCenter().Text("------------------------------------------------").FontSize(6f);

                // Invoice Meta
                string slipTitle = !_invoice.IsGstEnabled ? "RETAIL CASH MEMO" : "TAX INVOICE";
                col.Item().AlignCenter().Text(slipTitle).FontSize(8.5f).Bold();
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text($"Bill #: {_invoice.InvoiceNumber}").Bold();
                    r.AutoItem().Text($"{_invoice.InvoiceDate:dd/MM/yy HH:mm}");
                });
                col.Item().Text($"Customer: {_invoice.Customer.FullName} ({_invoice.Customer.PhoneNumber})");

                // Divider
                col.Item().PaddingVertical(2).AlignCenter().Text("------------------------------------------------").FontSize(6f);

                // Table Header
                col.Item().Row(r =>
                {
                    r.RelativeItem(6).Text("ITEM / IMEI").Bold();
                    r.ConstantItem(20).AlignCenter().Text("QTY").Bold();
                    r.ConstantItem(40).AlignRight().Text("RATE").Bold();
                    r.ConstantItem(45).AlignRight().Text("AMOUNT").Bold();
                });
                col.Item().PaddingVertical(1).AlignCenter().Text("------------------------------------------------").FontSize(6f);

                // Items
                foreach (var itm in _invoice.Items)
                {
                    col.Item().PaddingTop(1).Row(r =>
                    {
                        r.RelativeItem(6).Text(itm.ItemDescription).Bold();
                        r.ConstantItem(20).AlignCenter().Text(itm.Quantity.ToString("0"));
                        r.ConstantItem(40).AlignRight().Text($"{itm.UnitPrice:N0}");
                        r.ConstantItem(45).AlignRight().Text($"{itm.LineTotal:N0}").Bold();
                    });

                    // IMEI / Serial if present
                    if (!string.IsNullOrEmpty(itm.IMEI1) || !string.IsNullOrEmpty(itm.SerialNumber))
                    {
                        string imeiStr = itm.IMEI1 ?? itm.SerialNumber ?? "";
                        col.Item().Text($"  SN/IMEI: {imeiStr}").FontSize(6.5f);
                        if (itm.ConditionGrade.HasValue || itm.BatteryHealthPercentage.HasValue)
                        {
                            col.Item().Text($"  Grade: {itm.ConditionGrade} | Bat: {itm.BatteryHealthPercentage}% | War: {itm.WarrantyStatus}").FontSize(6f);
                        }
                    }
                }

                // Divider
                col.Item().PaddingVertical(2).AlignCenter().Text("------------------------------------------------").FontSize(6f);

                // Totals
                if (_invoice.IsGstEnabled)
                {
                    col.Item().Row(r => { r.RelativeItem().Text("Taxable Base:"); r.AutoItem().Text($"₹{_invoice.StandardTaxableValue:N2}"); });
                    col.Item().Row(r => { r.RelativeItem().Text("CGST (9%):"); r.AutoItem().Text($"₹{_invoice.TotalCGST:N2}"); });
                    col.Item().Row(r => { r.RelativeItem().Text("SGST (9%):"); r.AutoItem().Text($"₹{_invoice.TotalSGST:N2}"); });
                }
                else
                {
                    col.Item().Row(r => { r.RelativeItem().Text("Gross Total:"); r.AutoItem().Text($"₹{_invoice.GrossTotal:N2}"); });
                }

                if (_invoice.InvoiceDiscount > 0)
                {
                    col.Item().Row(r => { r.RelativeItem().Text("Discount:"); r.AutoItem().Text($"-₹{_invoice.InvoiceDiscount:N2}"); });
                }

                col.Item().PaddingVertical(1).AlignCenter().Text("================================================").FontSize(6f);
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("TOTAL AMOUNT:").FontSize(10).Bold();
                    r.AutoItem().Text($"₹{_invoice.GrandTotal:N0}").FontSize(11).Bold();
                });
                col.Item().AlignCenter().Text("================================================").FontSize(6f);

                // Payment Breakdown
                col.Item().PaddingTop(2).Text("PAYMENT RECEIVED:").FontSize(7f).Bold();
                foreach (var p in _invoice.Payments)
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"  {p.Mode}:");
                        r.AutoItem().Text($"₹{p.Amount:N0}");
                    });
                }

                // Barcode of Invoice Number
                byte[] barcodeBytes = _barcodeService.GenerateCode128Barcode(_invoice.InvoiceNumber, 240, 36);
                if (barcodeBytes.Length > 0)
                {
                    col.Item().PaddingTop(4).AlignCenter().Height(20, Unit.Point).Image(barcodeBytes);
                    col.Item().AlignCenter().Text(_invoice.InvoiceNumber).FontSize(6.5f);
                }

                // Dynamic UPI QR Code
                string upiString = $"upi://pay?pa={_company.UpiId}&pn=SasanGadgetLounge&am={_invoice.GrandTotal:0.00}&cu=INR";
                byte[] qrBytes = _barcodeService.GenerateQrCode(upiString, 100);
                if (qrBytes.Length > 0)
                {
                    col.Item().PaddingTop(4).AlignCenter().Height(48, Unit.Point).Image(qrBytes);
                    col.Item().AlignCenter().Text("SCAN TO PAY VIA UPI").FontSize(6.5f).Bold();
                }

                // Footer & Terms
                col.Item().PaddingTop(4).AlignCenter().Text("7-Day Testing Warranty on Pre-Owned").FontSize(6.5f).Bold();
                col.Item().AlignCenter().Text("No Warranty for Physical / Liquid Damage").FontSize(6.2f);
                col.Item().AlignCenter().Text("*** THANK YOU FOR VISITING SASAN! ***").FontSize(7f).Bold();
            });
        });
    }
}
