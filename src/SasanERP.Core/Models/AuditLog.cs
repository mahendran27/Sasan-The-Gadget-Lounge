namespace SasanERP.Core.Models;

/// <summary>
/// Security Audit Log Entity for tracking financial transactions, device status transitions,
/// inventory movements, and administrative changes for Sasan The Gadget Lounge ERP.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string Action { get; set; } = string.Empty; // e.g. "InvoiceCreated", "DeviceStatusChanged", "ExpenseRecorded", "BackupCreated"
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = "Admin";
    public string Details { get; set; } = string.Empty;
    public string? MachineName { get; set; } = Environment.MachineName;
}
