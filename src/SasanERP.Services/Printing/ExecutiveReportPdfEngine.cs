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
                        row.Spacing(6);

                        // Gross Sales
                        row.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(6).Column(c =>
                        {
                            c.Item().Text("GROSS SALES").FontSize(7.5f).Bold().FontColor("#64748B");
                            c.Item().Text($"₹{report.TotalGrossSales:N0}").FontSize(11).Bold().FontColor("#0F172A");
                        });

                        // Net Revenue
                        row.RelativeItem().Border(1).BorderColor("#0284C7").Background("#F0F9FF").Padding(6).Column(c =>
                        {
                            c.Item().Text("NET REVENUE").FontSize(7.5f).Bold().FontColor("#0369A1");
                            c.Item().Text($"₹{report.TotalNetRevenue:N0}").FontSize(11).Bold().FontColor("#0284C7");
                        });

                        // Gross Sales Profit
                        row.RelativeItem().Border(1).BorderColor("#16A34A").Background("#F0FDF4").Padding(6).Column(c =>
                        {
                            c.Item().Text("SALES PROFIT").FontSize(7.5f).Bold().FontColor("#15803D");
                            c.Item().Text($"₹{report.TotalProfitEstimated:N0}").FontSize(11).Bold().FontColor("#16A34A");
                        });

                        // Operating Expenses
                        row.RelativeItem().Border(1).BorderColor("#DC2626").Background("#FEF2F2").Padding(6).Column(c =>
                        {
                            c.Item().Text("STORE EXPENSES").FontSize(7.5f).Bold().FontColor("#DC2626");
                            c.Item().Text($"₹{report.TotalOperatingExpenses:N0}").FontSize(11).Bold().FontColor("#DC2626");
                        });

                        // True Net Profit
                        row.RelativeItem().Border(1).BorderColor("#059669").Background("#ECFDF5").Padding(6).Column(c =>
                        {
                            c.Item().Text("TRUE NET PROFIT").FontSize(7.5f).Bold().FontColor("#047857");
                            c.Item().Text($"₹{report.TrueNetBusinessProfit:N0}").FontSize(12).Bold().FontColor("#047857");
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

    /// <summary>
    /// Generates vector A4 Live Inventory Valuation & Shelf Aging Statement.
    /// Categorizes active gadgets, calculates purchase cost vs tagged price margins, and flags slow-moving stock.
    /// </summary>
    public void GenerateStockValuationReportPdf(
        StockValuationReportDto report,
        CompanyProfile company,
        string outputPath)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(9f).FontColor("#0F172A"));

                // Header
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem(7).Column(c =>
                        {
                            c.Item().Text(company.BusinessName.ToUpperInvariant())
                                .FontSize(15).Bold().FontColor("#0F172A");
                            c.Item().Text($"{company.AddressLine1}, {company.City}, {company.State} - {company.Pincode}")
                                .FontSize(8f).FontColor("#475569");
                            c.Item().Text($"GSTIN: {company.GSTIN} | Tel: {company.PhoneNumber} | Email: {company.Email}")
                                .FontSize(8f).FontColor("#475569");
                        });

                        row.RelativeItem(3).AlignRight().Column(c =>
                        {
                            c.Item().Border(1).BorderColor("#059669").Background("#ECFDF5").Padding(6).Column(ic =>
                            {
                                ic.Item().Text("LIVE STOCK VALUATION")
                                    .FontSize(9.5f).Bold().FontColor("#047857");
                                ic.Item().Text($"Snapshot: {DateTime.Now:dd-MMM-yyyy hh:mm tt}")
                                    .FontSize(7.5f).FontColor("#64748B");
                            });
                        });
                    });

                    col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor("#059669");
                });

                // Content
                page.Content().PaddingTop(10).Column(col =>
                {
                    // 1. KPI Cards Row
                    col.Item().Row(row =>
                    {
                        row.Spacing(6);

                        // Total In Stock
                        row.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(7).Column(c =>
                        {
                            c.Item().Text("UNITS IN STOCK").FontSize(7.5f).Bold().FontColor("#64748B");
                            c.Item().Text($"{report.TotalUnitsInStock} Devices").FontSize(11).Bold().FontColor("#0F172A");
                        });

                        // Valuation at Cost
                        row.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(7).Column(c =>
                        {
                            c.Item().Text("TOTAL COST (INVESTED)").FontSize(7.5f).Bold().FontColor("#64748B");
                            c.Item().Text($"₹{report.TotalCostValuation:N2}").FontSize(11).Bold().FontColor("#0284C7");
                        });

                        // Valuation at Retail
                        row.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(7).Column(c =>
                        {
                            c.Item().Text("RETAIL VALUATION").FontSize(7.5f).Bold().FontColor("#64748B");
                            c.Item().Text($"₹{report.TotalRetailValuation:N2}").FontSize(11).Bold().FontColor("#059669");
                        });

                        // Potential Margin
                        row.RelativeItem().Border(1).BorderColor("#059669").Background("#ECFDF5").Padding(7).Column(c =>
                        {
                            c.Item().Text("POTENTIAL GROSS PROFIT").FontSize(7.5f).Bold().FontColor("#047857");
                            c.Item().Text($"₹{report.TotalPotentialGrossProfit:N2}").FontSize(11).Bold().FontColor("#059669");
                            c.Item().Text($"{report.OverallMarginPercentage:F1}% Est. Margin").FontSize(7.5f).FontColor("#047857");
                        });

                        // Slow Moving Alert
                        row.RelativeItem().Border(1).BorderColor(report.SlowMovingCount > 0 ? "#DC2626" : "#E2E8F0")
                            .Background(report.SlowMovingCount > 0 ? "#FEF2F2" : "#F8FAFC").Padding(7).Column(c =>
                        {
                            c.Item().Text("SLOW MOVING (>30d)").FontSize(7.5f).Bold().FontColor(report.SlowMovingCount > 0 ? "#DC2626" : "#64748B");
                            c.Item().Text($"{report.SlowMovingCount} Devices").FontSize(11).Bold().FontColor(report.SlowMovingCount > 0 ? "#DC2626" : "#0F172A");
                        });
                    });

                    // 2. Category Summary Table
                    if (report.CategoryBreakdown.Any())
                    {
                        col.Item().PaddingTop(10).Text("CATEGORY INVENTORY SUMMARY").FontSize(9.5f).Bold().FontColor("#0F172A");
                        col.Item().PaddingTop(3).Table(tbl =>
                        {
                            tbl.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3); // Category
                                cols.ConstantColumn(65); // Count
                                cols.RelativeColumn(2); // Cost
                                cols.RelativeColumn(2); // Retail
                                cols.RelativeColumn(2); // Margin
                            });

                            tbl.Header(h =>
                            {
                                h.Cell().Background("#1E293B").Padding(4).Text("CATEGORY").FontSize(8).Bold().FontColor(Colors.White);
                                h.Cell().Background("#1E293B").Padding(4).AlignCenter().Text("UNITS").FontSize(8).Bold().FontColor(Colors.White);
                                h.Cell().Background("#1E293B").Padding(4).AlignRight().Text("COST (₹)").FontSize(8).Bold().FontColor(Colors.White);
                                h.Cell().Background("#1E293B").Padding(4).AlignRight().Text("RETAIL (₹)").FontSize(8).Bold().FontColor(Colors.White);
                                h.Cell().Background("#1E293B").Padding(4).AlignRight().Text("MARGIN (₹)").FontSize(8).Bold().FontColor(Colors.White);
                            });

                            foreach (var cat in report.CategoryBreakdown)
                            {
                                tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text(cat.CategoryName).Bold();
                                tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignCenter().Text(cat.DeviceCount.ToString()).FontSize(8.5f);
                                tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignRight().Text($"₹{cat.TotalCost:N0}").FontSize(8.5f);
                                tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignRight().Text($"₹{cat.TotalRetailValue:N0}").FontSize(8.5f);
                                tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignRight().Text($"₹{cat.PotentialMargin:N0}").Bold().FontColor("#059669");
                            }
                        });
                    }

                    // 3. Serialized Stock Items Table
                    col.Item().PaddingTop(10).Text("PHYSICAL DEVICES AUDIT LEDGER (IN-STOCK)").FontSize(9.5f).Bold().FontColor("#0F172A");
                    col.Item().PaddingTop(3).Table(tbl =>
                    {
                        tbl.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(85); // IMEI
                            cols.RelativeColumn(3); // Device / Specs
                            cols.ConstantColumn(80); // Condition & Battery
                            cols.ConstantColumn(65); // Cost
                            cols.ConstantColumn(65); // Price
                            cols.ConstantColumn(60); // Margin
                            cols.ConstantColumn(50); // Aging
                        });

                        tbl.Header(h =>
                        {
                            h.Cell().Background("#0F172A").Padding(4).Text("IMEI 1").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Background("#0F172A").Padding(4).Text("DEVICE / SPECS").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Background("#0F172A").Padding(4).Text("GRADE / BATT").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Background("#0F172A").Padding(4).AlignRight().Text("COST (₹)").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Background("#0F172A").Padding(4).AlignRight().Text("TAGGED (₹)").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Background("#0F172A").Padding(4).AlignRight().Text("MARGIN (₹)").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Background("#0F172A").Padding(4).AlignCenter().Text("AGING").FontSize(8).Bold().FontColor(Colors.White);
                        });

                        foreach (var unit in report.StockUnits.Take(45))
                        {
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(3.5f).Text(unit.IMEI1).FontSize(8).Bold().FontColor("#0369A1");
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(3.5f).Column(dc =>
                            {
                                dc.Item().Text(unit.ProductName).FontSize(8.5f).Bold();
                                dc.Item().Text($"{unit.Storage} • {unit.Color}").FontSize(7.5f).FontColor("#64748B");
                            });
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(3.5f).Text($"{unit.Grade.ToString().Replace("Grade", "")} • {unit.BatteryHealth}%").FontSize(8);
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(3.5f).AlignRight().Text($"₹{unit.Cost:N0}").FontSize(8);
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(3.5f).AlignRight().Text($"₹{unit.TaggedPrice:N0}").Bold().FontSize(8);
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(3.5f).AlignRight().Text($"₹{unit.PotentialMargin:N0}").Bold().FontColor("#059669").FontSize(8);
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(3.5f).AlignCenter()
                                .Text($"{unit.AgingDays}d").Bold().FontColor(unit.AgingDays > 30 ? "#DC2626" : "#475569").FontSize(8);
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
                            t.Span("Sasan The Gadget Lounge ERP • Stock Valuation Audit • Page ").FontSize(8).FontColor("#94A3B8");
                            t.CurrentPageNumber().FontSize(8).FontColor("#94A3B8");
                            t.Span(" of ").FontSize(8).FontColor("#94A3B8");
                            t.TotalPages().FontSize(8).FontColor("#94A3B8");
                        });

                        row.RelativeItem().AlignRight().Text("Inventory Custodian Signature: _______________________")
                            .FontSize(8.5f).FontColor("#475569");
                    });
                });
            });
        }).GeneratePdf(outputPath);
    }

    /// <summary>
    /// Generates vector A4 Customer Khata & Running Ledger Statement with dynamic UPI QR code payment.
    /// </summary>
    public void GenerateCustomerKhataStatementPdf(
        CustomerKhataStatementDto statement,
        CompanyProfile company,
        string outputPath)
    {
        byte[] qrBytes = _barcodeService.GenerateQrCode(statement.UpiPaymentString, 120);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(9f).FontColor("#0F172A"));

                // Header
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem(7).Column(c =>
                        {
                            c.Item().Text(company.BusinessName.ToUpperInvariant())
                                .FontSize(16).Bold().FontColor("#0F172A");
                            c.Item().Text($"{company.AddressLine1}, {company.City}, {company.State} - {company.Pincode}")
                                .FontSize(8.5f).FontColor("#475569");
                            c.Item().Text($"GSTIN: {company.GSTIN} | Tel: {company.PhoneNumber} | Email: {company.Email}")
                                .FontSize(8.5f).FontColor("#475569");
                        });

                        row.RelativeItem(3).AlignRight().Column(c =>
                        {
                            c.Item().Border(1).BorderColor("#D97706").Background("#FFFBEB").Padding(6).Column(ic =>
                            {
                                ic.Item().Text("KHATA STATEMENT")
                                    .FontSize(10).Bold().FontColor("#B45309");
                                ic.Item().Text($"Date: {DateTime.Now:dd-MMM-yyyy}")
                                    .FontSize(8f).FontColor("#64748B");
                            });
                        });
                    });

                    col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor("#D97706");
                });

                // Content
                page.Content().PaddingTop(12).Column(col =>
                {
                    // 1. Customer Details & Outstanding Balance & QR Code
                    col.Item().Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(10).Row(row =>
                    {
                        // Customer Profile
                        row.RelativeItem(5).Column(c =>
                        {
                            c.Item().Text("CUSTOMER DETAILS").FontSize(8f).Bold().FontColor("#64748B");
                            c.Item().Text(statement.CustomerName).FontSize(13).Bold().FontColor("#0F172A");
                            c.Item().Text($"Phone: {statement.PhoneNumber}").FontSize(8.5f).FontColor("#334155");
                            if (!string.IsNullOrWhiteSpace(statement.GSTIN))
                                c.Item().Text($"GSTIN: {statement.GSTIN}").FontSize(8.5f).Bold().FontColor("#0284C7");
                            if (!string.IsNullOrWhiteSpace(statement.AadhaarNumber))
                                c.Item().Text($"Aadhaar: {statement.AadhaarNumber}").FontSize(8f).FontColor("#475569");
                            if (!string.IsNullOrWhiteSpace(statement.Address))
                                c.Item().Text($"Address: {statement.Address}").FontSize(8f).FontColor("#475569");
                        });

                        // Outstanding Balance Card
                        row.RelativeItem(3).Padding(6).Border(1).BorderColor("#DC2626").Background("#FEF2F2").Column(c =>
                        {
                            c.Item().Text("CURRENT BALANCE DUE").FontSize(8f).Bold().FontColor("#DC2626");
                            c.Item().Text($"₹{statement.CurrentOutstanding:N2}").FontSize(15).Bold().FontColor("#DC2626");
                            c.Item().PaddingTop(4).Text($"Total Billed: ₹{statement.TotalBilled:N0}").FontSize(7.5f).FontColor("#475569");
                            c.Item().Text($"Total Paid: ₹{statement.TotalPaid:N0}").FontSize(7.5f).FontColor("#16A34A");
                        });

                        // QR Code
                        row.RelativeItem(2).AlignRight().Column(c =>
                        {
                            c.Item().Width(75).Height(75).Image(qrBytes);
                            c.Item().AlignCenter().Text("Scan to Pay UPI").FontSize(7f).Bold().FontColor("#475569");
                        });
                    });

                    // 2. Chronological Ledger Statement Table
                    col.Item().PaddingTop(12).Text("TRANSACTION HISTORY & RUNNING BALANCE")
                        .FontSize(10f).Bold().FontColor("#0F172A");

                    col.Item().PaddingTop(4).Table(tbl =>
                    {
                        tbl.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(75); // Date
                            cols.ConstantColumn(85); // Ref
                            cols.ConstantColumn(60); // Type
                            cols.RelativeColumn(3); // Description
                            cols.ConstantColumn(70); // Billed (Dr)
                            cols.ConstantColumn(70); // Paid (Cr)
                            cols.ConstantColumn(75); // Balance
                        });

                        tbl.Header(h =>
                        {
                            h.Cell().Background("#0F172A").Padding(4).Text("DATE").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Background("#0F172A").Padding(4).Text("REF #").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Background("#0F172A").Padding(4).Text("TYPE").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Background("#0F172A").Padding(4).Text("PARTICULARS").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Background("#0F172A").Padding(4).AlignRight().Text("BILLED (₹)").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Background("#0F172A").Padding(4).AlignRight().Text("PAID (₹)").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Background("#0F172A").Padding(4).AlignRight().Text("BALANCE (₹)").FontSize(8).Bold().FontColor(Colors.White);
                        });

                        foreach (var line in statement.StatementLines)
                        {
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text(line.Date.ToLocalTime().ToString("dd-MMM-yyyy")).FontSize(8);
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text(line.ReferenceNo).FontSize(8).Bold().FontColor("#0369A1");
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text(line.Type).FontSize(7.5f).Bold().FontColor(line.Type == "INVOICE" ? "#DC2626" : "#16A34A");
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text(line.Description).FontSize(8);
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignRight().Text(line.DebitAmount > 0 ? $"₹{line.DebitAmount:N2}" : "-").FontSize(8);
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignRight().Text(line.CreditAmount > 0 ? $"₹{line.CreditAmount:N2}" : "-").FontSize(8).FontColor("#16A34A");
                            tbl.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignRight().Text($"₹{line.RunningBalance:N2}").Bold().FontSize(8.5f);
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
                            t.Span("Sasan The Gadget Lounge ERP • Certified Khata Statement • Page ").FontSize(8).FontColor("#94A3B8");
                            t.CurrentPageNumber().FontSize(8).FontColor("#94A3B8");
                            t.Span(" of ").FontSize(8).FontColor("#94A3B8");
                            t.TotalPages().FontSize(8).FontColor("#94A3B8");
                        });

                        row.RelativeItem().AlignRight().Text("Customer Acknowledgment: _______________________")
                            .FontSize(8.5f).FontColor("#475569");
                    });
                });
            });
        }).GeneratePdf(outputPath);
    }
}
