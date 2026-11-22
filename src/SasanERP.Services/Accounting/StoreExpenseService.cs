using Microsoft.EntityFrameworkCore;
using SasanERP.Core.Enums;
using SasanERP.Core.Models;
using SasanERP.Data.Context;
using SasanERP.Services.Security;

namespace SasanERP.Services.Accounting;

public record ExpenseEntryDto(
    int Id,
    string VoucherNumber,
    DateTime TransactionDate,
    string AccountCode,
    string AccountName,
    decimal Amount,
    PaymentMode Mode,
    string Description,
    string RecordedBy);

public class ExpenseSummaryDto
{
    public decimal TotalExpenses { get; set; }
    public decimal CashExpenses { get; set; }
    public decimal BankExpenses { get; set; }
    public Dictionary<string, decimal> CategoryTotals { get; set; } = new();
    public List<ExpenseEntryDto> Entries { get; set; } = new();
}

/// <summary>
/// Store Operating Expense & Petty Cash Service for Sasan The Gadget Lounge.
/// Enforces balanced double-entry accounting (Expense A/c Dr. to Cash/Bank Cr.)
/// for showroom rent, staff wages, utilities, tea/hospitality, and maintenance.
/// </summary>
public class StoreExpenseService
{
    private readonly AppDbContext _context;
    private readonly AuditLogService _auditLogService;

    public StoreExpenseService(AppDbContext context, AuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Returns all available operational expense accounts (5004-5011).
    /// </summary>
    public async Task<List<Account>> GetExpenseAccountsAsync()
    {
        return await _context.Accounts
            .Where(a => a.Type == AccountType.Expense && a.AccountCode != "5001") // exclude COGS
            .OrderBy(a => a.AccountCode)
            .ToListAsync();
    }

    /// <summary>
    /// Records a store operating expense with atomic double-entry ledger vouchers.
    /// Debits the Expense Account and Credits Cash (1001) or SBI Bank (1002).
    /// </summary>
    public async Task<string> RecordExpenseAsync(
        string accountCode,
        decimal amount,
        PaymentMode paymentMode,
        string description,
        string recordedBy = "Owner")
    {
        if (amount <= 0) throw new ArgumentException("Expense amount must be greater than zero.");

        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var expenseAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == accountCode);
            if (expenseAcc == null)
                throw new KeyNotFoundException($"Expense account '{accountCode}' not found.");

            string sourceAccCode = paymentMode == PaymentMode.Cash ? "1001" : "1002";
            var sourceAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == sourceAccCode);
            if (sourceAcc == null)
                throw new KeyNotFoundException($"Source payment account '{sourceAccCode}' not found.");

            string voucherNo = $"EXP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            var now = DateTime.UtcNow;

            // 1. Debit Expense Account
            _context.LedgerEntries.Add(new LedgerEntry
            {
                AccountId = expenseAcc.Id,
                VoucherNumber = voucherNo,
                TransactionDate = now,
                EntryType = LedgerEntryType.Debit,
                Amount = amount,
                Narration = $"[EXPENSE] {description} ({expenseAcc.Name})",
                ReferenceType = "StoreExpense"
            });
            expenseAcc.CurrentBalance += amount;

            // 2. Credit Cash / Bank Account
            _context.LedgerEntries.Add(new LedgerEntry
            {
                AccountId = sourceAcc.Id,
                VoucherNumber = voucherNo,
                TransactionDate = now,
                EntryType = LedgerEntryType.Credit,
                Amount = amount,
                Narration = $"Paid via {paymentMode} for {expenseAcc.Name}: {description}",
                ReferenceType = "StoreExpense"
            });
            sourceAcc.CurrentBalance -= amount;

            // 3. Security Audit Trail
            await _auditLogService.LogAsync(
                "ExpenseRecorded",
                "StoreExpense",
                voucherNo,
                $"₹{amount:N2} paid from {sourceAcc.Name} for {expenseAcc.Name}: {description}",
                recordedBy);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return voucherNo;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Retrieves all store operating expenses within a date window and computes category breakdown.
    /// </summary>
    public async Task<ExpenseSummaryDto> GetExpensesAsync(DateTime fromUtc, DateTime toUtc)
    {
        var summary = new ExpenseSummaryDto();

        var entries = await _context.LedgerEntries
            .Include(le => le.Account)
            .Where(le => le.ReferenceType == "StoreExpense" 
                      && le.EntryType == LedgerEntryType.Debit
                      && le.TransactionDate >= fromUtc 
                      && le.TransactionDate <= toUtc)
            .OrderByDescending(le => le.TransactionDate)
            .ToListAsync();

        foreach (var e in entries)
        {
            summary.TotalExpenses += e.Amount;
            string catName = e.Account?.Name ?? "General Expense";
            if (!summary.CategoryTotals.ContainsKey(catName))
                summary.CategoryTotals[catName] = 0m;
            summary.CategoryTotals[catName] += e.Amount;

            var matchingCredit = await _context.LedgerEntries
                .Include(le => le.Account)
                .FirstOrDefaultAsync(le => le.VoucherNumber == e.VoucherNumber && le.EntryType == LedgerEntryType.Credit);

            PaymentMode mode = matchingCredit?.Account?.AccountCode == "1001" ? PaymentMode.Cash : PaymentMode.UPI;
            if (mode == PaymentMode.Cash) summary.CashExpenses += e.Amount;
            else summary.BankExpenses += e.Amount;

            summary.Entries.Add(new ExpenseEntryDto(
                e.Id,
                e.VoucherNumber,
                e.TransactionDate,
                e.Account?.AccountCode ?? "",
                catName,
                e.Amount,
                mode,
                e.Narration,
                "StoreAdmin"
            ));
        }

        return summary;
    }
}
