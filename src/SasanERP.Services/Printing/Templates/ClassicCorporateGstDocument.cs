using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SasanERP.Core.Models;
using SasanERP.Services.Barcode;

namespace SasanERP.Services.Printing.Templates;

/// <summary>
/// A4 Style 7: Classic Corporate GST Tax Invoice (Indian Enterprise Standard)
/// Follows formal Tally/Busy enterprise formatting with dual tax breakup, HSN summary, and bank remittance.
/// Completely optimized for high-volume monochrome laser printing and professional color output.
/// </summary>
public class ClassicCorporateGstDocument : IDocument
{
    private readonly Invoice _invoice;
    private readonly CompanyProfile _company;
    private readonly BarcodeGeneratorService _barcodeService;

    public ClassicCorporateGstDocument(Invoice invoice, CompanyProfile company, BarcodeGeneratorService barcodeService)
    {
        _invoice = invoice;
        _company = company;
        _barcodeService = barcodeService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Tax_Invoice_{_invoice.InvoiceNumber}_ClassicGST",
        Author = _company.BusinessName
    };

    public void Compose(IDocumentContainer container)
    {
        bool isBw = _company.IsMonochromePrintMode;
        string primaryColor = isBw ? "#000000" : "#1E3A8A";
        string tableHeaderBg = isBw ? "#F1F5F9" : "#E2E8F0";
        string headerTextColor = "#0F172A";
        byte[]? logoBytes = LogoProvider.GetLogoBytes(isBw, _company.LogoPath);

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30, Unit.Point);
            page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Segoe UI").FontColor("#1E293B"));

            // 1. HEADER
            page.Header().Column(col =>
            {
                // Top Centered Title
                col.Item().Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        string docTitle = !_invoice.IsGstEnabled ? "RETAIL INVOICE / CASH MEMO" : "TAX INVOICE";
                        c.Item().Text(docTitle).FontSize(13).Bold().FontColor(primaryColor);
                        c.Item().Text("(Issued under Section 31 of Central Goods and Services Tax Act, 2017)")
                            .FontSize(7).Italic().FontColor("#64748B");
                    });
                    r.RelativeItem().AlignRight().Text("ORIGINAL FOR RECIPIENT").FontSize(8).Bold().FontColor("#475569");
                });

                col.Item().PaddingTop(4).LineHorizontal(1).LineColor("#94A3B8");

                // Store & Invoice Details
                col.Item().PaddingTop(6).Row(row =>
                {
                    // Left: Logo & Company Address
                    row.RelativeItem(7).Row(r =>
                    {
                        if (logoBytes != null)
                        {
                            r.ConstantItem(48).PaddingRight(10).Image(logoBytes).FitArea();
                        }
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text(_company.BusinessName.ToUpper()).FontSize(14).Bold().FontColor(primaryColor);
                            c.Item().Text(_company.Tagline).FontSize(7.5f).Italic().FontColor("#475569");
                            c.Item().Text(_company.FormattedSingleLineAddress)
                                .FontSize(7.5f);
                            c.Item().Text($"GSTIN / UIN: {_company.GSTIN} | State: {_company.State} (Code: {_company.StateCode})")
                                .FontSize(8).Bold();
                            c.Item().Text($"Contact: {_company.PhoneNumber} | Email: {_company.Email}").FontSize(7.5f);
                        });
                    });

                    // Right: Invoice Meta Grid
                    row.RelativeItem(5).Border(1).BorderColor("#CBD5E1").Padding(6).Column(c =>
                    {
                        c.Item().Row(ir => { ir.RelativeItem().Text("Invoice No:").FontSize(8).Bold(); ir.RelativeItem().AlignRight().Text(_invoice.InvoiceNumber).FontSize(8.5f).Bold().FontColor(primaryColor); });
                        c.Item().Row(ir => { ir.RelativeItem().Text("Dated:").FontSize(8); ir.RelativeItem().AlignRight().Text($"{_invoice.InvoiceDate:dd-MMM-yyyy}").FontSize(8); });
                        c.Item().Row(ir => { ir.RelativeItem().Text("Place of Supply:").FontSize(8); ir.RelativeItem().AlignRight().Text("33-Tamil Nadu").FontSize(8); });
                        c.Item().Row(ir => { ir.RelativeItem().Text("Reverse Charge:").FontSize(8); ir.RelativeItem().AlignRight().Text("No").FontSize(8); });
                    });
                });

                // Customer Box
                col.Item().PaddingTop(6).Border(1).BorderColor("#CBD5E1").Padding(6).Row(r =>
                {
                    r.RelativeItem(7).Column(c =>
                    {
                        c.Item().Text("Details of Receiver (Billed To):").FontSize(7.5f).Bold().FontColor("#64748B");
                        c.Item().Text(_invoice.Customer.FullName).FontSize(9.5f).Bold();
                        c.Item().Text($"Phone: {_invoice.Customer.PhoneNumber}").FontSize(8);
                        if (!string.IsNullOrWhiteSpace(_invoice.Customer.Address))
                            c.Item().Text($"Address: {_invoice.Customer.Address}").FontSize(7.5f);
                    });

                    r.RelativeItem(5).Column(c =>
                    {
                        c.Item().Text($"GSTIN: {(!string.IsNullOrWhiteSpace(_invoice.Customer.GSTIN) ? _invoice.Customer.GSTIN : "Unregistered")}").FontSize(8).Bold();
                        c.Item().Text("State / UT Name: Tamil Nadu (Code: 33)").FontSize(7.5f);
                    });
                });
            });

            // 2. CONTENT TABLE
            page.Content().Column(col =>
            {
                col.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(20);
                        cols.RelativeColumn(7);
                        cols.ConstantColumn(45);
                        cols.ConstantColumn(24);
                        cols.ConstantColumn(55);
                        cols.ConstantColumn(52);
                        cols.ConstantColumn(42);
                        cols.ConstantColumn(42);
                        cols.ConstantColumn(62);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Background(tableHeaderBg).Border(1).BorderColor("#CBD5E1").Padding(4).AlignCenter().Text("Sl").FontSize(7.5f).Bold().FontColor(headerTextColor);
                        h.Cell().Background(tableHeaderBg).Border(1).BorderColor("#CBD5E1").Padding(4).Text("Description of Goods").FontSize(7.5f).Bold().FontColor(headerTextColor);
                        h.Cell().Background(tableHeaderBg).Border(1).BorderColor("#CBD5E1").Padding(4).AlignCenter().Text("HSN").FontSize(7.5f).Bold().FontColor(headerTextColor);
                        h.Cell().Background(tableHeaderBg).Border(1).BorderColor("#CBD5E1").Padding(4).AlignCenter().Text("Qty").FontSize(7.5f).Bold().FontColor(headerTextColor);
                        h.Cell().Background(tableHeaderBg).Border(1).BorderColor("#CBD5E1").Padding(4).AlignRight().Text("Rate").FontSize(7.5f).Bold().FontColor(headerTextColor);
                        h.Cell().Background(tableHeaderBg).Border(1).BorderColor("#CBD5E1").Padding(4).AlignRight().Text("Taxable").FontSize(7.5f).Bold().FontColor(headerTextColor);
                        h.Cell().Background(tableHeaderBg).Border(1).BorderColor("#CBD5E1").Padding(4).AlignRight().Text("CGST").FontSize(7.5f).Bold().FontColor(headerTextColor);
                        h.Cell().Background(tableHeaderBg).Border(1).BorderColor("#CBD5E1").Padding(4).AlignRight().Text("SGST").FontSize(7.5f).Bold().FontColor(headerTextColor);
                        h.Cell().Background(tableHeaderBg).Border(1).BorderColor("#CBD5E1").Padding(4).AlignRight().Text("Amount (₹)").FontSize(7.5f).Bold().FontColor(headerTextColor);
                    });

                    int idx = 1;
                    foreach (var item in _invoice.Items)
                    {
                        table.Cell().Border(1).BorderColor("#E2E8F0").Padding(4).AlignCenter().Text(idx.ToString()).FontSize(7.5f);
                        
                        table.Cell().Border(1).BorderColor("#E2E8F0").Padding(4).Column(c =>
                        {
                            c.Item().Text(item.ItemDescription).FontSize(8.5f).Bold();
                            if (item.IsSerialized)
                            {
                                string imeiStr = $"IMEI 1: {item.IMEI1}";
                                if (!string.IsNullOrWhiteSpace(item.IMEI2)) imeiStr += $" | IMEI 2: {item.IMEI2}";
                                if (!string.IsNullOrWhiteSpace(item.SerialNumber)) imeiStr += $" | S/N: {item.SerialNumber}";
                                c.Item().Text(imeiStr).FontSize(7).FontFamily("Consolas");

                                string specStr = $"Battery: {item.BatteryHealthPercentage ?? 100}% | Grade: {item.ConditionGrade} | Warranty: {item.WarrantyStatus}";
                                if (item.IsMarginScheme) specStr += " | [Tax Computed Under Margin Scheme Rule 32(5)]";
                                c.Item().Text(specStr).FontSize(6.8f).FontColor("#64748B");
                            }
                        });

                        table.Cell().Border(1).BorderColor("#E2E8F0").Padding(4).AlignCenter().Text(item.HSNCode ?? "-").FontSize(7.5f);
                        table.Cell().Border(1).BorderColor("#E2E8F0").Padding(4).AlignCenter().Text(item.Quantity.ToString()).FontSize(7.5f).Bold();
                        table.Cell().Border(1).BorderColor("#E2E8F0").Padding(4).AlignRight().Text($"₹{item.UnitPrice:N0}").FontSize(7.5f);
                        table.Cell().Border(1).BorderColor("#E2E8F0").Padding(4).AlignRight().Text($"₹{item.TaxableValue:N0}").FontSize(7.5f);
                        table.Cell().Border(1).BorderColor("#E2E8F0").Padding(4).AlignRight().Text($"₹{item.CGSTAmount:N0}").FontSize(7.5f);
                        table.Cell().Border(1).BorderColor("#E2E8F0").Padding(4).AlignRight().Text($"₹{item.SGSTAmount:N0}").FontSize(7.5f);
                        table.Cell().Border(1).BorderColor("#E2E8F0").Padding(4).AlignRight().Text($"₹{item.LineTotal:N0}").FontSize(8).Bold();

                        idx++;
                    }
                });

                // Financial Summary Block
                col.Item().Border(1).BorderColor("#CBD5E1").Row(r =>
                {
                    // Left Column: Bank Details & UPI QR
                    r.RelativeItem(7).Padding(8).Column(c =>
                    {
                        c.Item().Text("Company Bank Remittance Mandate:").FontSize(7.5f).Bold().FontColor("#475569");
                        c.Item().Text($"Bank Name: {_company.BankName} | Branch: {_company.BankBranch}").FontSize(7.5f);
                        c.Item().Text($"Account No: {_company.BankAccountNumber} | RTGS/IFSC: {_company.BankIFSC}").FontSize(8).Bold();
                        c.Item().Text($"UPI VPA: {_company.UpiId}").FontSize(8).Bold().FontColor(primaryColor);

                        c.Item().PaddingTop(4).Text("Payment Split Details:").FontSize(7.5f).Bold();
                        if (_invoice.Payments != null && _invoice.Payments.Any())
                        {
                            foreach (var p in _invoice.Payments)
                            {
                                c.Item().Text($"• {p.Mode}: ₹{p.Amount:N0}").FontSize(7.5f);
                            }
                        }
                    });

                    // Center Column: Dynamic UPI QR
                    r.RelativeItem(2).AlignCenter().Padding(6).Column(qc =>
                    {
                        string upiUri = BarcodeGeneratorService.BuildUpiPaymentUri(
                            _company.UpiId ?? "9080803070@upi",
                            _company.BusinessName,
                            _invoice.GrandTotal,
                            _invoice.InvoiceNumber);
                        byte[] qrBytes = _barcodeService.GenerateQrCode(upiUri, 75);
                        qc.Item().Image(qrBytes).FitArea();
                        qc.Item().AlignCenter().Text("Scan to Pay").FontSize(6.5f).Bold();
                    });

                    // Right Column: Tax Breakdown & Grand Total
                    r.RelativeItem(5).BorderLeft(1).BorderColor("#CBD5E1").Padding(6).Column(tc =>
                    {
                        tc.Item().Row(row => { row.RelativeItem().Text("Sub Total:").FontSize(7.5f); row.RelativeItem().AlignRight().Text($"₹{_invoice.GrossTotal:N2}").FontSize(7.5f).Bold(); });
                        if (_invoice.InvoiceDiscount > 0)
                            tc.Item().Row(row => { row.RelativeItem().Text("Discount:").FontSize(7.5f); row.RelativeItem().AlignRight().Text($"-₹{_invoice.InvoiceDiscount:N2}").FontSize(7.5f); });

                        tc.Item().Row(row => { row.RelativeItem().Text("Taxable Value:").FontSize(7.5f); row.RelativeItem().AlignRight().Text($"₹{_invoice.TotalTaxableValue:N2}").FontSize(7.5f); });
                        tc.Item().Row(row => { row.RelativeItem().Text("CGST Total:").FontSize(7.5f); row.RelativeItem().AlignRight().Text($"₹{_invoice.TotalCGST:N2}").FontSize(7.5f); });
                        tc.Item().Row(row => { row.RelativeItem().Text("SGST Total:").FontSize(7.5f); row.RelativeItem().AlignRight().Text($"₹{_invoice.TotalSGST:N2}").FontSize(7.5f); });

                        if (_invoice.HasMarginSchemeItems)
                            tc.Item().Row(row => { row.RelativeItem().Text("Rule 32(5) Margin Tax:").FontSize(7.5f); row.RelativeItem().AlignRight().Text($"₹{_invoice.TotalMarginTax:N2}").FontSize(7.5f).Bold(); });

                        if (_invoice.RoundOff != 0)
                            tc.Item().Row(row => { row.RelativeItem().Text("Round Off:").FontSize(7.5f); row.RelativeItem().AlignRight().Text($"₹{_invoice.RoundOff:N2}").FontSize(7.5f); });

                        tc.Item().PaddingTop(4).BorderTop(1).BorderColor("#CBD5E1").Row(row =>
                        {
                            row.RelativeItem().Text("Total Amount:").FontSize(9).Bold().FontColor(primaryColor);
                            row.RelativeItem().AlignRight().Text($"₹{_invoice.GrandTotal:N0}").FontSize(11).Bold().FontColor(primaryColor);
                        });
                    });
                });
            });

            // 3. FOOTER
            page.Footer().Column(col =>
            {
                col.Item().PaddingTop(4).Border(1).BorderColor("#CBD5E1").Padding(6).Row(r =>
                {
                    r.RelativeItem(7).Column(c =>
                    {
                        c.Item().Text("Declaration:").FontSize(7).Bold();
                        c.Item().Text("We declare that this invoice shows the actual price of the goods described and that all particulars are true and correct.")
                            .FontSize(6.5f).Italic().FontColor("#475569");
                        c.Item().PaddingTop(2).Text(_company.TermsAndConditions).FontSize(6.2f).FontColor("#64748B");
                    });

                    r.RelativeItem(5).AlignRight().Column(c =>
                    {
                        c.Item().AlignCenter().Text($"For {_company.BusinessName.ToUpper()}").FontSize(7.5f).Bold();
                        c.Item().Height(28); // Signature spacing
                        c.Item().AlignCenter().Text("Authorized Signatory").FontSize(7).Bold();
                    });
                });
            });
        });
    }
}
