using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SasanERP.Core.Enums;
using SasanERP.Core.Models;
using SasanERP.Data.Context;

namespace SasanERP.UI.ViewModels;

public class DaybookVoucherItem
{
    public int Id { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public string EntryType { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string Narration { get; set; } = string.Empty;
    public string? ReferenceType { get; set; }
}

public class AccountSummaryItem
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
}

public partial class DaybookViewModel : ObservableObject
{
    private readonly AppDbContext _context;

    public ObservableCollection<DaybookVoucherItem> Vouchers { get; } = new();
    public ObservableCollection<AccountSummaryItem> AccountsSummary { get; } = new();

    [ObservableProperty]
    private decimal _totalDebits;

    [ObservableProperty]
    private decimal _totalCredits;

    [ObservableProperty]
    private bool _isBalanced;

    // Expense Entry Fields
    [ObservableProperty]
    private string _expenseCategory = "Showroom Rent (Karthik Mahall)";

    public ObservableCollection<string> ExpenseCategories { get; } = new()
    {
        "Showroom Rent (Karthik Mahall)",
        "Electricity & Utilities",
        "Courier & Air Freight",
        "Import Customs Duty & Clearance",
        "Tea, Snacks & Staff Welfare",
        "Packaging & Sticker Materials",
        "Store Maintenance & Repairs"
    };

    [ObservableProperty]
    private decimal _expenseAmount;

    [ObservableProperty]
    private string _expensePaidTo = string.Empty;

    [ObservableProperty]
    private string _expenseDescription = string.Empty;

    [ObservableProperty]
    private string _expensePaymentMode = "Cash";

    public ObservableCollection<string> PaymentModes { get; } = new() { "Cash", "SBI Bank (UPI/NetBanking)" };

    [ObservableProperty]
    private string _statusMessage = "Double-Entry Ledger & Cashbook Ready.";

    [ObservableProperty]
    private bool _isLoading;

    public DaybookViewModel(AppDbContext context)
    {
        _context = context;
    }

    [RelayCommand]
    public async Task LoadDaybookAsync()
    {
        IsLoading = true;
        try
        {
            var entries = await _context.LedgerEntries
                .Include(e => e.Account)
                .OrderByDescending(e => e.TransactionDate)
                .ThenByDescending(e => e.Id)
                .ToListAsync();

            Vouchers.Clear();
            decimal sumDebits = 0;
            decimal sumCredits = 0;

            foreach (var e in entries)
            {
                decimal deb = e.EntryType == LedgerEntryType.Debit ? e.Amount : 0;
                decimal cred = e.EntryType == LedgerEntryType.Credit ? e.Amount : 0;
                sumDebits += deb;
                sumCredits += cred;

                Vouchers.Add(new DaybookVoucherItem
                {
                    Id = e.Id,
                    VoucherNumber = e.VoucherNumber,
                    TransactionDate = e.TransactionDate,
                    AccountName = e.Account?.Name ?? "Unknown Account",
                    AccountCode = e.Account?.AccountCode ?? "",
                    EntryType = e.EntryType.ToString(),
                    DebitAmount = deb,
                    CreditAmount = cred,
                    Narration = e.Narration,
                    ReferenceType = e.ReferenceType
                });
            }

            TotalDebits = sumDebits;
            TotalCredits = sumCredits;
            IsBalanced = (sumDebits == sumCredits);

            // Load Accounts Summary
            var accs = await _context.Accounts.OrderBy(a => a.AccountCode).ToListAsync();
            AccountsSummary.Clear();
            foreach (var a in accs)
            {
                AccountsSummary.Add(new AccountSummaryItem
                {
                    Code = a.AccountCode,
                    Name = a.Name,
                    Type = a.Type.ToString(),
                    CurrentBalance = a.CurrentBalance
                });
            }

            StatusMessage = $"Daybook loaded: {Vouchers.Count} journal entries. Ledger Balanced: {IsBalanced}.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RecordExpenseAsync()
    {
        if (ExpenseAmount <= 0)
        {
            StatusMessage = "[ERROR] Please enter a valid expense amount.";
            return;
        }

        if (string.IsNullOrWhiteSpace(ExpensePaidTo))
        {
            StatusMessage = "[ERROR] Please specify payee / vendor name.";
            return;
        }

        IsLoading = true;
        try
        {
            using var txn = await _context.Database.BeginTransactionAsync();

            string expVoucherNo = $"EXP-{DateTime.UtcNow:yyyyMMdd}-{DateTime.UtcNow.Ticks % 10000:D4}";

            // Find matching expense account
            string catPrefix = ExpenseCategory.Split(' ')[0];
            var expAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Name.Contains(catPrefix))
                             ?? await _context.Accounts.FirstOrDefaultAsync(a => a.Type == AccountType.Expense);

            var tenderAccount = ExpensePaymentMode.Contains("Cash")
                ? await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == "1001") // Cash in Hand
                : await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == "1002"); // SBI Current Account

            if (expAccount == null || tenderAccount == null)
            {
                StatusMessage = "[ERROR] Chart of accounts configuration missing for expense.";
                return;
            }

            // Record Expense Entity
            var expense = new Expense
            {
                Category = ExpenseCategory,
                Amount = ExpenseAmount,
                ExpenseDate = DateTime.UtcNow,
                PaidTo = ExpensePaidTo,
                ReferenceNumber = expVoucherNo,
                Description = string.IsNullOrWhiteSpace(ExpenseDescription) ? ExpenseCategory : ExpenseDescription,
                PaymentMode = ExpensePaymentMode.Contains("Cash") ? PaymentMode.Cash : PaymentMode.UPI
            };
            _context.Expenses.Add(expense);

            // Double Entry:
            // DEBIT: Expense Account (Increases Expense)
            _context.LedgerEntries.Add(new LedgerEntry
            {
                AccountId = expAccount.Id,
                VoucherNumber = expVoucherNo,
                TransactionDate = DateTime.UtcNow,
                EntryType = LedgerEntryType.Debit,
                Amount = ExpenseAmount,
                Narration = $"Paid {ExpenseCategory} to {ExpensePaidTo} ({ExpenseDescription})",
                ReferenceId = expense.Id,
                ReferenceType = "Expense"
            });
            expAccount.CurrentBalance += ExpenseAmount;

            // CREDIT: Cash/Bank Account (Reduces Asset)
            _context.LedgerEntries.Add(new LedgerEntry
            {
                AccountId = tenderAccount.Id,
                VoucherNumber = expVoucherNo,
                TransactionDate = DateTime.UtcNow,
                EntryType = LedgerEntryType.Credit,
                Amount = ExpenseAmount,
                Narration = $"Paid {ExpenseCategory} via {ExpensePaymentMode}",
                ReferenceId = expense.Id,
                ReferenceType = "Expense"
            });
            tenderAccount.CurrentBalance -= ExpenseAmount;

            await _context.SaveChangesAsync();
            await txn.CommitAsync();

            StatusMessage = $"[SUCCESS] Expense #{expVoucherNo} for ₹{ExpenseAmount:N2} recorded and balanced in Ledger!";

            // Reset Form & Reload
            ExpenseAmount = 0;
            ExpensePaidTo = string.Empty;
            ExpenseDescription = string.Empty;
            await LoadDaybookAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"[ERROR] Failed to record expense: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
