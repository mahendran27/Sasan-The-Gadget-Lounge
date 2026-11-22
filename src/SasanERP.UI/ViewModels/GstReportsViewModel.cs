using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SasanERP.Data.Context;
using SasanERP.Services.Reporting;

namespace SasanERP.UI.ViewModels;

public class GstInvoiceBreakdownItem
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string GSTIN { get; set; } = string.Empty;
    public decimal InvoiceTotal { get; set; }
    public decimal StandardTaxable { get; set; }
    public decimal CGST { get; set; }
    public decimal SGST { get; set; }
    public decimal MarginAmount { get; set; }
    public decimal MarginTax { get; set; }
    public bool IsMarginScheme { get; set; }
}

public partial class GstReportsViewModel : ObservableObject
{
    private readonly AppDbContext _context;
    private readonly GstReportService _gstReportService;

    [ObservableProperty]
    private int _selectedMonth = DateTime.UtcNow.Month;

    public ObservableCollection<int> Months { get; } = new(Enumerable.Range(1, 12));

    [ObservableProperty]
    private int _selectedYear = DateTime.UtcNow.Year;

    public ObservableCollection<int> Years { get; } = new(Enumerable.Range(DateTime.UtcNow.Year - 3, 5));

    [ObservableProperty]
    private decimal _standardTaxableTotal;

    [ObservableProperty]
    private decimal _cgstTotal;

    [ObservableProperty]
    private decimal _sgstTotal;

    [ObservableProperty]
    private decimal _marginProfitTotal;

    [ObservableProperty]
    private decimal _marginTaxTotal;

    [ObservableProperty]
    private decimal _totalGstLiability;

    public ObservableCollection<GstInvoiceBreakdownItem> Invoices { get; } = new();

    [ObservableProperty]
    private string _statusMessage = "GST Tax Audit & GSTR-1 Hub Ready.";

    [ObservableProperty]
    private bool _isLoading;

    public GstReportsViewModel(AppDbContext context, GstReportService gstReportService)
    {
        _context = context;
        _gstReportService = gstReportService;
    }

    [RelayCommand]
    public async Task LoadGstDataAsync()
    {
        IsLoading = true;
        try
        {
            var startDate = new DateTime(SelectedYear, SelectedMonth, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = startDate.AddMonths(1);

            var invs = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Items)
                .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate < endDate && i.IsGstEnabled)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            Invoices.Clear();
            decimal stdTaxable = 0;
            decimal cgst = 0;
            decimal sgst = 0;
            decimal marginProf = 0;
            decimal marginTax = 0;

            foreach (var i in invs)
            {
                stdTaxable += i.StandardTaxableValue;
                cgst += i.TotalCGST;
                sgst += i.TotalSGST;
                marginProf += i.TotalMarginAmount;
                marginTax += i.TotalMarginTax;

                Invoices.Add(new GstInvoiceBreakdownItem
                {
                    InvoiceNumber = i.InvoiceNumber,
                    InvoiceDate = i.InvoiceDate,
                    CustomerName = i.Customer.FullName,
                    GSTIN = i.Customer.GSTIN ?? "B2C Consumer",
                    InvoiceTotal = i.GrandTotal,
                    StandardTaxable = i.StandardTaxableValue,
                    CGST = i.TotalCGST,
                    SGST = i.TotalSGST,
                    MarginAmount = i.TotalMarginAmount,
                    MarginTax = i.TotalMarginTax,
                    IsMarginScheme = i.HasMarginSchemeItems
                });
            }

            StandardTaxableTotal = stdTaxable;
            CgstTotal = cgst;
            SgstTotal = sgst;
            MarginProfitTotal = marginProf;
            MarginTaxTotal = marginTax;
            TotalGstLiability = cgst + sgst + marginTax;

            StatusMessage = $"GST Data Loaded for {SelectedMonth:D2}/{SelectedYear}: {Invoices.Count} Invoices. Total Tax: ₹{TotalGstLiability:N2}.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ExportGstr1ExcelAsync()
    {
        IsLoading = true;
        try
        {
            byte[] excelBytes = await _gstReportService.GenerateGstr1ReportAsync(SelectedMonth, SelectedYear);

            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SasanERP_Reports");
            Directory.CreateDirectory(folder);

            string filePath = Path.Combine(folder, $"GSTR1_SasanTheGadgetLounge_{SelectedYear}_{SelectedMonth:D2}.xlsx");
            await File.WriteAllBytesAsync(filePath, excelBytes);

            StatusMessage = $"[SUCCESS] GSTR-1 Excel File exported: {filePath}";

            // Open with default Excel viewer if available
            try
            {
                new Process
                {
                    StartInfo = new ProcessStartInfo(filePath) { UseShellExecute = true }
                }.Start();
            }
            catch
            {
                // Ignore if Excel viewer not associated
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"[ERROR] Failed to export GSTR-1 Excel: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
