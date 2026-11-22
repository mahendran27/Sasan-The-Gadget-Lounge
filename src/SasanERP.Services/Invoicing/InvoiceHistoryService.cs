using System.IO;
using Microsoft.EntityFrameworkCore;
using SasanERP.Core.Enums;
using SasanERP.Core.Models;
using SasanERP.Data.Context;
using SasanERP.Services.Printing;

namespace SasanERP.Services.Invoicing;

public record InvoiceSummaryDto(
    int Id,
    string InvoiceNumber,
    DateTime InvoiceDate,
    string CustomerName,
    string CustomerPhone,
    InvoiceType InvoiceType,
    bool HasMarginScheme,
    int ItemCount,
    decimal GrandTotal,
    decimal AmountPaid,
    decimal BalanceDue,
    string PaymentModes);

/// <summary>
/// Service for searching past sales invoices, retrieving itemized details with IMEI history,
/// reprinting in 12 vector template styles, and exporting to PDF.
/// </summary>
public class InvoiceHistoryService
{
    private readonly AppDbContext _context;
    private readonly InvoicePrintEngine _printEngine;

    public InvoiceHistoryService(AppDbContext context, InvoicePrintEngine printEngine)
    {
        _context = context;
        _printEngine = printEngine;
    }

    /// <summary>
    /// Searches past invoices across Invoice Number, Customer Name, Phone Number, and Serialized Device IMEI.
    /// </summary>
    public async Task<List<InvoiceSummaryDto>> SearchInvoicesAsync(
        string? searchTerm = null, 
        DateTime? fromDate = null, 
        DateTime? toDate = null)
    {
        var query = _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .AsNoTracking();

        if (fromDate.HasValue)
            query = query.Where(i => i.InvoiceDate >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(i => i.InvoiceDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            query = query.Where(i =>
                i.InvoiceNumber.ToLower().Contains(term) ||
                (i.Customer != null && (i.Customer.FullName.ToLower().Contains(term) || i.Customer.PhoneNumber.Contains(term))) ||
                i.Items.Any(item => 
                    (item.IMEI1 != null && item.IMEI1.Contains(term)) ||
                    (item.IMEI2 != null && item.IMEI2.Contains(term)) ||
                    (item.SerialNumber != null && item.SerialNumber.ToLower().Contains(term)) ||
                    item.ItemDescription.ToLower().Contains(term)));
        }

        var invoices = await query
            .OrderByDescending(i => i.InvoiceDate)
            .Take(300)
            .ToListAsync();

        return invoices.Select(i => new InvoiceSummaryDto(
            i.Id,
            i.InvoiceNumber,
            i.InvoiceDate,
            i.Customer?.FullName ?? "Walk-in Customer",
            i.Customer?.PhoneNumber ?? "N/A",
            i.InvoiceType,
            i.HasMarginSchemeItems,
            i.Items.Count,
            i.GrandTotal,
            i.AmountPaid,
            i.BalanceDue,
            string.Join(", ", i.Payments.Select(p => p.Mode.ToString()).Distinct())
        )).ToList();
    }

    /// <summary>
    /// Fetches complete itemized invoice model including serialized IMEI specs and payments
    /// </summary>
    public async Task<Invoice?> GetInvoiceDetailsAsync(int invoiceId)
    {
        return await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
    }

    /// <summary>
    /// Generates vector PDF in chosen template style and writes to specified path
    /// </summary>
    public async Task<string> ExportPdfAsync(
        Invoice invoice, 
        InvoiceTemplateStyle style, 
        string destinationFilePath)
    {
        var company = await _context.CompanyProfiles.FirstOrDefaultAsync() ?? new CompanyProfile();
        string? dir = Path.GetDirectoryName(destinationFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        await Task.Run(() => _printEngine.SavePdf(style, invoice, company, destinationFilePath));
        return destinationFilePath;
    }
}
