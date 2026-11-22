namespace SasanERP.Core.Models;

/// <summary>
/// Customer Master for retail sales, credit accounts, and pre-owned buyback sellers
/// </summary>
public class Customer
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string City { get; set; } = "Theni";
    public string State { get; set; } = "Tamil Nadu";
    public string StateCode { get; set; } = "33"; // Tamil Nadu
    public string? GSTIN { get; set; }

    /// <summary>
    /// Aadhaar or Government ID number (Mandatory for used device buybacks under police record guidelines)
    /// </summary>
    public string? AadhaarNumber { get; set; }

    /// <summary>
    /// Current pending credit balance owed by the customer
    /// </summary>
    public decimal OutstandingCredit { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public virtual ICollection<SerializedUnit> DevicesSoldToShop { get; set; } = new List<SerializedUnit>();
}
