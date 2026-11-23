using System;
using System.Collections.Generic;
using SasanERP.Core.Models;

namespace SasanERP.UI.ViewModels;

/// <summary>
/// Snapshot model representing a parked/held POS transaction.
/// Enables cashiers to pause a sale, attend to another customer, and resume later seamlessly.
/// </summary>
public class HeldBill
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..6].ToUpper();
    public DateTime HeldAt { get; set; } = DateTime.Now;
    public string CustomerName { get; set; } = "Walk-in Retail Customer";
    public Customer? Customer { get; set; }
    public List<PosCartItemViewModel> Items { get; set; } = new();
    public decimal InvoiceDiscount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal CashTendered { get; set; }
    public decimal UpiTendered { get; set; }
    public decimal CardTendered { get; set; }
    public decimal ExchangeTendered { get; set; }
    public decimal FinanceEmiTendered { get; set; }
    public string? PaymentReference { get; set; }
    public bool IsGstEnabled { get; set; } = true;

    public string DisplaySummary => $"[{Id}] {CustomerName} • {Items.Count} item(s) • ₹{GrandTotal:N0} ({HeldAt:hh:mm tt})";
}
