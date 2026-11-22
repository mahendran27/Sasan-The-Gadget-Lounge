using SasanERP.Core.Enums;

namespace SasanERP.Core.Models;

/// <summary>
/// Line item in an invoice with serialized device specs and GST/Margin computation
/// </summary>
public class InvoiceItem
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public virtual Invoice Invoice { get; set; } = null!;

    public int ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;

    /// <summary>
    /// Foreign key to specific serialized device unit (null for bulk accessories)
    /// </summary>
    public int? SerializedUnitId { get; set; }
    public virtual SerializedUnit? SerializedUnit { get; set; }

    /// <summary>
    /// Line item description (printed on invoice)
    /// </summary>
    public string ItemDescription { get; set; } = string.Empty;

    public string HSNCode { get; set; } = "8517";

    // Serialized specifics captured at time of billing
    public string? IMEI1 { get; set; }
    public string? IMEI2 { get; set; }
    public string? SerialNumber { get; set; }
    public int? BatteryHealthPercentage { get; set; }
    public ConditionGrade? ConditionGrade { get; set; }
    public WarrantyStatus? WarrantyStatus { get; set; }

    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal ItemDiscount { get; set; }

    /// <summary>
    /// Acquisition cost of this item (for Rule 32(5) Margin computation)
    /// </summary>
    public decimal UnitCostPrice { get; set; }

    /// <summary>
    /// True if item is a pre-owned/second-hand device taxed under Rule 32(5)
    /// </summary>
    public bool IsMarginScheme { get; set; }

    /// <summary>
    /// Positive margin = Math.Max(0, (UnitPrice - ItemDiscount) - UnitCostPrice) * Quantity
    /// </summary>
    public decimal MarginAmount { get; set; }

    /// <summary>
    /// Applicable GST slab (e.g. 18.0)
    /// </summary>
    public decimal GSTPercentage { get; set; } = 18.0m;

    /// <summary>
    /// Taxable value under standard GST (or taxable portion of margin under Rule 32(5))
    /// </summary>
    public decimal TaxableValue { get; set; }

    public decimal CGSTPercentage { get; set; } = 9.0m;
    public decimal CGSTAmount { get; set; }

    public decimal SGSTPercentage { get; set; } = 9.0m;
    public decimal SGSTAmount { get; set; }

    public decimal IGSTPercentage { get; set; } = 0.0m;
    public decimal IGSTAmount { get; set; }

    /// <summary>
    /// Total line amount payable for this item
    /// </summary>
    public decimal LineTotal { get; set; }
}
