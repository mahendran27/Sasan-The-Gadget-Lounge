using SasanERP.Core.Enums;

namespace SasanERP.Core.Models;

/// <summary>
/// Master product entity for brand-new and pre-owned device catalogs and accessories
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public DeviceType Category { get; set; } = DeviceType.Smartphone;
    
    /// <summary>
    /// Stock Keeping Unit (e.g. "IP15PM-256-NT" or "ANK-PB-20000")
    /// </summary>
    public string SKU { get; set; } = string.Empty;
    
    /// <summary>
    /// Barcode string for scanning (EAN-13, UPC, or custom Code128)
    /// </summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// Harmonized System of Nomenclature code for GST (e.g., 8517 for phones, 8471 for laptops)
    /// </summary>
    public string HSNCode { get; set; } = "8517";

    /// <summary>
    /// If true, each unit is individually tracked with IMEI / Serial Number
    /// </summary>
    public bool IsSerialized { get; set; } = true;

    /// <summary>
    /// Cost price for bulk items or benchmark cost for new items
    /// </summary>
    public decimal DefaultCostPrice { get; set; }

    /// <summary>
    /// Maximum Retail Price printed on box
    /// </summary>
    public decimal MRP { get; set; }

    /// <summary>
    /// Standard selling price (inclusive of GST)
    /// </summary>
    public decimal SellingPrice { get; set; }

    /// <summary>
    /// Applicable GST tax slab rate (e.g. 18.0%)
    /// </summary>
    public decimal GSTPercentage { get; set; } = 18.0m;

    /// <summary>
    /// Current available stock (automatically calculated for serialized items)
    /// </summary>
    public int CurrentStock { get; set; }

    /// <summary>
    /// Minimum threshold before triggering reorder alert
    /// </summary>
    public int ReorderLevel { get; set; } = 2;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<SerializedUnit> SerializedUnits { get; set; } = new List<SerializedUnit>();
    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
}
