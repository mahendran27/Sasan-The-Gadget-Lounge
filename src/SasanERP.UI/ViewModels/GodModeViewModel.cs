using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using SasanERP.Core.Enums;
using SasanERP.Core.Models;
using SasanERP.Data.Context;
using SasanERP.Services.Analytics;
using SasanERP.Services.Invoicing;
using SasanERP.Services.Printing;

namespace SasanERP.UI.ViewModels;

/// <summary>
/// Master Administrative ViewModel ("God Mode") for Sasan The Gadget Lounge.
/// Empowers owner/manager with full CRUD editing across inventory, customers, and invoices,
/// custom date/time shift analytics, and QuestPDF executive report generation.
/// </summary>
public partial class GodModeViewModel : ObservableObject
{
    private readonly AppDbContext _context;
    private readonly PeriodAnalyticsService _analyticsService;
    private readonly ExecutiveReportPdfEngine _reportPdfEngine;

    // =========================================================================
    // TAB 1: PERIOD & SHIFT BUSINESS INTELLIGENCE
    // =========================================================================
    [ObservableProperty] private DateTime _fromDate = DateTime.Today;
    [ObservableProperty] private string _fromTime = "09:00";
    [ObservableProperty] private DateTime _toDate = DateTime.Today;
    [ObservableProperty] private string _toTime = "22:00";
    [ObservableProperty] private PeriodReportDto? _currentReport;

    // =========================================================================
    // TAB 2: SERIALIZED STOCK & IMEI MASTER EDITOR
    // =========================================================================
    public ObservableCollection<SerializedUnit> StockUnits { get; } = new();
    [ObservableProperty] private SerializedUnit? _selectedUnit;
    [ObservableProperty] private string _stockSearchText = string.Empty;

    // Unit Editing Properties
    [ObservableProperty] private string _editImei1 = string.Empty;
    [ObservableProperty] private string _editImei2 = string.Empty;
    [ObservableProperty] private string _editSerialNumber = string.Empty;
    [ObservableProperty] private decimal _editCost;
    [ObservableProperty] private decimal _editPrice;
    [ObservableProperty] private int? _editBattery;
    [ObservableProperty] private ConditionGrade _editGrade = ConditionGrade.GradeAPlus_Flawless;
    [ObservableProperty] private UnitStatus _editStatus = UnitStatus.InStock;
    [ObservableProperty] private WarrantyStatus _editWarranty = WarrantyStatus.ShopWarranty_90Days;

    public ObservableCollection<ConditionGrade> AvailableGrades { get; } = new(Enum.GetValues<ConditionGrade>());
    public ObservableCollection<UnitStatus> AvailableStatuses { get; } = new(Enum.GetValues<UnitStatus>());
    public ObservableCollection<WarrantyStatus> AvailableWarranties { get; } = new(Enum.GetValues<WarrantyStatus>());

    partial void OnSelectedUnitChanged(SerializedUnit? value)
    {
        if (value != null)
        {
            EditImei1 = value.IMEI1;
            EditImei2 = value.IMEI2 ?? string.Empty;
            EditSerialNumber = value.SerialNumber;
            EditCost = value.PurchaseCost;
            EditPrice = value.TaggedPrice;
            EditBattery = value.BatteryHealthPercentage;
            EditGrade = value.ConditionGrade;
            EditStatus = value.Status;
            EditWarranty = value.WarrantyStatus;
        }
    }

    partial void OnStockSearchTextChanged(string value)
    {
        _ = LoadStockUnitsAsync();
    }

    // =========================================================================
    // TAB 3: CUSTOMER MASTER & KHATA CREDIT MANAGER
    // =========================================================================
    public ObservableCollection<Customer> Customers { get; } = new();
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private string _customerSearchText = string.Empty;

    // Customer Editing Properties
    [ObservableProperty] private string _editCustomerName = string.Empty;
    [ObservableProperty] private string _editCustomerPhone = string.Empty;
    [ObservableProperty] private string _editCustomerGstin = string.Empty;
    [ObservableProperty] private string _editCustomerAadhaar = string.Empty;
    [ObservableProperty] private string _editCustomerAddress = string.Empty;

    // Khata Settlement Properties
    [ObservableProperty] private decimal _settleAmount;
    [ObservableProperty] private PaymentMode _settlePaymentMode = PaymentMode.Cash;
    [ObservableProperty] private string _settleReference = string.Empty;

    public ObservableCollection<PaymentMode> AvailablePaymentModes { get; } = new(Enum.GetValues<PaymentMode>());

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        if (value != null)
        {
            EditCustomerName = value.FullName;
            EditCustomerPhone = value.PhoneNumber;
            EditCustomerGstin = value.GSTIN ?? string.Empty;
            EditCustomerAadhaar = value.AadhaarNumber ?? string.Empty;
            EditCustomerAddress = value.Address ?? string.Empty;
            SettleAmount = value.OutstandingCredit;
        }
    }

    partial void OnCustomerSearchTextChanged(string value)
    {
        _ = LoadCustomersAsync();
    }

    // =========================================================================
    // TAB 4: ADMINISTRATIVE INVOICE VOIDING & AUDIT
    // =========================================================================
    public ObservableCollection<InvoiceSummaryDto> VoidableInvoices { get; } = new();
    [ObservableProperty] private InvoiceSummaryDto? _selectedVoidInvoice;
    [ObservableProperty] private string _voidSearchText = string.Empty;
    [ObservableProperty] private string _voidReason = "Customer Returned / Billing Mistake";

    partial void OnVoidSearchTextChanged(string value)
    {
        _ = LoadVoidableInvoicesAsync();
    }

    // Status Banner
    [ObservableProperty] private string _statusMessage = "👑 GOD MODE: Full Administrative Control & Business Intelligence Active.";
    [ObservableProperty] private bool _isBusy;

    public GodModeViewModel(
        AppDbContext context,
        PeriodAnalyticsService analyticsService,
        ExecutiveReportPdfEngine reportPdfEngine)
    {
        _context = context;
        _analyticsService = analyticsService;
        _reportPdfEngine = reportPdfEngine;
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            GenerateReportAsync(),
            LoadStockUnitsAsync(),
            LoadCustomersAsync(),
            LoadVoidableInvoicesAsync()
        );
    }

    // =========================================================================
    // TAB 1 ACTIONS
    // =========================================================================
    [RelayCommand]
    public async Task GenerateReportAsync()
    {
        IsBusy = true;
        try
        {
            var from = ParseDateTime(FromDate, FromTime, isStartOfDay: true);
            var to = ParseDateTime(ToDate, ToTime, isStartOfDay: false);

            CurrentReport = await _analyticsService.ComputePeriodReportAsync(from.ToUniversalTime(), to.ToUniversalTime());
            StatusMessage = $"Report computed: {CurrentReport.TotalInvoicesCount} invoices, Total Net Revenue: ₹{CurrentReport.TotalNetRevenue:N2}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Analytics calculation error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ExportPdfReportAsync()
    {
        if (CurrentReport == null)
        {
            await GenerateReportAsync();
        }

        if (CurrentReport == null) return;

        var saveDialog = new SaveFileDialog
        {
            Title = "Export Period Sales Report as PDF",
            Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*",
            FileName = $"Sasan_SalesReport_{FromDate:yyyyMMdd}_{ToDate:yyyyMMdd}.pdf",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (saveDialog.ShowDialog() == true)
        {
            IsBusy = true;
            try
            {
                var company = await _context.CompanyProfiles.FirstOrDefaultAsync() ?? new CompanyProfile();
                string targetPath = saveDialog.FileName;

                await Task.Run(() => _reportPdfEngine.GeneratePeriodSalesReportPdf(CurrentReport, company, targetPath));
                Process.Start(new ProcessStartInfo(targetPath) { UseShellExecute = true });
                StatusMessage = $"[REPORT EXPORTED] Saved & opened: {targetPath}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Report export error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    [RelayCommand]
    public async Task QuickPresetAsync(string preset)
    {
        DateTime now = DateTime.Now;
        switch (preset)
        {
            case "Today":
                FromDate = now.Date;
                FromTime = "09:00";
                ToDate = now.Date;
                ToTime = "22:00";
                break;
            case "MorningShift":
                FromDate = now.Date;
                FromTime = "09:00";
                ToDate = now.Date;
                ToTime = "14:00";
                break;
            case "EveningShift":
                FromDate = now.Date;
                FromTime = "14:00";
                ToDate = now.Date;
                ToTime = "22:00";
                break;
            case "ThisWeek":
                FromDate = now.Date.AddDays(-(int)now.DayOfWeek);
                FromTime = "09:00";
                ToDate = now.Date;
                ToTime = "22:00";
                break;
            case "ThisMonth":
                FromDate = new DateTime(now.Year, now.Month, 1);
                FromTime = "09:00";
                ToDate = now.Date;
                ToTime = "22:00";
                break;
        }

        await GenerateReportAsync();
    }

    private static DateTime ParseDateTime(DateTime date, string timeStr, bool isStartOfDay)
    {
        if (TimeSpan.TryParse(timeStr, out var time))
        {
            return date.Date.Add(time);
        }
        return isStartOfDay ? date.Date : date.Date.AddDays(1).AddTicks(-1);
    }

    // =========================================================================
    // TAB 2 ACTIONS: STOCK FULL CRUD
    // =========================================================================
    [RelayCommand]
    public async Task LoadStockUnitsAsync()
    {
        var query = _context.SerializedUnits
            .Include(u => u.Product)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(StockSearchText))
        {
            string t = StockSearchText.Trim().ToLower();
            query = query.Where(u =>
                u.IMEI1.Contains(t) ||
                (u.IMEI2 != null && u.IMEI2.Contains(t)) ||
                u.SerialNumber.ToLower().Contains(t) ||
                u.Product.Name.ToLower().Contains(t));
        }

        var units = await query.OrderByDescending(u => u.PurchaseDate).Take(150).ToListAsync();
        StockUnits.Clear();
        foreach (var u in units) StockUnits.Add(u);

        if (StockUnits.Any() && SelectedUnit == null)
            SelectedUnit = StockUnits.First();
    }

    [RelayCommand]
    public async Task SaveSelectedUnitChangesAsync()
    {
        if (SelectedUnit == null) return;
        IsBusy = true;
        try
        {
            await _analyticsService.UpdateSerializedUnitAsync(
                SelectedUnit.Id,
                EditImei1,
                string.IsNullOrWhiteSpace(EditImei2) ? null : EditImei2,
                EditSerialNumber,
                EditCost,
                EditPrice,
                EditBattery,
                EditGrade,
                EditStatus,
                EditWarranty,
                "AdminOwner");

            await LoadStockUnitsAsync();
            StatusMessage = $"[SUCCESS] Device IMEI {EditImei1} updated successfully (Cost: ₹{EditCost:N0}, Price: ₹{EditPrice:N0}, Status: {EditStatus})";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to update unit: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // =========================================================================
    // TAB 3 ACTIONS: CUSTOMER CRUD & KHATA
    // =========================================================================
    [RelayCommand]
    public async Task LoadCustomersAsync()
    {
        var query = _context.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(CustomerSearchText))
        {
            string t = CustomerSearchText.Trim().ToLower();
            query = query.Where(c => c.FullName.ToLower().Contains(t) || c.PhoneNumber.Contains(t));
        }

        var list = await query.OrderByDescending(c => c.OutstandingCredit).ThenBy(c => c.FullName).Take(100).ToListAsync();
        Customers.Clear();
        foreach (var c in list) Customers.Add(c);

        if (Customers.Any() && SelectedCustomer == null)
            SelectedCustomer = Customers.First();
    }

    [RelayCommand]
    public async Task SaveCustomerChangesAsync()
    {
        if (SelectedCustomer == null) return;
        IsBusy = true;
        try
        {
            await _analyticsService.UpdateCustomerAsync(
                SelectedCustomer.Id,
                EditCustomerName,
                EditCustomerPhone,
                EditCustomerGstin,
                EditCustomerAadhaar,
                EditCustomerAddress,
                "AdminOwner");

            await LoadCustomersAsync();
            StatusMessage = $"[SUCCESS] Customer {EditCustomerName} ({EditCustomerPhone}) updated successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Customer update error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SettleCustomerKhataAsync()
    {
        if (SelectedCustomer == null || SettleAmount <= 0) return;
        IsBusy = true;
        try
        {
            await _analyticsService.SettleCustomerCreditAsync(
                SelectedCustomer.Id,
                SettleAmount,
                SettlePaymentMode,
                SettleReference,
                "AdminOwner");

            await LoadCustomersAsync();
            StatusMessage = $"[KHATA SETTLED] Received ₹{SettleAmount:N2} via {SettlePaymentMode} from {SelectedCustomer.FullName}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Khata settlement error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // =========================================================================
    // TAB 4 ACTIONS: INVOICE VOIDING
    // =========================================================================
    [RelayCommand]
    public async Task LoadVoidableInvoicesAsync()
    {
        var query = _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Where(i => !i.IsVoided)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(VoidSearchText))
        {
            string t = VoidSearchText.Trim().ToLower();
            query = query.Where(i => i.InvoiceNumber.ToLower().Contains(t) ||
                                     (i.Customer != null && i.Customer.PhoneNumber.Contains(t)));
        }

        var list = await query.OrderByDescending(i => i.InvoiceDate).Take(50).ToListAsync();
        VoidableInvoices.Clear();
        foreach (var inv in list)
        {
            VoidableInvoices.Add(new InvoiceSummaryDto(
                inv.Id,
                inv.InvoiceNumber,
                inv.InvoiceDate,
                inv.Customer?.FullName ?? "Walk-in",
                inv.Customer?.PhoneNumber ?? "N/A",
                inv.InvoiceType,
                inv.HasMarginSchemeItems,
                inv.Items.Count,
                inv.GrandTotal,
                inv.AmountPaid,
                inv.BalanceDue,
                string.Join(", ", inv.Payments.Select(p => p.Mode.ToString()))
            ));
        }

        if (VoidableInvoices.Any() && SelectedVoidInvoice == null)
            SelectedVoidInvoice = VoidableInvoices.First();
    }

    [RelayCommand]
    public async Task VoidSelectedInvoiceAsync()
    {
        if (SelectedVoidInvoice == null) return;
        IsBusy = true;
        try
        {
            bool success = await _analyticsService.VoidInvoiceAsync(
                SelectedVoidInvoice.Id,
                VoidReason,
                "AdminOwner");

            if (success)
            {
                StatusMessage = $"[INVOICE VOIDED] #{SelectedVoidInvoice.InvoiceNumber} cancelled. Devices returned to InStock. Ledgers reversed.";
                await LoadVoidableInvoicesAsync();
                await LoadStockUnitsAsync();
                await GenerateReportAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Void error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
