using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SasanERP.Core.Models;
using SasanERP.Services.Analytics;
using SasanERP.Services.Barcode;

namespace SasanERP.Services.Printing;

/// <summary>
/// Executive PDF Report Generator for Sasan The Gadget Lounge.
/// Produces publication-quality A4 vector reports for Period Sales Audits,
/// Shift Day-End Z-Reports, Inventory Valuation, and Customer Khata Statements.
/// </summary>
public class ExecutiveReportPdfEngine
{
    private readonly BarcodeGeneratorService _barcodeService;

    public ExecutiveReportPdfEngine(BarcodeGeneratorService barcodeService)
    {
        _barcodeService = barcodeService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Generates vector A4 Shift / Period Sales Reconciliation Report (Day-End Z-Report)
    /// </summary>
    public void GeneratePeriodSalesReportPdf(
        PeriodReportDto report, 
        CompanyProfile company, 
        string outputPath)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(9.5f).FontColor("#0F172A"));

                // Header
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem(7).Column(c =>
                        {
                            c.Item().Text(company.BusinessName.ToUpperInvariant())
                                .FontSize(16).Bold().FontColor("#0F172A");
                            c.Item().Text($"{company.AddressLine1}, {company.AddressLine2}, {company.City}, {company.State} - {company.Pincode}")
                                .FontSize(8.5f).FontColor("#475569");
                            c.Item().Text($"GSTIN: {company.GSTIN} | Tel: {company.PhoneNumber} | Email: {company.Email}")
                                .FontSize(8.5f).FontColor("#475569");
                        });

                        row.RelativeItem(3).AlignRight().Column(c =>
                        {
                            c.Item().Border(1).BorderColor("#0284C7").Background("#F0F9FF").Padding(6).Column(ic =>
                            {
                                ic.Item().Text("SHIFT / SALES AUDIT")
                                    .FontSize(10).Bold().FontColor("#0369A1");
                                ic.Item().Text($"Printed: {DateTime.Now:dd-MMM-yyyy hh:mm tt}")
                                    .FontSize(7.5f).FontColor("#64748B");
                            });
                        });
                    });

                    col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor("#0284C7");

                    // Filter Period Badge
                    col.Item().PaddingTop(6).Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(8).Row(r =>
                    {
                        r.RelativeItem().Text(t =>
                        {
                            t.Span("AUDIT WINDOW: ").Bold().FontColor("#334155");
                            t.Span($"{report.FromUtc.ToLocalTime():dd-MMM-yyyy hh:mm tt}  to  {report.ToUtc.ToLocalTime():dd-MMM-yyyy hh:mm tt}")
                                .Bold().FontColor("#0284C7");
                        });
                        r.RelativeItem().AlignRight().Text(t =>
                        {
                            t.Span("INVOICES BILLED: ").Bold().FontColor("#334155");
                            t.Span($"{report.TotalInvoicesCount} Bills ({report.TotalItemsSoldCount} Items)")
                                .Bold().FontColor("#16A34A");
                        });
                    });
                });

                // Content
                page.Content().PaddingTop(12).Column(col =>
                {
                    // 1. Executive Summary Cards Grid
                    col.Item().Row(row =>
                    {
                        row.Spacing(8);

                        // Gross Sales
                        row.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(8).Column(c =>
                        {
                            c.Item().Text("GROSS SALES").FontSize(8).Bold().FontColor("#64748B");
                            c.Item().Text($"₹{report.TotalGrossSales:N2}").FontSize(12).Bold().FontColor("#0F172A");
                        });

                        // Discounts
                        row.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(8).Column(c =>
                        {
                            c.Item().Text("DISCOUNTS ALLOWED").FontSize(8).Bold().FontColor("#64748B");
                            c.Item().Text($"₹{report.TotalDiscounts:N2}").FontSize(12).Bold().FontColor("#DC2626");
                        });

                        // Net Revenue
                        row.RelativeItem().Border(1).BorderColor("#0284C7").Background("#F0F9FF").Padding(8).Column(c =>
                        {
                            c.Item().Text("NET REVENUE (BILLED)").FontSize(8).Bold().FontColor("#0369A1");
                            c.Item().Text($"₹{report.TotalNetRevenue:N2}").FontSize(13).Bold().FontColor("#0284C7");
                        });

                        // Net Profit
                        row.RelativeItem().Border(1).BorderColor("#16A34A").Background("#F0FDF4").Padding(8).Column(c =>
                        {
                            c.Item().Text("ESTIMATED NET PROFIT").FontSize(8).Bold().FontColor("#15803D");
                            c.Item().Text($"₹{report.TotalProfitEstimated:N2}").FontSize(13).Bold().FontColor("#16A34A");
                        });
                    });

                    // 2. Tender Collection Split Table
                    col.Item().PaddingTop(12).Text("PAYMENT MODE & REGISTER RECONCILIATION")
                        .FontSize(10.5f).Bold().FontColor("#0F172A");

                    col.Item().PaddingTop(4).Table(tbl =>
                    {
                        tbl.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3); // Mode
                            cols.RelativeColumn(3); // Amount
                            cols.RelativeColumn(4); // Verification Note
                        });

                        tbl.Header(h =>
                        {
                            h.Cell().Background("#0F172A").Padding(5).Text("TENDER MODE").FontSize(8.5f).Bold().FontColor(Colors.White);
                            h.Cell().Background("#0F172A").Padding(5).AlignRight().Text("COLLECTION (₹)").FontSize(8.5f).Bold().FontColor(Colors.White);
                            h.Cell().Background("#0F172A").Padding(5).Text("VERIFICATION / CHANNEL").FontSize(8.5f).Bold().FontColor(Colors.White);
                        });

                        void AddTenderRow(string mode, decimal amt, string note)
                        {
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).Text(mode).Bold();
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).AlignRight().Text($"₹{amt:N2}").Bold();
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).Text(note).FontSize(8).FontColor("#64748B");
                        }

                        AddTenderRow("Cash in Hand", report.CashCollected, "Physical cash drawer currency tally");
                        AddTenderRow("UPI / QR Credits", report.UpiCollected, "SBI Current A/c instant bank credits");
                        AddTenderRow("Card Swipes (POS)", report.CardCollected, "PineLabs / EDC machine EDC batch settlement");
                        AddTenderRow("Old Phone Exchange", report.ExchangeDeductions, "Customer trade-in devices added to pre-owned stock");
                        AddTenderRow("Finance / EMI", report.FinanceEmiCollected, "Bajaj Finserv / HDB / PineLabs EMI approval");
                        AddTenderRow("Customer Credit (Khata)", report.CustomerCreditAdded, "Unpaid balance added to customer ledger receivables");
                    });

                    // 3. Tax Breakdown Summary
                    col.Item().PaddingTop(10).Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(8).Row(r =>
                    {
                        r.RelativeItem().Text(t =>
                        {
                            t.Span("Standard Output GST: ").FontColor("#64748B");
                            t.Span($"₹{report.StandardTaxCollected:N2}").Bold().FontColor("#0F172A");
                        });
                        r.RelativeItem().Text(t =>
                        {
                            t.Span("Rule 32(5) Margin GST: ").FontColor("#64748B");
                            t.Span($"₹{report.MarginTaxCollected:N2}").Bold().FontColor("#EA580C");
                        });
                        r.RelativeItem().Text(t =>
                        {
                            t.Span("Stock in Hand: ").FontColor("#64748B");
                            t.Span($"{report.ActiveStockUnitsCount} Devices (₹{report.ActiveStockValuationCost:N0} Cost)").Bold().FontColor("#0284C7");
                        });
                    });

                    // 4. Invoices itemized list
                    col.Item().PaddingTop(12).Text("INVOICES REGISTER IN PERIOD")
                        .FontSize(10.5f).Bold().FontColor("#0F172A");

                    col.Item().PaddingTop(4).Table(tbl =>
                    {
                        tbl.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(85); // Inv #
                            cols.ConstantColumn(80); // Time
                            cols.RelativeColumn(3); // Customer
                            cols.ConstantColumn(50); // Items
                            cols.ConstantColumn(80); // Net
                            cols.RelativeColumn(2); // Mode
                        });

                        tbl.Header(h =>
                        {
                            h.Cell().Background("#0F172A").Padding(4).Text("INVOICE #").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Background("#0F172A").Padding(4).Text("TIME").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Background("#0F172A").Padding(4).Text("CUSTOMER").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Background("#0F172A").Padding(4).AlignCenter().Text("ITEMS").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Background("#0F172A").Padding(4).AlignRight().Text("AMOUNT (₹)").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Background("#0F172A").Padding(4).Text("PAYMENT").FontSize(8).Bold().FontColor(Colors.White);
                        });

                        foreach (var inv in report.InvoicesInPeriod.Take(30))
                        {
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text(inv.InvoiceNumber).Bold().FontColor("#0369A1");
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text(inv.InvoiceDate.ToLocalTime().ToString("hh:mm tt")).FontSize(8);
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text(inv.CustomerName).FontSize(8.5f);
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignCenter().Text(inv.ItemCount.ToString()).FontSize(8.5f);
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignRight().Text($"₹{inv.GrandTotal:N0}").Bold();
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text(inv.PaymentModes).FontSize(7.5f).FontColor("#64748B");
                        }
                    });
                });

                // Footer
                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(1).LineColor("#E2E8F0");
                    col.Item().PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("Sasan The Gadget Lounge ERP • Certified Executive Report • Page ").FontSize(8).FontColor("#94A3B8");
                            t.CurrentPageNumber().FontSize(8).FontColor("#94A3B8");
                            t.Span(" of ").FontSize(8).FontColor("#94A3B8");
                            t.TotalPages().FontSize(8).FontColor("#94A3B8");
                        });

                        row.RelativeItem().AlignRight().Text("Store Manager / Owner Signature: _______________________")
                            .FontSize(8.5f).FontColor("#475569");
                    });
                });
            });
        }).GeneratePdf(outputPath);
    }
}
