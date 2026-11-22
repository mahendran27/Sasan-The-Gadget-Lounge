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

/// <summary>
/// Available Invoice Template Layout Styles (10 A4 Designs + POS Roll & Mobile WhatsApp)
/// Fully compatible with both Full Color and Black & White / Grayscale laser printing.
/// </summary>
public enum InvoiceTemplateStyle
{
    ExecutiveMinimalist = 1,          // A4 Style 1: Clean hairline dividers, monospaced numbers, B2B corporate look
    LuxuryGadgetLounge = 2,           // A4 Style 2: Obsidian/Cyan accent, watermark, platinum diagnostic card
    Rule32_5_MarginSchemeAudit = 3,   // A4 Style 3: Pre-owned focus, statutory preamble, CA-auditable margin breakdown
    ThermalRoll_80mm = 4,             // POS Roll Style: 3-inch 80mm ESC/POS continuous roll with bottom dynamic UPI QR
    DualCopyCounterfoil = 5,          // A4 Style 4: A4 halved into Buyer Copy + Shop Signed Warranty Counterfoil
    EcoDigitalWhatsApp = 6,           // Mobile Style: Smartphone 9:16 portrait layout with clickable Maps/Instagram links
    DeviceHealthCertificate = 7,      // A4 Style 5: Includes 12-point technician hardware inspection checklist
    ModernNeoBrutalist = 8,           // A4 Style 6: High-contrast 2pt geometric borders, punchy laser B&W & Color
    ClassicCorporateGst = 9,          // A4 Style 7: Traditional formal Indian GST invoice with dual tax & bank mandate
    DarkCyberLounge = 10,             // A4 Style 8: Tech Lounge dark/light hybrid with electric cyan & amber accents
    CompactTechDocket = 11,           // A4 Style 9: Dense 2-column spatial docket layout with full gadget telemetry
    EliteBoutiqueWatermark = 12       // A4 Style 10: Luxury boutique styling with ornate dividers and central watermark
}

/// <summary>
/// Available UI Themes and Ergonomic Display Modes (Choices 1 to 6)
/// </summary>
public enum AppThemeMode
{
    ElectricCyberCyan = 1,            // Choice 1: Windows 11 Fluent Dark (Obsidian #08101E, Cyan #00E5FF, Amber #FF9100)
    WarmTitaniumQuartz = 2,           // Choice 2: Architectural Light Mode (White #F8F9FA, Slate #334155, Titanium Gold #D4AF37)
    NordicFrost = 3,                  // Choice 3: Mica & Acrylic translucent glass surfaces with subtle elevation
    KioskTouchMode = 4,               // Choice 4: Touchscreen-friendly 48px+ targets and on-screen numpad
    TerminalSpeedHud = 5,             // Choice 5: Bloomberg/FinTech ultra-fast keyboard-only HUD
    AdaptiveDayNightShift = 6         // Choice 6: Auto shifts between Light (9am-6pm) and Dark (6pm-10pm) based on clock
}

/// <summary>
/// Available Barcode Sticker & Label Designs (Choices 1 to 7)
/// </summary>
public enum BarcodeLabelStyle
{
    Flagship_50x30mm = 1,             // Choice 1: All-in-one thermal sticker (Shop, Model, Battery %, Grade, Warranty, Barcode, IMEI, Price)
    CompactAccessory_50x25mm = 2,     // Choice 2: Streamlined tag for chargers, power banks, earphones (SKU, Barcode, Price)
    DumbbellJewelryTag = 3,           // Choice 3: Butterfly/dumbbell wrap tag for smartwatches, earbuds cases, braided cables
    TamperWarrantyVoidSeal_25mm = 4,  // Choice 4: Round 25mm warranty seal over screw/SIM tray ("Warranty Void If Broken", Micro-QR)
    DualQrTechExplorer = 5,           // Choice 5: Left Code-128 scanner barcode + Right Customer Tech Explorer QR code
    MultiStickerA4Sheet_24Up = 6,     // Choice 6: Standard A4 24-up label sheet for batch import shipment intake
    MetallicSilverWeatherproof = 7    // Choice 7: Ultra-durable silver polyester layout with high-contrast borders
}
