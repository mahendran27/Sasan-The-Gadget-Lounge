namespace SasanERP.Core.Models;

/// <summary>
/// Vendor, Importer, and Distributor Master
/// </summary>
public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? GSTIN { get; set; }
    public bool IsImporter { get; set; }
    public string? CountryOfOrigin { get; set; } // e.g. "Dubai (UAE)", "Hong Kong", "Singapore", "USA"
    public decimal OutstandingPayable { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<SerializedUnit> SuppliedUnits { get; set; } = new List<SerializedUnit>();
}
