using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SasanERP.Core.Models;
using SasanERP.Services.Barcode;

namespace SasanERP.Services.Printing;

/// <summary>
/// World-Class Vector A4 GST Tax Invoice Document Generator using QuestPDF
/// Specifically tailored for Sasan The Gadget Lounge with IMEI tracking and Rule 32(5) compliance.
/// </summary>
public class A4InvoiceDocument : IDocument
{
    private readonly Invoice _invoice;
    private readonly CompanyProfile _company;
    private readonly BarcodeGeneratorService _barcodeService;

    // Brand Palette: Dark Slate, Electric Cyan, Warm Amber
    private const string PrimaryColor = "#0B192C";
    private const string AccentCyan = "#00B4D8";
    private const string WarmAmber = "#E65100";
    private const string BackgroundLight = "#F8F9FA";
    private const string BorderGray = "#DEE2E6";

    public A4InvoiceDocument(Invoice invoice, CompanyProfile company, BarcodeGeneratorService barcodeService)
    {
        _invoice = invoice;
        _company = company;
        _barcodeService = barcodeService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Invoice_{_invoice.InvoiceNumber}",
        Author = _company.BusinessName,
        Subject = "GST Tax Invoice"
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(25, Unit.Point);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Segoe UI").FontColor("#212529"));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                // Left: Company Branding
                row.RelativeItem(7).Column(c =>
                {
                    c.Item().Text(_company.BusinessName.ToUpper())
                        .FontSize(20).Bold().FontColor(PrimaryColor);
                    
                    c.Item().Text(_company.Tagline)
                        .FontSize(9).Italic().FontColor(AccentCyan);

                    c.Item().PaddingTop(2).Text($"{_company.AddressLine1}, {_company.AddressLine2}")
                        .FontSize(8.5f).FontColor("#495057");

                    c.Item().Text($"{_company.City}, {_company.State} - {_company.Pincode} (State Code: {_company.StateCode})")
                        .FontSize(8.5f).FontColor("#495057");

                    c.Item().Text($"Phone: +91 {_company.PhoneNumber} | Email: {_company.Email}")
                        .FontSize(8.5f).Bold().FontColor("#343A40");

                    c.Item().Text($"GSTIN: {_company.GSTIN}")
                        .FontSize(9).Bold().FontColor(PrimaryColor);
                });

                // Right: Invoice Header & Type
                row.RelativeItem(5).Column(c =>
                {
                    c.Item().AlignRight().Container()
                        .Background(_invoice.HasMarginSchemeItems ? WarmAmber : PrimaryColor)
                        .PaddingVertical(4).PaddingHorizontal(10)
                        .Text(_invoice.HasMarginSchemeItems ? "TAX INVOICE / MARGIN SCHEME" : "TAX INVOICE")
                        .FontSize(11).Bold().FontColor(Colors.White);

                    c.Item().PaddingTop(4).AlignRight().Text(t =>
                    {
                        t.Span("Invoice No: ").Bold();
                        t.Span(_invoice.InvoiceNumber).Bold().FontColor(PrimaryColor);
                    });

                    c.Item().AlignRight().Text(t =>
                    {
                        t.Span("Date: ").Bold();
                        t.Span(_invoice.InvoiceDate.ToString("dd-MMM-yyyy hh:mm tt"));
                    });

                    c.Item().AlignRight().Text(t =>
                    {
                        t.Span("Place of Supply: ").Bold();
                        t.Span($"{_company.StateCode} - {_company.State}");
                    });

                    c.Item().AlignRight().Text(t =>
                    {
                        t.Span("Billed By: ").Bold();
                        t.Span(_invoice.BilledBy);
                    });
                });
            });

            col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(PrimaryColor);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            // Customer & Billing Info Box
            col.Item().PaddingVertical(6).Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(BorderGray).Background(BackgroundLight).Padding(8).Column(c =>
                {
                    c.Item().Text("CUSTOMER DETAILS (BILL TO):").FontSize(8.5f).Bold().FontColor(PrimaryColor);
                    c.Item().Text(_invoice.Customer.FullName).FontSize(10).Bold();
                    c.Item().Text($"Mobile: {_invoice.Customer.PhoneNumber}").FontSize(8.5f);
                    if (!string.IsNullOrWhiteSpace(_invoice.Customer.Address))
                        c.Item().Text($"Address: {_invoice.Customer.Address}, {_invoice.Customer.City}").FontSize(8.5f);
                    if (!string.IsNullOrWhiteSpace(_invoice.Customer.GSTIN))
                        c.Item().Text($"Customer GSTIN: {_invoice.Customer.GSTIN}").FontSize(8.5f).Bold();
                });
            });

            // Items Grid
            col.Item().Element(ComposeItemsTable);

            // Statutory Margin Scheme Notice (if applicable)
            if (_invoice.HasMarginSchemeItems)
            {
                col.Item().PaddingTop(4).Container()
                    .Border(1).BorderColor(WarmAmber).Background("#FFF8E1").Padding(6)
                    .Text(t =>
                    {
                        t.Span("Statutory Notice (Rule 32(5) CGST Rules, 2017): ").Bold().FontColor(WarmAmber);
                        t.Span("Items marked with (*) are pre-owned/second-hand gadgets supplied under the GST Margin Scheme. No Input Tax Credit (ITC) has been availed on purchase, and tax is charged only on value addition/margin.")
                         .FontSize(8).FontColor("#5D4037");
                    });
            }

            // Financial Summary & UPI QR Section
            col.Item().PaddingTop(8).Row(row =>
            {
                // Left: Dynamic UPI QR Code & Bank Account Info
                row.RelativeItem(5).Column(c =>
                {
                    c.Item().Border(1).BorderColor(BorderGray).Padding(6).Row(qrRow =>
                    {
                        // Generate UPI QR Code
                        string upiUri = BarcodeGeneratorService.BuildUpiPaymentUri(
                            _company.UpiId ?? "9080803070@sbi",
                            _company.BusinessName,
                            _invoice.GrandTotal,
                            _invoice.InvoiceNumber);

                        byte[] qrBytes = _barcodeService.GenerateQrCode(upiUri, 110);

                        qrRow.ConstantItem(75).Column(qrCol =>
                        {
                            if (qrBytes.Length > 0)
                                qrCol.Item().Image(qrBytes);
                        });

                        qrRow.RelativeItem().PaddingLeft(6).Column(bankCol =>
                        {
                            bankCol.Item().Text("SCAN TO PAY (UPI)").FontSize(8).Bold().FontColor(AccentCyan);
                            bankCol.Item().Text($"UPI ID: {_company.UpiId}").FontSize(7.5f).Bold();
                            bankCol.Item().PaddingTop(2).Text("BANK TRANSFER (NEFT/RTGS):").FontSize(7.5f).Bold().FontColor(PrimaryColor);
                            bankCol.Item().Text($"Bank: {_company.BankName}").FontSize(7.5f);
                            bankCol.Item().Text($"A/c No: {_company.BankAccountNumber}").FontSize(7.5f).Bold();
                            bankCol.Item().Text($"IFSC: {_company.BankIFSC} | Branch: {_company.BankBranch}").FontSize(7.5f);
                        });
                    });

                    // Payment Tender Breakdown
                    c.Item().PaddingTop(4).Border(1).BorderColor(BorderGray).Background(BackgroundLight).Padding(4).Column(payCol =>
                    {
                        payCol.Item().Text("PAYMENT RECEIVED SUMMARY:").FontSize(8).Bold().FontColor(PrimaryColor);
                        foreach (var pay in _invoice.Payments)
                        {
                            payCol.Item().Row(pr =>
                            {
                                pr.RelativeItem().Text($"• {pay.Mode}:").FontSize(7.5f);
                                pr.AutoItem().Text($"INR {pay.Amount:N2}").FontSize(7.5f).Bold();
                            });
                        }
                    });
                });

                // Right: Financial Totals Table
                row.RelativeItem(7).PaddingLeft(10).Column(c =>
                {
                    c.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(3);
                            cd.RelativeColumn(2);
                        });

                        AddSummaryRow(table, "Total Taxable Value:", $"INR {_invoice.StandardTaxableValue:N2}");
                        AddSummaryRow(table, "CGST (Central Tax):", $"INR {_invoice.TotalCGST:N2}");
                        AddSummaryRow(table, "SGST (State Tax):", $"INR {_invoice.TotalSGST:N2}");

                        if (_invoice.InvoiceDiscount > 0)
                            AddSummaryRow(table, "Bill Discount:", $"- INR {_invoice.InvoiceDiscount:N2}");

                        if (_invoice.RoundOff != 0)
                            AddSummaryRow(table, "Round Off Adjustment:", $"INR {_invoice.RoundOff:N2}");

                        // Grand Total Highlight
                        table.Cell().Background(PrimaryColor).Padding(5)
                            .Text("GRAND TOTAL:").FontSize(11).Bold().FontColor(Colors.White);

                        table.Cell().Background(PrimaryColor).Padding(5).AlignRight()
                            .Text($"INR {_invoice.GrandTotal:N2}").FontSize(12).Bold().FontColor("#00E5FF");
                    });

                    c.Item().PaddingTop(4).Text(t =>
                    {
                        t.Span("Amount in Words: ").Bold().FontSize(8);
                        t.Span(NumberToWords((long)_invoice.GrandTotal) + " Rupees Only").Italic().FontSize(8);
                    });
                });
            });
        });
    }

    private void ComposeItemsTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(22); // S.No
                columns.RelativeColumn(7);  // Description & Serial Specs
                columns.ConstantColumn(42); // HSN
                columns.ConstantColumn(26); // Qty
                columns.ConstantColumn(58); // Rate
                columns.ConstantColumn(46); // Disc
                columns.ConstantColumn(54); // Taxable
                columns.ConstantColumn(40); // GST %
                columns.ConstantColumn(64); // Total (INR)
            });

            // Table Header
            table.Header(header =>
            {
                header.Cell().Background(PrimaryColor).Padding(4).Text("#").FontSize(8).Bold().FontColor(Colors.White);
                header.Cell().Background(PrimaryColor).Padding(4).Text("ITEM & DEVICE IDENTIFICATION").FontSize(8).Bold().FontColor(Colors.White);
                header.Cell().Background(PrimaryColor).Padding(4).AlignCenter().Text("HSN").FontSize(8).Bold().FontColor(Colors.White);
                header.Cell().Background(PrimaryColor).Padding(4).AlignCenter().Text("QTY").FontSize(8).Bold().FontColor(Colors.White);
                header.Cell().Background(PrimaryColor).Padding(4).AlignRight().Text("RATE").FontSize(8).Bold().FontColor(Colors.White);
                header.Cell().Background(PrimaryColor).Padding(4).AlignRight().Text("DISC").FontSize(8).Bold().FontColor(Colors.White);
                header.Cell().Background(PrimaryColor).Padding(4).AlignRight().Text("TAXABLE").FontSize(8).Bold().FontColor(Colors.White);
                header.Cell().Background(PrimaryColor).Padding(4).AlignCenter().Text("GST%").FontSize(8).Bold().FontColor(Colors.White);
                header.Cell().Background(PrimaryColor).Padding(4).AlignRight().Text("AMOUNT").FontSize(8).Bold().FontColor(Colors.White);
            });

            int index = 1;
            foreach (var item in _invoice.Items)
            {
                string bg = (index % 2 == 0) ? BackgroundLight : Colors.White;

                table.Cell().Background(bg).Padding(4).Text(index.ToString()).FontSize(8);

                table.Cell().Background(bg).Padding(4).Column(col =>
                {
                    col.Item().Text(t =>
                    {
                        if (item.IsMarginScheme)
                            t.Span("(*) ").Bold().FontColor(WarmAmber);
                        t.Span(item.ItemDescription).Bold().FontSize(8.5f);
                    });

                    // Print IMEI and Device Diagnostics if serialized
                    if (!string.IsNullOrWhiteSpace(item.IMEI1) || !string.IsNullOrWhiteSpace(item.SerialNumber))
                    {
                        var specs = new List<string>();
                        if (!string.IsNullOrWhiteSpace(item.IMEI1)) specs.Add($"IMEI 1: {item.IMEI1}");
                        if (!string.IsNullOrWhiteSpace(item.IMEI2)) specs.Add($"IMEI 2: {item.IMEI2}");
                        if (!string.IsNullOrWhiteSpace(item.SerialNumber)) specs.Add($"S/N: {item.SerialNumber}");
                        if (item.BatteryHealthPercentage.HasValue) specs.Add($"Battery: {item.BatteryHealthPercentage}%");
                        if (item.ConditionGrade.HasValue) specs.Add($"Grade: {item.ConditionGrade}");
                        if (item.WarrantyStatus.HasValue) specs.Add($"Warranty: {item.WarrantyStatus}");

                        col.Item().Text(string.Join(" | ", specs))
                            .FontSize(7.2f).FontColor("#495057").Italic();
                    }
                });

                table.Cell().Background(bg).Padding(4).AlignCenter().Text(item.HSNCode).FontSize(8);
                table.Cell().Background(bg).Padding(4).AlignCenter().Text(item.Quantity.ToString("0")).FontSize(8);
                table.Cell().Background(bg).Padding(4).AlignRight().Text($"{item.UnitPrice:N0}").FontSize(8);
                table.Cell().Background(bg).Padding(4).AlignRight().Text($"{item.ItemDiscount:N0}").FontSize(8);
                table.Cell().Background(bg).Padding(4).AlignRight().Text($"{item.TaxableValue:N2}").FontSize(8);
                table.Cell().Background(bg).Padding(4).AlignCenter().Text($"{item.GSTPercentage:0}%").FontSize(8);
                table.Cell().Background(bg).Padding(4).AlignRight().Text($"{item.LineTotal:N2}").FontSize(8.5f).Bold();

                index++;
            }
        });
    }

    private static void AddSummaryRow(TableDescriptor table, string label, string value)
    {
        table.Cell().BorderBottom(0.5f).BorderColor(BorderGray).PaddingVertical(2).Text(label).FontSize(8).Bold();
        table.Cell().BorderBottom(0.5f).BorderColor(BorderGray).PaddingVertical(2).AlignRight().Text(value).FontSize(8).Bold();
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(BorderGray);

            col.Item().PaddingTop(4).Row(row =>
            {
                // Left: Terms and Conditions
                row.RelativeItem(8).Column(c =>
                {
                    c.Item().Text("TERMS & CONDITIONS:").FontSize(7.5f).Bold().FontColor(PrimaryColor);
                    c.Item().Text(_company.TermsAndConditions).FontSize(6.8f).FontColor("#6C757D");
                });

                // Right: Authorized Signatory Block
                row.RelativeItem(4).Column(c =>
                {
                    c.Item().AlignRight().Text($"For {_company.BusinessName.ToUpper()}").FontSize(8).Bold();
                    c.Item().PaddingTop(28).AlignRight().Text("AUTHORIZED SIGNATORY").FontSize(7.5f).Bold();
                    c.Item().AlignRight().Text("(Computer Generated Invoice)").FontSize(6.5f).Italic();
                });
            });
        });
    }

    private static string NumberToWords(long number)
    {
        if (number == 0) return "Zero";
        if (number < 0) return "Minus " + NumberToWords(Math.Abs(number));

        string words = "";

        if ((number / 10000000) > 0)
        {
            words += NumberToWords(number / 10000000) + " Crore ";
            number %= 10000000;
        }

        if ((number / 100000) > 0)
        {
            words += NumberToWords(number / 100000) + " Lakh ";
            number %= 100000;
        }

        if ((number / 1000) > 0)
        {
            words += NumberToWords(number / 1000) + " Thousand ";
            number %= 1000;
        }

        if ((number / 100) > 0)
        {
            words += NumberToWords(number / 100) + " Hundred ";
            number %= 100;
        }

        if (number > 0)
        {
            var unitsMap = new[] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
            var tensMap = new[] { "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

            if (number < 20)
                words += unitsMap[number];
            else
            {
                words += tensMap[number / 10];
                if ((number % 10) > 0)
                    words += " " + unitsMap[number % 10];
            }
        }

        return words.Trim();
    }
}
