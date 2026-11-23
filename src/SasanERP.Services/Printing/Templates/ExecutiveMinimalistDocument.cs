using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SasanERP.Core.Models;
using SasanERP.Services.Barcode;

namespace SasanERP.Services.Printing.Templates;

/// <summary>
/// Choice 1: The Executive Minimalist Invoice Template (A4 Vector)
/// Designed for corporate B2B clients, IT professionals, MacBooks, and high-value laptops.
/// Features clean hairline dividers, monospaced tabular figures, and minimal monochrome aesthetics.
/// </summary>
public class ExecutiveMinimalistDocument : IDocument
{
    private readonly Invoice _invoice;
    private readonly CompanyProfile _company;
    private readonly BarcodeGeneratorService _barcodeService;

    public ExecutiveMinimalistDocument(Invoice invoice, CompanyProfile company, BarcodeGeneratorService barcodeService)
    {
        _invoice = invoice;
        _company = company;
        _barcodeService = barcodeService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Minimalist_Invoice_{_invoice.InvoiceNumber}",
        Author = _company.BusinessName
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(35, Unit.Point);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Segoe UI").FontColor("#1A1A1A"));

            // Minimalist Header
            page.Header().Column(col =>
            {
                byte[]? logoBytes = LogoProvider.GetLogoBytes(_company.IsMonochromePrintMode, _company.LogoPath);
                col.Item().Row(row =>
                {
                    row.RelativeItem(7).Row(r =>
                    {
                        if (logoBytes != null)
                        {
                            r.ConstantItem(48).PaddingRight(10).Image(logoBytes).FitArea();
                        }
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text(_company.BusinessName.ToUpper()).FontSize(15).Bold().FontColor("#000000");
                            c.Item().Text(_company.FormattedSingleLineAddress)
                                .FontSize(7.8f).FontColor("#555555");
                            c.Item().Text($"GSTIN: {_company.GSTIN} | State Code: {_company.StateCode} | Tel: {_company.PhoneNumber}")
                                .FontSize(7.8f).FontColor("#555555");
                        });
                    });

                    row.RelativeItem(5).AlignRight().Column(c =>
                    {
                        string docTitle = !_invoice.IsGstEnabled ? "RETAIL BILL" : "TAX INVOICE";
                        c.Item().Text(docTitle).FontSize(14).Bold().FontColor("#000000");
                        c.Item().Text($"Invoice #: {_invoice.InvoiceNumber}").FontSize(9).Bold();
                        c.Item().Text($"Date: {_invoice.InvoiceDate:dd MMM yyyy}").FontSize(8.5f);
                    });
                });

                col.Item().PaddingTop(12).LineHorizontal(1).LineColor("#000000");
            });

            // Content
            page.Content().Column(col =>
            {
                // Customer Details
                col.Item().PaddingVertical(10).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("BILLED TO:").FontSize(8).Bold().FontColor("#777777");
                        c.Item().Text(_invoice.Customer.FullName).FontSize(10).Bold();
                        c.Item().Text($"Mobile: {_invoice.Customer.PhoneNumber}").FontSize(8.5f);
                        if (!string.IsNullOrWhiteSpace(_invoice.Customer.GSTIN))
                            c.Item().Text($"Customer GSTIN: {_invoice.Customer.GSTIN}").FontSize(8.5f).Bold();
                    });
                });

                // Items Table
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(24);
                        cols.RelativeColumn(7);
                        cols.ConstantColumn(45);
                        cols.ConstantColumn(30);
                        cols.ConstantColumn(65);
                        cols.ConstantColumn(75);
                    });

                    table.Header(h =>
                    {
                        h.Cell().BorderBottom(1).BorderColor("#000000").PaddingVertical(4).Text("#").FontSize(8).Bold();
                        h.Cell().BorderBottom(1).BorderColor("#000000").PaddingVertical(4).Text("DESCRIPTION & SERIAL NUMBER").FontSize(8).Bold();
                        h.Cell().BorderBottom(1).BorderColor("#000000").PaddingVertical(4).AlignCenter().Text("HSN").FontSize(8).Bold();
                        h.Cell().BorderBottom(1).BorderColor("#000000").PaddingVertical(4).AlignCenter().Text("QTY").FontSize(8).Bold();
                        h.Cell().BorderBottom(1).BorderColor("#000000").PaddingVertical(4).AlignRight().Text("RATE").FontSize(8).Bold();
                        h.Cell().BorderBottom(1).BorderColor("#000000").PaddingVertical(4).AlignRight().Text("TOTAL (INR)").FontSize(8).Bold();
                    });

                    int idx = 1;
                    foreach (var itm in _invoice.Items)
                    {
                        table.Cell().BorderBottom(0.5f).BorderColor("#E0E0E0").PaddingVertical(5).Text(idx.ToString()).FontSize(8);
                        table.Cell().BorderBottom(0.5f).BorderColor("#E0E0E0").PaddingVertical(5).Column(c =>
                        {
                            c.Item().Text(itm.ItemDescription).Bold().FontSize(8.5f);
                            if (!string.IsNullOrEmpty(itm.IMEI1) || !string.IsNullOrEmpty(itm.SerialNumber))
                            {
                                c.Item().Text($"S/N or IMEI: {itm.IMEI1 ?? itm.SerialNumber} | Grade: {itm.ConditionGrade} | Warranty: {itm.WarrantyStatus}")
                                    .FontSize(7.5f).FontColor("#666666");
                            }
                        });
                        table.Cell().BorderBottom(0.5f).BorderColor("#E0E0E0").PaddingVertical(5).AlignCenter().Text(itm.HSNCode).FontSize(8);
                        table.Cell().BorderBottom(0.5f).BorderColor("#E0E0E0").PaddingVertical(5).AlignCenter().Text(itm.Quantity.ToString("0")).FontSize(8);
                        table.Cell().BorderBottom(0.5f).BorderColor("#E0E0E0").PaddingVertical(5).AlignRight().Text($"{itm.UnitPrice:N2}").FontSize(8);
                        table.Cell().BorderBottom(0.5f).BorderColor("#E0E0E0").PaddingVertical(5).AlignRight().Text($"{itm.LineTotal:N2}").FontSize(8.5f).Bold();
                        idx++;
                    }
                });

                // Summary
                col.Item().PaddingTop(15).Row(row =>
                {
                    row.RelativeItem(6).Column(c =>
                    {
                        c.Item().Text("PAYMENT METHOD:").FontSize(7.5f).Bold().FontColor("#777777");
                        foreach (var p in _invoice.Payments)
                        {
                            c.Item().Text($"• {p.Mode}: INR {p.Amount:N2}").FontSize(8);
                        }

                        c.Item().PaddingTop(10).Text("TERMS & CONDITIONS:").FontSize(7.5f).Bold().FontColor("#777777");
                        c.Item().Text(_company.TermsAndConditions).FontSize(6.8f).FontColor("#666666");
                    });

                    row.RelativeItem(6).AlignRight().Column(c =>
                    {
                        if (_invoice.IsGstEnabled)
                        {
                            c.Item().Row(r => { r.RelativeItem().Text("Taxable Value:").FontSize(8); r.AutoItem().Text($"INR {_invoice.StandardTaxableValue:N2}").FontSize(8); });
                            c.Item().Row(r => { r.RelativeItem().Text("CGST (9%):").FontSize(8); r.AutoItem().Text($"INR {_invoice.TotalCGST:N2}").FontSize(8); });
                            c.Item().Row(r => { r.RelativeItem().Text("SGST (9%):").FontSize(8); r.AutoItem().Text($"INR {_invoice.TotalSGST:N2}").FontSize(8); });
                        }

                        c.Item().PaddingTop(4).LineHorizontal(1).LineColor("#000000");
                        c.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("NET TOTAL:").FontSize(11).Bold();
                            r.AutoItem().Text($"INR {_invoice.GrandTotal:N2}").FontSize(12).Bold();
                        });
                    });
                });
            });

            // Minimalist Footer
            page.Footer().Column(col =>
            {
                col.Item().LineHorizontal(0.5f).LineColor("#CCCCCC");
                col.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Text("Sasan The Gadget Lounge - Premium Technology Retail").FontSize(7.5f).FontColor("#888888");
                    row.RelativeItem().AlignRight().Text($"For {_company.BusinessName.ToUpper()}  (Authorized Signatory)").FontSize(7.5f).Bold();
                });
            });
        });
    }
}
