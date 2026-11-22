using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SasanERP.Core.Models;
using SasanERP.Data.Context;

namespace SasanERP.Services.Reporting;

/// <summary>
/// GSTR-1 and GSTR-3B Tax Audit Excel Report Generator using ClosedXML
/// Produces CA-auditable workbooks including Rule 32(5) Second-Hand Margin Scheme registers.
/// </summary>
public class GstReportService
{
    private readonly AppDbContext _context;

    public GstReportService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Generates multi-sheet GSTR-1 Excel workbook for a given month and year
    /// </summary>
    public async Task<byte[]> GenerateGstr1ReportAsync(int month, int year)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddTicks(-1);

        var invoices = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
                .ThenInclude(item => item.Product)
            .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
            .OrderBy(i => i.InvoiceDate)
            .ToListAsync();

        using var workbook = new XLWorkbook();

        // 1. Sheet: B2C Retail Sales (Standard GST)
        var wsB2C = workbook.Worksheets.Add("B2C_Standard_GST");
        wsB2C.Cell(1, 1).Value = "Invoice Number";
        wsB2C.Cell(1, 2).Value = "Invoice Date";
        wsB2C.Cell(1, 3).Value = "Customer Name";
        wsB2C.Cell(1, 4).Value = "Customer Mobile";
        wsB2C.Cell(1, 5).Value = "Place of Supply";
        wsB2C.Cell(1, 6).Value = "Taxable Value (INR)";
        wsB2C.Cell(1, 7).Value = "CGST (INR)";
        wsB2C.Cell(1, 8).Value = "SGST (INR)";
        wsB2C.Cell(1, 9).Value = "Total Tax (INR)";
        wsB2C.Cell(1, 10).Value = "Invoice Value (INR)";
        var headerRange1 = wsB2C.Range(1, 1, 1, 10);
        headerRange1.Style.Font.Bold = true;
        headerRange1.Style.Fill.BackgroundColor = XLColor.FromHtml("#0B192C");
        headerRange1.Style.Font.FontColor = XLColor.White;

        int row1 = 2;
        foreach (var inv in invoices.Where(i => !i.HasMarginSchemeItems))
        {
            wsB2C.Cell(row1, 1).Value = inv.InvoiceNumber;
            wsB2C.Cell(row1, 2).Value = inv.InvoiceDate.ToString("yyyy-MM-dd");
            wsB2C.Cell(row1, 3).Value = inv.Customer.FullName;
            wsB2C.Cell(row1, 4).Value = inv.Customer.PhoneNumber;
            wsB2C.Cell(row1, 5).Value = "33-Tamil Nadu";
            wsB2C.Cell(row1, 6).Value = inv.StandardTaxableValue;
            wsB2C.Cell(row1, 7).Value = inv.TotalCGST;
            wsB2C.Cell(row1, 8).Value = inv.TotalSGST;
            wsB2C.Cell(row1, 9).Value = inv.TotalCGST + inv.TotalSGST;
            wsB2C.Cell(row1, 10).Value = inv.GrandTotal;
            row1++;
        }
        wsB2C.Columns(1, 10).Width = 18;

        // 2. Sheet: Rule 32(5) Second-Hand Margin Scheme Register (Crucial for CA Audit)
        var wsMargin = workbook.Worksheets.Add("Rule32_5_Margin_Scheme");
        wsMargin.Cell(1, 1).Value = "Invoice Number";
        wsMargin.Cell(1, 2).Value = "Invoice Date";
        wsMargin.Cell(1, 3).Value = "Device Description";
        wsMargin.Cell(1, 4).Value = "IMEI / Serial";
        wsMargin.Cell(1, 5).Value = "Purchase Cost (A)";
        wsMargin.Cell(1, 6).Value = "Sale Price (B)";
        wsMargin.Cell(1, 7).Value = "Margin Value (B - A)";
        wsMargin.Cell(1, 8).Value = "Taxable Margin Value";
        wsMargin.Cell(1, 9).Value = "CGST on Margin (9%)";
        wsMargin.Cell(1, 10).Value = "SGST on Margin (9%)";
        wsMargin.Cell(1, 11).Value = "Total Margin GST";
        var headerRange2 = wsMargin.Range(1, 1, 1, 11);
        headerRange2.Style.Font.Bold = true;
        headerRange2.Style.Fill.BackgroundColor = XLColor.FromHtml("#E65100");
        headerRange2.Style.Font.FontColor = XLColor.White;

        int row2 = 2;
        foreach (var inv in invoices)
        {
            foreach (var item in inv.Items.Where(it => it.IsMarginScheme))
            {
                wsMargin.Cell(row2, 1).Value = inv.InvoiceNumber;
                wsMargin.Cell(row2, 2).Value = inv.InvoiceDate.ToString("yyyy-MM-dd");
                wsMargin.Cell(row2, 3).Value = item.ItemDescription;
                wsMargin.Cell(row2, 4).Value = item.IMEI1 ?? item.SerialNumber ?? "-";
                wsMargin.Cell(row2, 5).Value = item.UnitCostPrice;
                wsMargin.Cell(row2, 6).Value = item.UnitPrice - item.ItemDiscount;
                wsMargin.Cell(row2, 7).Value = item.MarginAmount;
                wsMargin.Cell(row2, 8).Value = item.TaxableValue;
                wsMargin.Cell(row2, 9).Value = item.CGSTAmount;
                wsMargin.Cell(row2, 10).Value = item.SGSTAmount;
                wsMargin.Cell(row2, 11).Value = item.CGSTAmount + item.SGSTAmount;
                row2++;
            }
        }
        wsMargin.Columns(1, 11).Width = 18;

        // 3. Sheet: HSN Summary
        var wsHsn = workbook.Worksheets.Add("HSN_Summary");
        wsHsn.Cell(1, 1).Value = "HSN Code";
        wsHsn.Cell(1, 2).Value = "Description";
        wsHsn.Cell(1, 3).Value = "UQC";
        wsHsn.Cell(1, 4).Value = "Total Quantity";
        wsHsn.Cell(1, 5).Value = "Total Value";
        wsHsn.Cell(1, 6).Value = "Taxable Value";
        wsHsn.Cell(1, 7).Value = "CGST Amount";
        wsHsn.Cell(1, 8).Value = "SGST Amount";
        var headerRange3 = wsHsn.Range(1, 1, 1, 8);
        headerRange3.Style.Font.Bold = true;
        headerRange3.Style.Fill.BackgroundColor = XLColor.FromHtml("#00838F");
        headerRange3.Style.Font.FontColor = XLColor.White;

        var hsnGroups = invoices.SelectMany(i => i.Items)
            .GroupBy(i => i.HSNCode)
            .OrderBy(g => g.Key);

        int row3 = 2;
        foreach (var group in hsnGroups)
        {
            wsHsn.Cell(row3, 1).Value = group.Key;
            wsHsn.Cell(row3, 2).Value = GetHsnDescription(group.Key);
            wsHsn.Cell(row3, 3).Value = "NOS";
            wsHsn.Cell(row3, 4).Value = group.Sum(x => x.Quantity);
            wsHsn.Cell(row3, 5).Value = group.Sum(x => x.LineTotal);
            wsHsn.Cell(row3, 6).Value = group.Sum(x => x.TaxableValue);
            wsHsn.Cell(row3, 7).Value = group.Sum(x => x.CGSTAmount);
            wsHsn.Cell(row3, 8).Value = group.Sum(x => x.SGSTAmount);
            row3++;
        }
        wsHsn.Columns(1, 8).Width = 18;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static string GetHsnDescription(string hsn) => hsn switch
    {
        "8517" => "Smartphones & Cellular Network Devices",
        "8471" => "Automatic Data Processing Machines / Laptops",
        "8806" => "Unmanned Aircraft / Drones",
        "8504" => "Static Converters, Adapters & Power Banks",
        "9503" => "Electronic Toys & Models",
        "8518" => "Headphones, Earphones & Microphones",
        _ => "General Gadgets & Accessories"
    };
}
