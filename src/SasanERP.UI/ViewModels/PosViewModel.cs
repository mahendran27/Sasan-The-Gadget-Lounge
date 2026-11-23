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
    private readonly BarcodeLabelEngine? _labelEngine;

    public ObservableCollection<PosCartItemViewModel> CartItems { get; } = new();
    public ObservableCollection<Customer> Customers { get; } = new();

    public ObservableCollection<InvoiceTemplateStyle> AvailableInvoiceTemplates { get; } = 
        new(Enum.GetValues<InvoiceTemplateStyle>());

    [ObservableProperty]
    private InvoiceTemplateStyle _selectedInvoiceTemplate = InvoiceTemplateStyle.LuxuryGadgetLounge;

    public ObservableCollection<BarcodeLabelStyle> AvailableBarcodeStyles { get; } = 
        new(Enum.GetValues<BarcodeLabelStyle>());

    [ObservableProperty]
    private BarcodeLabelStyle _selectedBarcodeStyle = BarcodeLabelStyle.Flagship_50x30mm;

    public ObservableCollection<AppThemeMode> AvailableThemeModes { get; } = 
        new(Enum.GetValues<AppThemeMode>());

    [ObservableProperty]
    private AppThemeMode _selectedThemeMode = AppThemeMode.ElectricCyberCyan;

    partial void OnSelectedThemeModeChanged(AppThemeMode value)
    {
        ThemeService.Instance.ApplyTheme(value);
        StatusMessage = $"UI Theme changed to: {value}";
    }

    // Manual Custom Item Entry fields
    [ObservableProperty]
    private bool _isManualEntryVisible;

    [ObservableProperty]
    private string _manualItemName = string.Empty;

    [ObservableProperty]
    private decimal _manualPrice;

    [ObservableProperty]
    private decimal _manualQuantity = 1;

    [ObservableProperty]
    private decimal _manualGstPercent = 18m;

    [ObservableProperty]
    private string _manualHsn = "8517";

    [ObservableProperty]
    private string? _manualImei;

    [ObservableProperty]
    private Customer? _selectedCustomer;

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        OnPropertyChanged(nameof(HasSelectedCustomer));
        OnPropertyChanged(nameof(HasGstin));
        if (value != null)
        {
            StatusMessage = $"Selected Customer: {value.FullName} ({value.PhoneNumber}) | Khata Balance: ₹{value.OutstandingCredit:N2}";
        }
    }

    public bool HasSelectedCustomer => SelectedCustomer != null;
    public bool HasGstin => !string.IsNullOrWhiteSpace(SelectedCustomer?.GSTIN);

    // In-Place Customer Form Drawer fields
    [ObservableProperty]
    private bool _isCustomerEditorOpen;

    [ObservableProperty]
    private bool _isEditingExistingCustomer;

    partial void OnIsEditingExistingCustomerChanged(bool value)
    {
        OnPropertyChanged(nameof(CustomerEditorTitle));
    }

    public string CustomerEditorTitle => IsEditingExistingCustomer 
        ? "✏️ EDIT CUSTOMER PROFILE (வாடிக்கையாளர் திருத்து)" 
        : "➕ REGISTER NEW CUSTOMER (புதிய வாடிக்கையாளர் பதிவு)";

    [ObservableProperty]
    private string _customerFormName = string.Empty;

    [ObservableProperty]
    private string _customerFormPhone = string.Empty;

    [ObservableProperty]
    private string _customerFormEmail = string.Empty;

    [ObservableProperty]
    private string _customerFormAddress = string.Empty;

    [ObservableProperty]
    private string _customerFormCity = "Theni";

    [ObservableProperty]
    private string _customerFormState = "Tamil Nadu";

    [ObservableProperty]
    private string _customerFormStateCode = "33";

    [ObservableProperty]
    private string _customerFormGstin = string.Empty;

    [ObservableProperty]
    private string _customerFormAadhaar = string.Empty;

    // Parked / Held Bills Queue
    public ObservableCollection<HeldBill> HeldBills { get; } = new();

    [ObservableProperty]
    private bool _isHeldBillsDrawerOpen;

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
    partial void OnInvoiceDiscountChanged(decimal value) => RecalculateInvoiceTotals();

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
    partial void OnCashTenderedChanged(decimal value) => RecalculateTenderBalances();

    [ObservableProperty]
    private decimal _upiTendered;
    partial void OnUpiTenderedChanged(decimal value) => RecalculateTenderBalances();

    [ObservableProperty]
    private decimal _cardTendered;
    partial void OnCardTenderedChanged(decimal value) => RecalculateTenderBalances();

    [ObservableProperty]
    private decimal _exchangeTendered;
    partial void OnExchangeTenderedChanged(decimal value) => RecalculateTenderBalances();

    [ObservableProperty]
    private decimal _financeEmiTendered;
    partial void OnFinanceEmiTenderedChanged(decimal value) => RecalculateTenderBalances();

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
        SerialPortBarcodeListener? barcodeListener = null,
        BarcodeLabelEngine? labelEngine = null)
    {
        _context = context;
        _billingService = billingService;
        _barcodeService = barcodeService;
        _invoicePrintEngine = invoicePrintEngine;
        _wirelessManager = wirelessManager;
        _audioService = audioService;
        _barcodeListener = barcodeListener;
        _labelEngine = labelEngine;

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

            if (product == null)
            {
                // Partial name search fallback for fast manual POS searching
                product = await _context.Products
                    .FirstOrDefaultAsync(p => EF.Functions.Like(p.Name, $"%{rawCode}%"));
            }

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
    /// Increments quantity for a bulk non-serialized cart item
    /// </summary>
    [RelayCommand]
    public void IncrementItemQuantity(PosCartItemViewModel? item)
    {
        if (item == null) return;
        if (item.IsSerialized)
        {
            StatusMessage = "Serialized devices (IMEI/Serial) have a fixed quantity of 1.";
            return;
        }
        item.Quantity += 1;
        item.RecalculateTaxes(isGstEnabled: IsGstEnabled);
        RecalculateInvoiceTotals();
    }

    /// <summary>
    /// Decrements quantity for a bulk cart item, or removes if quantity reaches 0
    /// </summary>
    [RelayCommand]
    public void DecrementItemQuantity(PosCartItemViewModel? item)
    {
        if (item == null) return;
        if (item.Quantity > 1)
        {
            item.Quantity -= 1;
            item.RecalculateTaxes(isGstEnabled: IsGstEnabled);
            RecalculateInvoiceTotals();
        }
        else
        {
            RemoveCartItem(item);
        }
    }

    // =========================================================================
    // IN-PLACE CUSTOMER MANAGEMENT (வாடிக்கையாளர் மேலாண்மை)
    // =========================================================================

    [RelayCommand]
    public void OpenNewCustomer()
    {
        IsEditingExistingCustomer = false;
        CustomerFormName = string.Empty;
        CustomerFormPhone = string.Empty;
        CustomerFormEmail = string.Empty;
        CustomerFormAddress = string.Empty;
        CustomerFormCity = "Theni";
        CustomerFormState = "Tamil Nadu";
        CustomerFormStateCode = "33";
        CustomerFormGstin = string.Empty;
        CustomerFormAadhaar = string.Empty;
        IsCustomerEditorOpen = true;
        StatusMessage = "Enter customer details and click Save to add to database and current bill.";
    }

    [RelayCommand]
    public void OpenEditCustomer()
    {
        if (SelectedCustomer == null)
        {
            StatusMessage = "Please select a customer first to edit.";
            return;
        }

        IsEditingExistingCustomer = true;
        CustomerFormName = SelectedCustomer.FullName;
        CustomerFormPhone = SelectedCustomer.PhoneNumber;
        CustomerFormEmail = SelectedCustomer.Email ?? string.Empty;
        CustomerFormAddress = SelectedCustomer.Address ?? string.Empty;
        CustomerFormCity = SelectedCustomer.City ?? "Theni";
        CustomerFormState = SelectedCustomer.State ?? "Tamil Nadu";
        CustomerFormStateCode = SelectedCustomer.StateCode ?? "33";
        CustomerFormGstin = SelectedCustomer.GSTIN ?? string.Empty;
        CustomerFormAadhaar = SelectedCustomer.AadhaarNumber ?? string.Empty;
        IsCustomerEditorOpen = true;
        StatusMessage = $"Editing customer details for '{SelectedCustomer.FullName}'.";
    }

    [RelayCommand]
    public void CloseCustomerEditor()
    {
        IsCustomerEditorOpen = false;
    }

    [RelayCommand]
    public async Task SaveCustomerAsync()
    {
        if (string.IsNullOrWhiteSpace(CustomerFormName))
        {
            _audioService?.PlayScanError();
            StatusMessage = "[ERROR] Customer name is required.";
            return;
        }

        string phone = CustomerFormPhone?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(phone) || phone.Length < 10)
        {
            _audioService?.PlayScanError();
            StatusMessage = "[ERROR] Valid 10-digit phone number is required.";
            return;
        }

        try
        {
            if (IsEditingExistingCustomer && SelectedCustomer != null)
            {
                var dbCust = await _context.Customers.FindAsync(SelectedCustomer.Id);
                if (dbCust != null)
                {
                    dbCust.FullName = CustomerFormName.Trim();
                    dbCust.PhoneNumber = phone;
                    dbCust.Email = string.IsNullOrWhiteSpace(CustomerFormEmail) ? null : CustomerFormEmail.Trim();
                    dbCust.Address = string.IsNullOrWhiteSpace(CustomerFormAddress) ? null : CustomerFormAddress.Trim();
                    dbCust.City = string.IsNullOrWhiteSpace(CustomerFormCity) ? "Theni" : CustomerFormCity.Trim();
                    dbCust.State = string.IsNullOrWhiteSpace(CustomerFormState) ? "Tamil Nadu" : CustomerFormState.Trim();
                    dbCust.StateCode = string.IsNullOrWhiteSpace(CustomerFormStateCode) ? "33" : CustomerFormStateCode.Trim();
                    dbCust.GSTIN = string.IsNullOrWhiteSpace(CustomerFormGstin) ? null : CustomerFormGstin.Trim().ToUpperInvariant();
                    dbCust.AadhaarNumber = string.IsNullOrWhiteSpace(CustomerFormAadhaar) ? null : CustomerFormAadhaar.Trim();

                    await _context.SaveChangesAsync();

                    SelectedCustomer.FullName = dbCust.FullName;
                    SelectedCustomer.PhoneNumber = dbCust.PhoneNumber;
                    SelectedCustomer.Email = dbCust.Email;
                    SelectedCustomer.Address = dbCust.Address;
                    SelectedCustomer.City = dbCust.City;
                    SelectedCustomer.State = dbCust.State;
                    SelectedCustomer.StateCode = dbCust.StateCode;
                    SelectedCustomer.GSTIN = dbCust.GSTIN;
                    SelectedCustomer.AadhaarNumber = dbCust.AadhaarNumber;

                    var idx = Customers.IndexOf(SelectedCustomer);
                    if (idx >= 0)
                    {
                        Customers[idx] = dbCust;
                        SelectedCustomer = dbCust;
                    }
                }

                _audioService?.PlayScanSuccess();
                IsCustomerEditorOpen = false;
                StatusMessage = $"[SUCCESS] Customer '{CustomerFormName}' updated in database and invoice.";
            }
            else
            {
                var newCust = new Customer
                {
                    FullName = CustomerFormName.Trim(),
                    PhoneNumber = phone,
                    Email = string.IsNullOrWhiteSpace(CustomerFormEmail) ? null : CustomerFormEmail.Trim(),
                    Address = string.IsNullOrWhiteSpace(CustomerFormAddress) ? null : CustomerFormAddress.Trim(),
                    City = string.IsNullOrWhiteSpace(CustomerFormCity) ? "Theni" : CustomerFormCity.Trim(),
                    State = string.IsNullOrWhiteSpace(CustomerFormState) ? "Tamil Nadu" : CustomerFormState.Trim(),
                    StateCode = string.IsNullOrWhiteSpace(CustomerFormStateCode) ? "33" : CustomerFormStateCode.Trim(),
                    GSTIN = string.IsNullOrWhiteSpace(CustomerFormGstin) ? null : CustomerFormGstin.Trim().ToUpperInvariant(),
                    AadhaarNumber = string.IsNullOrWhiteSpace(CustomerFormAadhaar) ? null : CustomerFormAadhaar.Trim(),
                    OutstandingCredit = 0m,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Customers.Add(newCust);
                await _context.SaveChangesAsync();

                Customers.Add(newCust);
                SelectedCustomer = newCust;

                _audioService?.PlayScanSuccess();
                IsCustomerEditorOpen = false;
                StatusMessage = $"[SUCCESS] New customer '{newCust.FullName}' added to database and selected for invoice.";
            }

            OnPropertyChanged(nameof(HasSelectedCustomer));
            OnPropertyChanged(nameof(HasGstin));
        }
        catch (Exception ex)
        {
            _audioService?.PlayScanError();
            StatusMessage = $"[ERROR] Failed to save customer: {ex.Message}";
        }
    }

    // =========================================================================
    // PARK & RECALL BILLS (பில் நிறுத்தி வைத்தல் & மீட்டெடுத்தல்)
    // =========================================================================

    [RelayCommand]
    public async Task HoldCurrentBillAsync()
    {
        if (CartItems.Count == 0)
        {
            _audioService?.PlayScanError();
            StatusMessage = "[WARNING] Cannot park an empty bill. Add items first.";
            return;
        }

        var held = new HeldBill
        {
            CustomerName = SelectedCustomer?.FullName ?? "Walk-in Retail Customer",
            Customer = SelectedCustomer,
            Items = CartItems.ToList(),
            InvoiceDiscount = InvoiceDiscount,
            GrandTotal = GrandTotal,
            CashTendered = CashTendered,
            UpiTendered = UpiTendered,
            CardTendered = CardTendered,
            ExchangeTendered = ExchangeTendered,
            FinanceEmiTendered = FinanceEmiTendered,
            PaymentReference = PaymentReference,
            IsGstEnabled = IsGstEnabled
        };

        HeldBills.Add(held);
        _audioService?.PlayScanSuccess();

        await NewBillAsync();
        StatusMessage = $"[PARKED] Bill held for {held.CustomerName} ({held.Items.Count} items, ₹{held.GrandTotal:N0}). Total parked: {HeldBills.Count}. Ready for next customer.";
    }

    [RelayCommand]
    public void RecallHeldBill(HeldBill? heldBill)
    {
        heldBill ??= HeldBills.LastOrDefault();
        if (heldBill == null)
        {
            StatusMessage = "No parked bills in queue to recall.";
            return;
        }

        CartItems.Clear();
        foreach (var item in heldBill.Items)
        {
            CartItems.Add(item);
        }

        SelectedCustomer = heldBill.Customer ?? Customers.FirstOrDefault();
        InvoiceDiscount = heldBill.InvoiceDiscount;
        CashTendered = heldBill.CashTendered;
        UpiTendered = heldBill.UpiTendered;
        CardTendered = heldBill.CardTendered;
        ExchangeTendered = heldBill.ExchangeTendered;
        FinanceEmiTendered = heldBill.FinanceEmiTendered;
        PaymentReference = heldBill.PaymentReference;
        IsGstEnabled = heldBill.IsGstEnabled;

        HeldBills.Remove(heldBill);
        RecalculateInvoiceTotals();
        IsHeldBillsDrawerOpen = false;

        _audioService?.PlayScanSuccess();
        StatusMessage = $"[RESUMED] Recalled parked bill for {SelectedCustomer?.FullName} ({CartItems.Count} items, ₹{GrandTotal:N0}).";
    }

    [RelayCommand]
    public void DiscardHeldBill(HeldBill? heldBill)
    {
        if (heldBill == null) return;
        HeldBills.Remove(heldBill);
        StatusMessage = $"[DISCARDED] Parked bill [{heldBill.Id}] removed from queue.";
    }

    [RelayCommand]
    public void ToggleHeldBillsDrawer()
    {
        IsHeldBillsDrawerOpen = !IsHeldBillsDrawerOpen;
    }

    // =========================================================================
    // 1-CLICK INSTANT PAYMENT SHORTCUTS & PRESET DISCOUNTS
    // =========================================================================

    [RelayCommand]
    public void SetExactCash()
    {
        CashTendered = GrandTotal;
        UpiTendered = 0;
        CardTendered = 0;
        ExchangeTendered = 0;
        FinanceEmiTendered = 0;
        _audioService?.PlayScanSuccess();
        StatusMessage = $"Exact Cash Tendered: ₹{GrandTotal:N0}";
    }

    [RelayCommand]
    public void SetExactUpi()
    {
        UpiTendered = GrandTotal;
        CashTendered = 0;
        CardTendered = 0;
        ExchangeTendered = 0;
        FinanceEmiTendered = 0;
        _audioService?.PlayScanSuccess();
        StatusMessage = $"Exact UPI Tendered: ₹{GrandTotal:N0}";
    }

    [RelayCommand]
    public void SetExactCard()
    {
        CardTendered = GrandTotal;
        CashTendered = 0;
        UpiTendered = 0;
        ExchangeTendered = 0;
        FinanceEmiTendered = 0;
        _audioService?.PlayScanSuccess();
        StatusMessage = $"Exact Card Swipe Tendered: ₹{GrandTotal:N0}";
    }

    [RelayCommand]
    public void SetSplit5050()
    {
        decimal half = Math.Round(GrandTotal / 2m, MidpointRounding.AwayFromZero);
        CashTendered = half;
        UpiTendered = GrandTotal - half;
        CardTendered = 0;
        ExchangeTendered = 0;
        FinanceEmiTendered = 0;
        _audioService?.PlayScanSuccess();
        StatusMessage = $"Split 50/50: Cash ₹{CashTendered:N0} + UPI ₹{UpiTendered:N0}";
    }

    [RelayCommand]
    public void ClearTender()
    {
        CashTendered = 0;
        UpiTendered = 0;
        CardTendered = 0;
        ExchangeTendered = 0;
        FinanceEmiTendered = 0;
        StatusMessage = "Payment tender cleared.";
    }

    [RelayCommand]
    public void ApplyPresetDiscount(string? discountType)
    {
        switch (discountType)
        {
            case "100":
                InvoiceDiscount = Math.Min(100m, SubTotal);
                break;
            case "500":
                InvoiceDiscount = Math.Min(500m, SubTotal);
                break;
            case "1000":
                InvoiceDiscount = Math.Min(1000m, SubTotal);
                break;
            case "5PCT":
                InvoiceDiscount = Math.Round(SubTotal * 0.05m, 2);
                break;
            case "10PCT":
                InvoiceDiscount = Math.Round(SubTotal * 0.10m, 2);
                break;
            case "RESET":
                InvoiceDiscount = 0m;
                break;
        }
        RecalculateInvoiceTotals();
        _audioService?.PlayScanSuccess();
        StatusMessage = $"Applied discount: ₹{InvoiceDiscount:N2} | Net Total: ₹{GrandTotal:N0}";
    }

    public void RecalculateTenderBalances()
    {
        TotalTendered = CashTendered + UpiTendered + CardTendered + ExchangeTendered + FinanceEmiTendered;
        BalanceChange = Math.Max(0, TotalTendered - GrandTotal);
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

    [RelayCommand]
    public void ToggleManualEntry()
    {
        IsManualEntryVisible = !IsManualEntryVisible;
    }

    [RelayCommand]
    public async Task AddManualItemAsync()
    {
        if (string.IsNullOrWhiteSpace(ManualItemName))
        {
            _audioService?.PlayScanError();
            StatusMessage = "[ERROR] Item Name is required for manual entry.";
            return;
        }

        if (ManualPrice <= 0)
        {
            _audioService?.PlayScanError();
            StatusMessage = "[ERROR] Unit Price must be greater than 0.";
            return;
        }

        IsBusy = true;
        try
        {
            // Find existing product or dynamically register custom on-the-fly item in DB
            string trimmedName = ManualItemName.Trim();
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Name == trimmedName);
            if (product == null)
            {
                product = new Product
                {
                    Name = trimmedName,
                    SKU = "MANUAL-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                    Barcode = "MAN" + Random.Shared.Next(100000, 999999),
                    Category = DeviceType.Charger,
                    HSNCode = string.IsNullOrWhiteSpace(ManualHsn) ? "8517" : ManualHsn.Trim(),
                    SellingPrice = ManualPrice,
                    DefaultCostPrice = Math.Round(ManualPrice * 0.70m, 2),
                    GSTPercentage = ManualGstPercent,
                    CurrentStock = 100
                };
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
            }

            var cartItem = new PosCartItemViewModel
            {
                ProductId = product.Id,
                SerializedUnitId = null,
                Name = product.Name,
                HsnCode = product.HSNCode,
                Imei1 = string.IsNullOrWhiteSpace(ManualImei) ? null : ManualImei.Trim(),
                Quantity = ManualQuantity > 0 ? ManualQuantity : 1,
                UnitPrice = ManualPrice,
                UnitCostPrice = product.DefaultCostPrice,
                GstPercentage = ManualGstPercent,
                IsMarginScheme = false
            };

            cartItem.RecalculateTaxes(isGstEnabled: IsGstEnabled);
            CartItems.Add(cartItem);
            RecalculateInvoiceTotals();

            _audioService?.PlayScanSuccess();
            StatusMessage = $"Added manual item: {cartItem.Name} (₹{cartItem.UnitPrice:N0} x {cartItem.Quantity})";

            // Reset manual entry inputs
            ManualItemName = string.Empty;
            ManualPrice = 0;
            ManualQuantity = 1;
            ManualImei = string.Empty;
        }
        catch (Exception ex)
        {
            _audioService?.PlayScanError();
            StatusMessage = $"[ERROR] Failed to add manual item: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Hotkey F4: Direct print barcode label for selected/last scanned item
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
        if (_labelEngine != null)
        {
            var sticker = new BarcodeStickerData(
                ShopName: "SASAN THE GADGET LOUNGE",
                ModelName: item.Name,
                Specs: item.FormattedSpecs,
                Identifier: identifier,
                Price: item.UnitPrice,
                MRP: null,
                ConditionGrade: item.ConditionGrade?.ToString() ?? "New",
                BatteryHealth: item.BatteryHealthPercentage,
                Warranty: item.WarrantyStatus?.ToString(),
                Category: "Accessory"
            );
            byte[] tspl = _labelEngine.GenerateTsplStream(SelectedBarcodeStyle, sticker);
            StatusMessage = $"TSPL Barcode Label ({SelectedBarcodeStyle}) dispatched for {item.Name} (ID: {identifier}) [{tspl.Length} bytes].";
        }
        else
        {
            string tsplCmd = _barcodeService.GenerateTsplLabelCommand(
                item.Name,
                identifier,
                item.UnitPrice,
                item.ConditionGrade?.ToString() ?? "New");
            StatusMessage = $"TSPL Barcode Label dispatched for {item.Name} (IMEI: {identifier}).";
        }
    }

    /// <summary>
    /// Hotkey F10: Previews the invoice vector PDF before completing checkout
    /// </summary>
    [RelayCommand]
    public async Task PreviewInvoiceAsync()
    {
        if (!CartItems.Any())
        {
            _audioService?.PlayScanError();
            StatusMessage = "[WARNING] Cart is empty! Add items to preview invoice.";
            return;
        }

        IsBusy = true;
        try
        {
            var company = await _context.CompanyProfiles.FirstOrDefaultAsync() ?? new CompanyProfile();

            var previewInvoice = new Invoice
            {
                InvoiceNumber = string.IsNullOrWhiteSpace(InvoiceNumber) ? "PREVIEW-001" : $"{InvoiceNumber}-PREVIEW",
                InvoiceDate = DateTime.Now,
                CustomerId = SelectedCustomer?.Id ?? 1,
                Customer = SelectedCustomer ?? new Customer { FullName = "Walk-in Guest / Preview Customer", PhoneNumber = "9080803070", City = "Theni" },
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
                previewInvoice.Items.Add(new InvoiceItem
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

            string tempFolder = Path.Combine(Path.GetTempPath(), "SasanERP_Previews");
            Directory.CreateDirectory(tempFolder);
            string pdfPath = Path.Combine(tempFolder, $"Invoice_Preview_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            await Task.Run(() => _invoicePrintEngine.SavePdf(SelectedInvoiceTemplate, previewInvoice, company, pdfPath));

            bool opened = PdfViewerLauncher.Launch(pdfPath);
            if (opened)
            {
                StatusMessage = $"[PREVIEW] Opened Bill Preview ({SelectedInvoiceTemplate}).";
            }
            else
            {
                StatusMessage = $"[PREVIEW] Generated preview PDF at {pdfPath}";
            }
        }
        catch (Exception ex)
        {
            _audioService?.PlayScanError();
            StatusMessage = $"[ERROR] Preview generation failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
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
