using SasanERP.Core.Enums;

namespace SasanERP.Core.Models;

/// <summary>
/// Individual physical device unit tracked by IMEI and Serial Number.
/// Critical for mobile phones, laptops, and drones.
/// Stores exact purchase cost for Rule 32(5) Margin Scheme computation.
/// </summary>
public class SerializedUnit
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;

    /// <summary>
    /// Primary 15-digit International Mobile Station Equipment Identity
    /// </summary>
    public string IMEI1 { get; set; } = string.Empty;

    /// <summary>
    /// Secondary IMEI for Dual-SIM/eSIM devices
    /// </summary>
    public string? IMEI2 { get; set; }

    /// <summary>
    /// Hardware Serial Number (laptops, drones, iPhones)
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;
    public string StorageCapacity { get; set; } = string.Empty; // e.g. "128GB", "256GB", "1TB"

    /// <summary>
    /// Battery Maximum Capacity percentage (crucial for iPhones & MacBooks)
    /// </summary>
    public int? BatteryHealthPercentage { get; set; }

    /// <summary>
    /// Condition assessment: BrandNew, Grade A+, Grade A, Grade B, Grade C
    /// </summary>
    public ConditionGrade ConditionGrade { get; set; } = ConditionGrade.GradeAPlus_Flawless;

    /// <summary>
    /// Warranty status offered with this device
    /// </summary>
    public WarrantyStatus WarrantyStatus { get; set; } = WarrantyStatus.ShopWarranty_90Days;
    public DateTime? WarrantyExpiryDate { get; set; }

    /// <summary>
    /// Origin of device intake (Distributor, Direct Import, Customer Buyback)
    /// </summary>
    public PurchaseSource PurchaseSource { get; set; } = PurchaseSource.DirectImport;

    /// <summary>
    /// Individual device purchase cost in INR.
    /// Under GST Rule 32(5), Profit Margin = Selling Price - PurchaseCost.
    /// </summary>
    public decimal PurchaseCost { get; set; }

    /// <summary>
    /// Tagged retail selling price for this specific unit
    /// </summary>
    public decimal TaggedPrice { get; set; }

    /// <summary>
    /// Actual price sold for in POS invoice
    /// </summary>
    public decimal? ActualSoldPrice { get; set; }

    public UnitStatus Status { get; set; } = UnitStatus.InStock;

    // Supplier link (if bought from dealer/importer)
    public int? SupplierId { get; set; }
    public virtual Supplier? Supplier { get; set; }

    // Buyback seller link (if bought back from customer)
    public int? BuybackCustomerId { get; set; }
    public virtual Customer? BuybackCustomer { get; set; }

    /// <summary>
    /// Path to attached customer ID document (Aadhaar/Driving License) for anti-theft buyback compliance
    /// </summary>
    public string? CustomerAadhaarDocPath { get; set; }

    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

    // Sale relationship
    public int? SoldInvoiceId { get; set; }
    public virtual Invoice? SoldInvoice { get; set; }
    public DateTime? SoldDate { get; set; }

    public string? DiagnosticNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Days elapsed since device was acquired into inventory
    /// </summary>
    public int AgingDays => (int)(DateTime.UtcNow - PurchaseDate).TotalDays;

    /// <summary>
    /// True if device has been sitting in stock for more than 60 days
    /// </summary>
    public bool IsAgingAlert => Status == UnitStatus.InStock && AgingDays > 60;
}
