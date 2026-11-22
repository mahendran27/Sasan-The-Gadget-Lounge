using SasanERP.Core.Enums;

namespace SasanERP.Core.Models;

/// <summary>
/// Payment split record for multi-tender transactions (Cash, UPI, Card, EMI, Exchange)
/// </summary>
public class PaymentTransaction
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public virtual Invoice Invoice { get; set; } = null!;

    public PaymentMode Mode { get; set; } = PaymentMode.Cash;

    public decimal Amount { get; set; }

    /// <summary>
    /// UPI Reference Number, Card Auth Code, or Bajaj/HDB EMI Approval Number
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// If payment mode is OldPhoneExchange, links to the serialized device taken into inventory
    /// </summary>
    public int? ExchangeDeviceId { get; set; }
    public virtual SerializedUnit? ExchangeDevice { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
}
