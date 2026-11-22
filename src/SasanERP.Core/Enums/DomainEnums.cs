namespace SasanERP.Core.Enums;

/// <summary>
/// Categories of devices and retail merchandise sold at Sasan The Gadget Lounge
/// </summary>
public enum DeviceType
{
    Smartphone = 1,
    Laptop = 2,
    Drone = 3,
    Charger = 4,
    PowerBank = 5,
    Toy = 6,
    AudioPlayer = 7,
    Smartwatch = 8,
    Accessory = 9
}

/// <summary>
/// Physical condition grading for pre-owned/second-hand gadgets
/// </summary>
public enum ConditionGrade
{
    BrandNew = 0,
    GradeAPlus_Flawless = 1,      // Mint, zero scratches, 100% functional
    GradeA_MinorWear = 2,         // Very light usage marks, excellent screen
    GradeB_VisibleScratches = 3,  // Normal wear, small scuffs, fully tested
    GradeC_DefectiveParts = 4     // Screen replaced or cosmetic flaws, discounted
}

/// <summary>
/// Warranty coverage offered to retail buyer
/// </summary>
public enum WarrantyStatus
{
    BrandWarrantyRemaining = 1,
    ShopWarranty_30Days = 2,
    ShopWarranty_90Days = 3,
    ShopWarranty_180Days = 4,
    ShopWarranty_1Year = 5,
    OutWarranty = 6
}

/// <summary>
/// Intake source of inventory items
/// </summary>
public enum PurchaseSource
{
    BrandDistributor = 1,
    DirectImport = 2,
    CustomerBuyback = 3,
    DealerExchange = 4
}

/// <summary>
/// Current inventory tracking lifecycle state of an individual unit
/// </summary>
public enum UnitStatus
{
    InStock = 1,
    Reserved = 2,
    Sold = 3,
    ReturnedToSupplier = 4,
    CustomerReturnRMA = 5,
    WrittenOffDefective = 6
}

/// <summary>
/// Multi-tender payment modes supported at POS
/// </summary>
public enum PaymentMode
{
    Cash = 1,
    UPI = 2,
    CreditDebitCard = 3,
    BajajFinservEMI = 4,
    HdbFinanceEMI = 5,
    PineLabsEMI = 6,
    OldPhoneExchange = 7
}

/// <summary>
/// Indian GST Invoice Classification
/// </summary>
public enum InvoiceType
{
    TaxInvoice = 1,            // Standard GST Invoice (CGST/SGST/IGST)
    MarginSchemeInvoice = 2,   // Rule 32(5) of CGST Rules 2017 (Second-Hand Goods)
    BillOfSupply = 3           // Exempted / Composite supply
}

/// <summary>
/// Financial chart of accounts classification
/// </summary>
public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Revenue = 4,
    Expense = 5
}

/// <summary>
/// Double-entry ledger entry classification
/// </summary>
public enum LedgerEntryType
{
    Debit = 1,
    Credit = 2
}
