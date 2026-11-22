using SasanERP.Core.Enums;

namespace SasanERP.Core.Models;

/// <summary>
/// Sales Invoice Header representing Tax Invoices and Margin Scheme Invoices
/// </summary>
public class Invoice
{
    public int Id { get; set; }

    /// <summary>
    /// Sequential Invoice Number (e.g. "SGL/24-25/0042")
    /// </summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

    public int CustomerId { get; set; }
    public virtual Customer Customer { get; set; } = null!;

    public InvoiceType InvoiceType { get; set; } = InvoiceType.TaxInvoice;

    /// <summary>
    /// Toggle to enable or disable GST on this invoice. When disabled, tax is 0.
    /// </summary>
    public bool IsGstEnabled { get; set; } = true;

    /// <summary>
    /// Indicates whether any items in this bill are billed under GST Rule 32(5) Margin Scheme
    /// </summary>
    public bool HasMarginSchemeItems { get; set; }

    /// <summary>
    /// Sum of items' gross prices before invoice-level discount
    /// </summary>
    public decimal GrossTotal { get; set; }

    /// <summary>
    /// Overall discount given on the bill
    /// </summary>
    public decimal InvoiceDiscount { get; set; }

    /// <summary>
    /// Total taxable value for Standard GST items
    /// </summary>
    public decimal StandardTaxableValue { get; set; }
    public decimal TotalTaxableValue => StandardTaxableValue + TotalMarginAmount;

    /// <summary>
    /// Total Central GST collected (Standard items)
    /// </summary>
    public decimal TotalCGST { get; set; }

    /// <summary>
    /// Total State GST collected (Standard items)
    /// </summary>
    public decimal TotalSGST { get; set; }

    /// <summary>
    /// Total Integrated GST collected (Inter-state items)
    /// </summary>
    public decimal TotalIGST { get; set; }

    /// <summary>
    /// Total profit margin across all Rule 32(5) pre-owned items
    /// </summary>
    public decimal TotalMarginAmount { get; set; }

    /// <summary>
    /// Total GST payable on margin (internal tax liability under Rule 32(5))
    /// </summary>
    public decimal TotalMarginTax { get; set; }

    /// <summary>
    /// Final bill amount payable by customer
    /// </summary>
    public decimal GrandTotal { get; set; }

    /// <summary>
    /// Rounding adjustment to nearest rupee
    /// </summary>
    public decimal RoundOff { get; set; }

    /// <summary>
    /// Total amount received across all payment tenders
    /// </summary>
    public decimal AmountPaid { get; set; }

    /// <summary>
    /// Outstanding balance added to customer ledger if unpaid
    /// </summary>
    public decimal BalanceDue { get; set; }

    public string BilledBy { get; set; } = "Admin";
    public string? Remarks { get; set; }

    /// <summary>
    /// Administrative void / cancellation state (God Mode)
    /// </summary>
    public bool IsVoided { get; set; }
    public string? VoidReason { get; set; }
    public DateTime? VoidedDate { get; set; }

    // Navigation properties
    public virtual ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public virtual ICollection<PaymentTransaction> Payments { get; set; } = new List<PaymentTransaction>();
    public virtual ICollection<SerializedUnit> SoldDevices { get; set; } = new List<SerializedUnit>();
}
