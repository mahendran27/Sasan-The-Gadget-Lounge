using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SasanERP.Core.Models;
using SasanERP.Services.Barcode;

namespace SasanERP.Services.Printing.Templates;

/// <summary>
/// Choice 3: Rule 32(5) Margin Scheme Audit Invoice Template (A4 Vector)
/// Built strictly for second-hand / pre-owned gadgets under Rule 32(5) of the CGST Rules, 2017.
/// Features statutory legal preambles, margin breakdown table, non-ITC declarations, and CA-auditable records.
/// </summary>
public class Rule32_5_MarginSchemeAuditDocument : IDocument
{
    private readonly Invoice _invoice;
    private readonly CompanyProfile _company;
    private readonly BarcodeGeneratorService _barcodeService;

    public Rule32_5_MarginSchemeAuditDocument(Invoice invoice, CompanyProfile company, BarcodeGeneratorService barcodeService)
    {
        _invoice = invoice;
        _company = company;
        _barcodeService = barcodeService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"GST_MarginScheme_Invoice_{_invoice.InvoiceNumber}",
        Author = _company.BusinessName
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30, Unit.Point);
            page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Segoe UI").FontColor("#1E293B"));

            // Statutory Header
            page.Header().Column(col =>
            {
                // Top Statutory Declaration Ribbon
                col.Item().Background("#FEF3C7").Border(1).BorderColor("#F59E0B").Padding(6).Column(c =>
                {
                    c.Item().AlignCenter().Text("TAX INVOICE UNDER RULE 32(5) OF CGST RULES, 2017")
                        .FontSize(10).Bold().FontColor("#92400E");
                    c.Item().AlignCenter().Text("SUPPLY OF PRE-OWNED / SECOND-HAND GOODS — TAX COMPUTED SOLELY ON MARGIN")
                        .FontSize(7.5f).Bold().FontColor("#B45309");
                });

                bool isBw = _company.IsMonochromePrintMode;
                byte[]? logoBytes = LogoProvider.GetLogoBytes(isBw, _company.LogoPath);

                col.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem(7).Row(r =>
                    {
                        if (logoBytes != null)
                        {
                            r.ConstantItem(48).PaddingRight(10).Image(logoBytes).FitArea();
                        }
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text(_company.BusinessName.ToUpper()).FontSize(14).Bold().FontColor("#0F172A");
                            c.Item().Text(_company.FormattedSingleLineAddress)
                                .FontSize(7.8f).FontColor("#475569");
                            c.Item().Text($"GSTIN: {_company.GSTIN} | State Code: {_company.StateCode} (Tamil Nadu) | Phone: {_company.PhoneNumber}")
                                .FontSize(7.8f).FontColor("#475569");
                            c.Item().Text("Specialized Registered Dealer in Pre-Owned Mobile Phones & Laptops").FontSize(7.2f).Italic().FontColor("#64748B");
                        });
                    });

                    row.RelativeItem(5).AlignRight().Column(c =>
                    {
                        c.Item().Border(1).BorderColor("#CBD5E1").Padding(6).Column(ic =>
                        {
                            ic.Item().Text($"INVOICE NO: {_invoice.InvoiceNumber}").FontSize(9).Bold().FontColor("#0F172A");
                            ic.Item().Text($"Date: {_invoice.InvoiceDate:dd-MM-yyyy}").FontSize(8.5f);
                            ic.Item().Text($"Place of Supply: 33-Tamil Nadu").FontSize(8f);
                            ic.Item().Text("Reverse Charge: NO").FontSize(8f);
                        });
                    });
                });

                col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#94A3B8");
            });

            // Content
            page.Content().Column(col =>
            {
                // Buyer Details & Rule 32(5) Notice - Unified Card (Zero Stray Vertical Lines)
                col.Item().PaddingTop(10).Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Row(row =>
                {
                    row.RelativeItem(6).Padding(8).Column(c =>
                    {
                        c.Item().Text("DETAILS OF RECIPIENT / BUYER").FontSize(7.5f).Bold().FontColor("#475569");
                        c.Item().Text(_invoice.Customer.FullName).FontSize(10).Bold();
                        c.Item().Text($"Mobile: {_invoice.Customer.PhoneNumber}").FontSize(8f);
                        if (!string.IsNullOrWhiteSpace(_invoice.Customer.Address))
                            c.Item().Text($"Address: {_invoice.Customer.Address}").FontSize(8f);
                        if (!string.IsNullOrWhiteSpace(_invoice.Customer.GSTIN))
                            c.Item().Text($"Recipient GSTIN: {_invoice.Customer.GSTIN}").FontSize(8f).Bold();
                    });

                    row.ConstantItem(1).Background("#E2E8F0");

                    row.RelativeItem(6).Padding(8).Column(c =>
                    {
                        c.Item().Text("STATUTORY RULE 32(5) NOTICE").FontSize(7.5f).Bold().FontColor("#92400E");
                        c.Item().Text("As per Rule 32(5) of the Central Goods and Services Tax Rules, 2017, the value of supply of second-hand goods shall be the difference between the selling price and purchase price. Where the value of such supply is negative, it shall be ignored.").FontSize(7f).FontColor("#64748B");
                        c.Item().PaddingTop(2).Text("NO INPUT TAX CREDIT (ITC) IS ADMISSIBLE TO THE RECIPIENT.").FontSize(7f).Bold().FontColor("#DC2626");
                    });
                });

                // Margin Scheme Audit Table
                col.Item().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(20);
                        cols.RelativeColumn(6);
                        cols.ConstantColumn(40);
                        cols.ConstantColumn(55);
                        cols.ConstantColumn(55);
                        cols.ConstantColumn(50);
                        cols.ConstantColumn(50);
                        cols.ConstantColumn(65);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Background("#1E293B").Padding(4).Text("#").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background("#1E293B").Padding(4).Text("DESCRIPTION, IMEI & CONDITION").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background("#1E293B").Padding(4).AlignCenter().Text("HSN").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background("#1E293B").Padding(4).AlignRight().Text("SALE PRICE").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background("#1E293B").Padding(4).AlignRight().Text("PURCHASE").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background("#1E293B").Padding(4).AlignRight().Text("MARGIN").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background("#1E293B").Padding(4).AlignRight().Text("GST (18%)").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                        h.Cell().Background("#1E293B").Padding(4).AlignRight().Text("TOTAL (₹)").FontSize(7.5f).Bold().FontColor("#FFFFFF");
                    });

                    int idx = 1;
                    foreach (var itm in _invoice.Items)
                    {
                        string bg = (idx % 2 == 0) ? "#F8FAFC" : "#FFFFFF";

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(4).Text(idx.ToString()).FontSize(7.5f);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(4).Column(c =>
                        {
                            c.Item().Text(itm.ItemDescription).FontSize(8.5f).Bold();
                            if (!string.IsNullOrEmpty(itm.IMEI1) || !string.IsNullOrEmpty(itm.SerialNumber))
                            {
                                c.Item().Text($"IMEI: {itm.IMEI1 ?? itm.SerialNumber} | Grade: {itm.ConditionGrade} | Battery: {itm.BatteryHealthPercentage}%")
                                    .FontSize(7f).FontColor("#475569");
                            }
                        });

                        decimal purchaseCost = itm.UnitCostPrice;
                        decimal margin = itm.MarginAmount;
                        decimal gstOnMargin = (itm.CGSTAmount + itm.SGSTAmount + itm.IGSTAmount);

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(4).AlignCenter().Text(itm.HSNCode).FontSize(7.5f);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(4).AlignRight().Text($"₹{itm.LineTotal:N2}").FontSize(7.5f);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(4).AlignRight().Text($"₹{(purchaseCost * itm.Quantity):N2}").FontSize(7.5f);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(4).AlignRight().Text($"₹{margin:N2}").FontSize(7.5f).Bold();
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(4).AlignRight().Text($"₹{gstOnMargin:N2}").FontSize(7.5f);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#CBD5E1").Padding(4).AlignRight().Text($"₹{itm.LineTotal:N2}").FontSize(8f).Bold();

                        idx++;
                    }
                });

                // Margin Scheme Audit Tax Box
                col.Item().PaddingTop(12).Row(row =>
                {
                    row.RelativeItem(7).Column(c =>
                    {
                        c.Item().Text("CA AUDIT & STATUTORY COMPLIANCE NOTES").FontSize(8).Bold().FontColor("#0F172A");
                        c.Item().Text("1. Tax liability under Rule 32(5) is computed as: Output GST = Margin Amount × (GST Rate / 100).").FontSize(7f);
                        c.Item().Text("2. If purchase cost equals or exceeds sale value, Margin = 0, Output GST = NIL.").FontSize(7f);
                        c.Item().Text("3. This record is linked to statutory purchase buyback registers with Aadhaar/Govt ID verification.").FontSize(7f);

                        // Barcode for audit
                        byte[] barcodeBytes = _barcodeService.GenerateCode128Barcode(_invoice.InvoiceNumber, 240, 36);
                        if (barcodeBytes.Length > 0)
                        {
                            c.Item().PaddingTop(8).Height(24, Unit.Point).Image(barcodeBytes);
                        }
                    });

                    row.RelativeItem(5).Border(1).BorderColor("#1E293B").Padding(8).Column(c =>
                    {
                        c.Item().Row(r => { r.RelativeItem().Text("Total Sale Value:").FontSize(8); r.AutoItem().Text($"₹{_invoice.GrandTotal:N2}").FontSize(8).Bold(); });
                        c.Item().Row(r => { r.RelativeItem().Text("Total Margin Base:").FontSize(8); r.AutoItem().Text($"₹{_invoice.TotalMarginAmount:N2}").FontSize(8); });
                        c.Item().Row(r => { r.RelativeItem().Text("CGST on Margin:").FontSize(8); r.AutoItem().Text($"₹{_invoice.TotalCGST:N2}").FontSize(8); });
                        c.Item().Row(r => { r.RelativeItem().Text("SGST on Margin:").FontSize(8); r.AutoItem().Text($"₹{_invoice.TotalSGST:N2}").FontSize(8); });

                        c.Item().PaddingVertical(4).LineHorizontal(1).LineColor("#1E293B");

                        c.Item().Background("#1E293B").Padding(4).Row(r =>
                        {
                            r.RelativeItem().Text("NET INVOICE PAYABLE:").FontSize(9).Bold().FontColor("#FFFFFF");
                            r.AutoItem().Text($"₹{_invoice.GrandTotal:N2}").FontSize(11).Bold().FontColor("#FBBF24");
                        });
                    });
                });
            });

            // Legal Footer
            page.Footer().Column(col =>
            {
                col.Item().LineHorizontal(0.5f).LineColor("#CBD5E1");
                col.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem(7).Text("Subject to Theni Jurisdiction | Electronic Signature Validated under IT Act, 2000").FontSize(7f).FontColor("#64748B");
                    row.RelativeItem(5).AlignRight().Text($"For {_company.BusinessName.ToUpper()}  (Authorized Signatory)").FontSize(7.5f).Bold();
                });
            });
        });
    }
}
