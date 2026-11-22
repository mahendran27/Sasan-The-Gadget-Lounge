using SasanERP.Core.Enums;

namespace SasanERP.Core.Models;

/// <summary>
/// General Ledger Account in the Chart of Accounts
/// </summary>
public class Account
{
    public int Id { get; set; }
    public string AccountCode { get; set; } = string.Empty; // e.g., "1001" for Cash, "4001" for Sales
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; } = AccountType.Asset;
    public string? Description { get; set; }
    public decimal CurrentBalance { get; set; }
    public bool IsSystemAccount { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
}

/// <summary>
/// Double-entry journal/ledger entry ensuring ACID balanced financial accounting
/// </summary>
public class LedgerEntry
{
    public int Id { get; set; }

    public int AccountId { get; set; }
    public virtual Account Account { get; set; } = null!;

    /// <summary>
    /// Voucher identifier grouping the balanced debit and credit entries of a transaction
    /// </summary>
    public string VoucherNumber { get; set; } = string.Empty;

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    public LedgerEntryType EntryType { get; set; } = LedgerEntryType.Debit;

    public decimal Amount { get; set; }

    public string Narration { get; set; } = string.Empty;

    /// <summary>
    /// Entity ID that generated this entry (e.g. InvoiceId, BuybackUnitId, ExpenseId)
    /// </summary>
    public int? ReferenceId { get; set; }

    /// <summary>
    /// Entity table name (e.g. "Invoice", "SerializedUnit", "Expense")
    /// </summary>
    public string? ReferenceType { get; set; }
}

/// <summary>
/// Operational and Store Expenses (Import duties, courier, rent, electricity, tea/pantry)
/// </summary>
public class Expense
{
    public int Id { get; set; }
    public string Category { get; set; } = "Shop Maintenance"; // "Import Customs/Duty", "Courier & Freight", "Rent", "Electricity"
    public decimal Amount { get; set; }
    public PaymentMode PaymentMode { get; set; } = PaymentMode.Cash;
    public string? ReferenceNumber { get; set; }
    public string PaidTo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
}
