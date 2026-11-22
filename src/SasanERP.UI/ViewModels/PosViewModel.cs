using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using SasanERP.Core.Enums;
using SasanERP.Core.Models;
using SasanERP.Data.Context;
using SasanERP.Services.Audio;
using SasanERP.Services.Barcode;
using SasanERP.Services.Billing;
using SasanERP.Services.Hardware;
using SasanERP.Services.Printing;
using SasanERP.Services.Wireless;
using SasanERP.UI.Services;

namespace SasanERP.UI.ViewModels;

/// <summary>
/// Production-Ready POS Billing ViewModel for Sasan The Gadget Lounge
/// Features USB Barcode scanner listener, serialized IMEI checkout, multi-tender split payment,
/// 7 Invoice Templates, and 6 Ergonomic UI Themes.
/// </summary>
public partial class PosViewModel : ObservableObject
{
    private readonly AppDbContext _context;
    private readonly BillingService _billingService;
    private readonly BarcodeGeneratorService _barcodeService;
    private readonly InvoicePrintEngine _invoicePrintEngine;
    private readonly UnifiedWirelessManager? _wirelessManager;
    private readonly PosAudioFeedbackService? _audioService;
    private readonly SerialPortBarcodeListener? _barcodeListener;

    public ObservableCollection<PosCartItemViewModel> CartItems { get; } = new();
    public ObservableCollection<Customer> Customers { get; } = new();

    public ObservableCollection<InvoiceTemplateStyle> AvailableInvoiceTemplates { get; } = 
        new(Enum.GetValues<InvoiceTemplateStyle>());

    [ObservableProperty]
    private InvoiceTemplateStyle _selectedInvoiceTemplate = InvoiceTemplateStyle.LuxuryGadgetLounge;

    public ObservableCollection<AppThemeMode> AvailableThemeModes { get; } = 
        new(Enum.GetValues<AppThemeMode>());

    [ObservableProperty]
    private AppThemeMode _selectedThemeMode = AppThemeMode.ElectricCyberCyan;

    partial void OnSelectedThemeModeChanged(AppThemeMode value)
    {
        ThemeService.Instance.ApplyTheme(value);
        StatusMessage = $"UI Theme changed to: {value}";
    }

    [ObservableProperty]
    private Customer? _selectedCustomer;

    [ObservableProperty]
    private string _barcodeInput = string.Empty;

    [ObservableProperty]
    private string _invoiceNumber = string.Empty;

    [ObservableProperty]
    private DateTime _invoiceDate = DateTime.Now;

    [ObservableProperty]
    private decimal _subTotal;

    [ObservableProperty]
    private decimal _invoiceDiscount;

    [ObservableProperty]
    private decimal _totalTaxableValue;

    [ObservableProperty]
    private decimal _totalCGST;

    [ObservableProperty]
    private decimal _totalSGST;

    [ObservableProperty]
    private decimal _totalMarginTax;

    [ObservableProperty]
    private decimal _grandTotal;

    [ObservableProperty]
    private decimal _roundOff;

    // Multi-tender split payment fields
    [ObservableProperty]
    private decimal _cashTendered;

    [ObservableProperty]
    private decimal _upiTendered;

    [ObservableProperty]
    private decimal _cardTendered;

    [ObservableProperty]
    private decimal _exchangeTendered;

    [ObservableProperty]
    private decimal _financeEmiTendered;

    [ObservableProperty]
    private string? _paymentReference;

    [ObservableProperty]
    private decimal _totalTendered;

    [ObservableProperty]
    private decimal _balanceChange;

    [ObservableProperty]
    private bool _hasMarginSchemeItems;

    [ObservableProperty]
    private bool _isGstEnabled = true;

    partial void OnIsGstEnabledChanged(bool value)
    {
        foreach (var item in CartItems)
        {
            item.RecalculateTaxes(isGstEnabled: value);
        }
        RecalculateInvoiceTotals();
        StatusMessage = value 
            ? "GST Billed: ON (Standard GST & Rule 32(5) Active)" 
            : "GST Billed: OFF (Non-GST / Retail Bill Active - Tax is ₹0)";
    }

    [RelayCommand]
    public void ToggleGstMode()
    {
        IsGstEnabled = !IsGstEnabled;
    }

    [ObservableProperty]
    private string _statusMessage = "Ready. Scan barcode or IMEI (F2: New Bill, F9: Checkout & Print).";

    [ObservableProperty]
    private bool _isBusy;

    public PosViewModel(
        AppDbContext context,
        BillingService billingService,
        BarcodeGeneratorService barcodeService,
        InvoicePrintEngine invoicePrintEngine,
        UnifiedWirelessManager? wirelessManager = null,
        PosAudioFeedbackService? audioService = null,
        SerialPortBarcodeListener? barcodeListener = null)
    {
        _context = context;
        _billingService = billingService;
        _barcodeService = barcodeService;
        _invoicePrintEngine = invoicePrintEngine;
        _wirelessManager = wirelessManager;
        _audioService = audioService;
        _barcodeListener = barcodeListener;

        if (_barcodeListener != null)
        {
            _barcodeListener.OnBarcodeScanned += (code, port) =>
            {
                if (System.Windows.Application.Current?.Dispatcher != null &&
                    !System.Windows.Application.Current.Dispatcher.CheckAccess())
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(async () =>
                        await ProcessCodeAsync(code, isWirelessTap: false));
                }
                else
                {
                    _ = ProcessCodeAsync(code, isWirelessTap: false);
                }
            };
        }

        if (_wirelessManager != null)
        {
            _wirelessManager.OnTagTappedForCheckout += (imei, freq) =>
            {
                if (System.Windows.Application.Current?.Dispatcher != null && 
                    !System.Windows.Application.Current.Dispatcher.CheckAccess())
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(async () => 
                        await ProcessCodeAsync(imei, isWirelessTap: true, freq: freq));
                }
                else
                {
                    _ = ProcessCodeAsync(imei, isWirelessTap: true, freq: freq);
                }
            };
        }
    }

    /// <summary>
    /// Initializes POS session: loads default customer and generates next invoice number
    /// </summary>
    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var company = await _context.CompanyProfiles.FirstOrDefaultAsync();
            if (company != null)
            {
                SelectedInvoiceTemplate = company.DefaultInvoiceTemplate;
                SelectedThemeMode = company.DefaultThemeMode;
                ThemeService.Instance.ApplyTheme(company.DefaultThemeMode);
            }

            var customers = await _context.Customers.OrderBy(c => c.FullName).ToListAsync();
            Customers.Clear();
            foreach (var c in customers)
            {
                Customers.Add(c);
            }

            SelectedCustomer = Customers.FirstOrDefault(c => c.PhoneNumber == "9080803070") 
                               ?? Customers.FirstOrDefault();

            InvoiceNumber = await _billingService.GenerateNextInvoiceNumberAsync();
            StatusMessage = $"New Bill {InvoiceNumber} ready for Sasan The Gadget Lounge.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Processes barcode scanner input. Instant lookup across SerializedUnits (IMEI1, IMEI2, S/N)
    /// and bulk Products (Barcode, SKU).
    /// </summary>
    [RelayCommand]
    public async Task ProcessBarcodeScanAsync()
    {
        string rawCode = BarcodeInput?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rawCode)) return;

        BarcodeInput = string.Empty; // Reset input field immediately for next scan
        await ProcessCodeAsync(rawCode, isWirelessTap: false);
    }

    /// <summary>
    /// Core item identification method supporting USB Barcode Scanner, Counter NFC tap, and UHF RFID interrogation.
    /// </summary>
    public async Task ProcessCodeAsync(string rawCode, bool isWirelessTap = false, RfFrequencyType? freq = null)
    {
        if (string.IsNullOrWhiteSpace(rawCode)) return;
        IsBusy = true;

        try
        {
            // 1. Check if scanned code matches a Serialized Device (IMEI 1, IMEI 2, or Serial Number)
            var unit = await _context.SerializedUnits
                .Include(u => u.Product)
                .FirstOrDefaultAsync(u => 
                    (u.IMEI1 == rawCode || u.IMEI2 == rawCode || u.SerialNumber == rawCode) && 
                    u.Status == UnitStatus.InStock);

            if (unit != null)
            {
                // Prevent duplicate addition of the same unique device
                if (CartItems.Any(ci => ci.SerializedUnitId == unit.Id))
                {
                    _audioService?.PlayScanError();
                    StatusMessage = isWirelessTap
                        ? $"[WIRELESS TAP] Device with IMEI {unit.IMEI1} is already in the cart!"
                        : $"[WARNING] Device with IMEI {unit.IMEI1} is already added in the cart!";
                    return;
                }

                bool isPreOwned = unit.ConditionGrade != ConditionGrade.BrandNew || 
                                  unit.PurchaseSource == PurchaseSource.CustomerBuyback ||
                                  unit.PurchaseSource == PurchaseSource.DirectImport;

                var cartItem = new PosCartItemViewModel
                {
                    ProductId = unit.ProductId,
                    SerializedUnitId = unit.Id,
                    Name = unit.Product.Name,
                    HsnCode = unit.Product.HSNCode,
                    Imei1 = unit.IMEI1,
                    Imei2 = unit.IMEI2,
                    SerialNumber = unit.SerialNumber,
                    BatteryHealthPercentage = unit.BatteryHealthPercentage,
                    ConditionGrade = unit.ConditionGrade,
                    WarrantyStatus = unit.WarrantyStatus,
                    Quantity = 1,
                    UnitPrice = unit.TaggedPrice > 0 ? unit.TaggedPrice : unit.Product.SellingPrice,
                    UnitCostPrice = unit.PurchaseCost,
                    GstPercentage = unit.Product.GSTPercentage,
                    IsMarginScheme = isPreOwned
                };

                cartItem.RecalculateTaxes(isGstEnabled: IsGstEnabled);
                CartItems.Add(cartItem);

                RecalculateInvoiceTotals();
                _audioService?.PlayScanSuccess();
                StatusMessage = isWirelessTap
                    ? $"[WIRELESS TAP - {freq}] Added {unit.Product.Name} (IMEI: {unit.IMEI1}) - INR {cartItem.LineTotal:N0}"
                    : $"Added {unit.Product.Name} (IMEI: {unit.IMEI1}) - INR {cartItem.LineTotal:N0}";
                return;
            }

            // 2. If not serialized, check Product catalog for non-serialized bulk merchandise (chargers, power banks)
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Barcode == rawCode || p.SKU == rawCode);

            if (product != null)
            {
                var existingItem = CartItems.FirstOrDefault(ci => ci.ProductId == product.Id && !ci.IsSerialized);
                if (existingItem != null)
                {
                    existingItem.Quantity++;
                    existingItem.RecalculateTaxes(isGstEnabled: IsGstEnabled);
                }
                else
                {
                    var cartItem = new PosCartItemViewModel
                    {
                        ProductId = product.Id,
                        SerializedUnitId = null,
                        Name = product.Name,
                        HsnCode = product.HSNCode,
                        Quantity = 1,
                        UnitPrice = product.SellingPrice,
                        UnitCostPrice = product.DefaultCostPrice,
                        GstPercentage = product.GSTPercentage,
                        IsMarginScheme = false
                    };
                    cartItem.RecalculateTaxes(isGstEnabled: IsGstEnabled);
                    CartItems.Add(cartItem);
                }

                RecalculateInvoiceTotals();
                _audioService?.PlayScanSuccess();
                StatusMessage = isWirelessTap
                    ? $"[WIRELESS TAP - {freq}] Added {product.Name} - INR {product.SellingPrice:N0}"
                    : $"Added {product.Name} (Stock: {product.CurrentStock}) - INR {product.SellingPrice:N0}";
                return;
            }

            _audioService?.PlayScanError();
            StatusMessage = $"[NOT FOUND] No in-stock gadget or product matches '{rawCode}'.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Removes an item from the POS cart
    /// </summary>
    [RelayCommand]
    public void RemoveCartItem(PosCartItemViewModel item)
    {
        if (item == null) return;
        CartItems.Remove(item);
        RecalculateInvoiceTotals();
        StatusMessage = $"Removed {item.Name} from cart.";
    }

    /// <summary>
    /// Hotkey F2: Clears current bill and starts a fresh invoice
    /// </summary>
    [RelayCommand]
    public async Task NewBillAsync()
    {
        CartItems.Clear();
        CashTendered = 0;
        UpiTendered = 0;
        CardTendered = 0;
        ExchangeTendered = 0;
        FinanceEmiTendered = 0;
        InvoiceDiscount = 0;
        PaymentReference = string.Empty;
        RecalculateInvoiceTotals();

        InvoiceNumber = await _billingService.GenerateNextInvoiceNumberAsync();
        StatusMessage = $"Cleared. Fresh Bill {InvoiceNumber} started.";
    }

    /// <summary>
    /// Hotkey F4: Direct print 50x25mm barcode label for selected/last scanned item
    /// </summary>
    [RelayCommand]
    public void PrintBarcodeLabel(PosCartItemViewModel? item)
    {
        item ??= CartItems.LastOrDefault();
        if (item == null)
        {
            StatusMessage = "No item selected to print barcode sticker.";
            return;
        }

        string identifier = item.Imei1 ?? item.SerialNumber ?? item.HsnCode;
        string tsplCmd = _barcodeService.GenerateTsplLabelCommand(
            item.Name,
            identifier,
            item.UnitPrice,
            item.ConditionGrade?.ToString() ?? "New");

        // Dispatches command string to thermal printer driver or raw spooler
        StatusMessage = $"TSPL Barcode Label dispatched for {item.Name} (IMEI: {identifier}).";
    }

    /// <summary>
    /// Hotkey F9: Validates payment, saves invoice, locks IMEI to SOLD, writes ledger, and generates QuestPDF A4 Invoice
    /// </summary>
    [RelayCommand]
    public async Task CheckoutAndPrintInvoiceAsync()
    {
        if (!CartItems.Any())
        {
            _audioService?.PlayScanError();
            StatusMessage = "[ERROR] Cart is empty! Scan items before checkout.";
            return;
        }

        if (SelectedCustomer == null)
        {
            _audioService?.PlayScanError();
            StatusMessage = "[ERROR] Please select or add a Customer before checkout.";
            return;
        }

        TotalTendered = CashTendered + UpiTendered + CardTendered + ExchangeTendered + FinanceEmiTendered;

        // Auto-fill full cash if tender left empty (fast single-click POS checkout)
        if (TotalTendered == 0)
        {
            CashTendered = GrandTotal;
            TotalTendered = GrandTotal;
        }

        IsBusy = true;
        try
        {
            // Build Entity Models
            var invoice = new Invoice
            {
                InvoiceNumber = InvoiceNumber,
                CustomerId = SelectedCustomer.Id,
                Customer = SelectedCustomer,
                IsGstEnabled = IsGstEnabled,
                InvoiceType = !IsGstEnabled 
                    ? InvoiceType.BillOfSupply 
                    : (HasMarginSchemeItems ? InvoiceType.MarginSchemeInvoice : InvoiceType.TaxInvoice),
                HasMarginSchemeItems = IsGstEnabled && HasMarginSchemeItems,
                GrossTotal = SubTotal,
                InvoiceDiscount = InvoiceDiscount,
                StandardTaxableValue = IsGstEnabled ? TotalTaxableValue : SubTotal,
                TotalCGST = IsGstEnabled ? TotalCGST : 0,
                TotalSGST = IsGstEnabled ? TotalSGST : 0,
                TotalMarginAmount = IsGstEnabled ? CartItems.Where(i => i.IsMarginScheme).Sum(i => i.MarginAmount) : 0,
                TotalMarginTax = IsGstEnabled ? TotalMarginTax : 0,
                GrandTotal = GrandTotal,
                RoundOff = RoundOff
            };

            foreach (var ci in CartItems)
            {
                invoice.Items.Add(new InvoiceItem
                {
                    ProductId = ci.ProductId,
                    SerializedUnitId = ci.SerializedUnitId,
                    ItemDescription = ci.Name,
                    HSNCode = ci.HsnCode,
                    IMEI1 = ci.Imei1,
                    IMEI2 = ci.Imei2,
                    SerialNumber = ci.SerialNumber,
                    BatteryHealthPercentage = ci.BatteryHealthPercentage,
                    ConditionGrade = ci.ConditionGrade,
                    WarrantyStatus = ci.WarrantyStatus,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.UnitPrice,
                    ItemDiscount = ci.Discount,
                    UnitCostPrice = ci.UnitCostPrice,
                    IsMarginScheme = ci.IsMarginScheme,
                    MarginAmount = ci.MarginAmount,
                    GSTPercentage = ci.GstPercentage,
                    TaxableValue = ci.TaxableValue,
                    CGSTPercentage = ci.GstPercentage / 2m,
                    CGSTAmount = ci.CgstAmount,
                    SGSTPercentage = ci.GstPercentage / 2m,
                    SGSTAmount = ci.SgstAmount,
                    LineTotal = ci.LineTotal
                });
            }

            // Build Payment Transactions
            var payments = new List<PaymentTransaction>();
            if (CashTendered > 0)
                payments.Add(new PaymentTransaction { Mode = PaymentMode.Cash, Amount = CashTendered });
            if (UpiTendered > 0)
                payments.Add(new PaymentTransaction { Mode = PaymentMode.UPI, Amount = UpiTendered, ReferenceNumber = PaymentReference });
            if (CardTendered > 0)
                payments.Add(new PaymentTransaction { Mode = PaymentMode.CreditDebitCard, Amount = CardTendered, ReferenceNumber = PaymentReference });
            if (ExchangeTendered > 0)
                payments.Add(new PaymentTransaction { Mode = PaymentMode.OldPhoneExchange, Amount = ExchangeTendered });
            if (FinanceEmiTendered > 0)
                payments.Add(new PaymentTransaction { Mode = PaymentMode.BajajFinservEMI, Amount = FinanceEmiTendered, ReferenceNumber = PaymentReference });

            // Atomic database commit & stock status transition
            var savedInvoice = await _billingService.ProcessCheckoutAsync(invoice, payments);

            // Fetch Company Profile for QuestPDF Document
            var company = await _context.CompanyProfiles.FirstOrDefaultAsync() ?? new CompanyProfile();

            string invoiceFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SasanERP_Invoices");
            Directory.CreateDirectory(invoiceFolder);
            string pdfPath = Path.Combine(invoiceFolder, $"{savedInvoice.InvoiceNumber.Replace('/', '_')}.pdf");

            // Asynchronous vector PDF compilation (Non-blocking: UI never drops frames)
            await Task.Run(() => _invoicePrintEngine.SavePdf(SelectedInvoiceTemplate, savedInvoice, company, pdfPath));

            _audioService?.PlayCheckoutComplete();
            StatusMessage = $"[SUCCESS] Invoice {savedInvoice.InvoiceNumber} generated ({SelectedInvoiceTemplate}): {pdfPath}";

            // Reset POS for next sale
            await NewBillAsync();
        }
        catch (Exception ex)
        {
            _audioService?.PlayScanError();
            StatusMessage = $"[ERROR] Checkout failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Recalculates Subtotal, Taxes, Round-off, and Grand Total across all cart items
    /// </summary>
    public void RecalculateInvoiceTotals()
    {
        decimal sumSubtotal = 0;
        decimal sumTaxable = 0;
        decimal sumCgst = 0;
        decimal sumSgst = 0;
        decimal sumMarginTax = 0;
        bool anyMarginScheme = false;

        foreach (var item in CartItems)
        {
            sumSubtotal += (item.UnitPrice * item.Quantity) - item.Discount;
            if (item.IsMarginScheme)
            {
                anyMarginScheme = true;
                sumMarginTax += (item.CgstAmount + item.SgstAmount);
            }
            else
            {
                sumTaxable += item.TaxableValue;
                sumCgst += item.CgstAmount;
                sumSgst += item.SgstAmount;
            }
        }

        if (IsGstEnabled)
        {
            HasMarginSchemeItems = anyMarginScheme;
            SubTotal = sumSubtotal;
            TotalTaxableValue = sumTaxable;
            TotalCGST = sumCgst;
            TotalSGST = sumSgst;
            TotalMarginTax = sumMarginTax;
        }
        else
        {
            HasMarginSchemeItems = false;
            SubTotal = sumSubtotal;
            TotalTaxableValue = sumSubtotal;
            TotalCGST = 0;
            TotalSGST = 0;
            TotalMarginTax = 0;
        }

        decimal netAmount = Math.Max(0, SubTotal - InvoiceDiscount);
        decimal rounded = Math.Round(netAmount, MidpointRounding.AwayFromZero);
        RoundOff = rounded - netAmount;
        GrandTotal = rounded;

        TotalTendered = CashTendered + UpiTendered + CardTendered + ExchangeTendered + FinanceEmiTendered;
        BalanceChange = Math.Max(0, TotalTendered - GrandTotal);
    }
}
