using Microsoft.EntityFrameworkCore;
using SasanERP.Core.Enums;
using SasanERP.Core.Models;
using SasanERP.Core.Specifications;
using SasanERP.Data.Context;

namespace SasanERP.Services.Billing;

/// <summary>
/// Billing and Financial Transaction Service.
/// Handles invoice creation, stock state transition to SOLD, and atomic double-entry ledger postings.
/// </summary>
public class BillingService
{
    private readonly AppDbContext _context;

    public BillingService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Generates next sequential invoice number (e.g. SGL/24-25/0001)
    /// </summary>
    public async Task<string> GenerateNextInvoiceNumberAsync()
    {
        var company = await _context.CompanyProfiles.FirstOrDefaultAsync() ?? new CompanyProfile();
        int currentYear = DateTime.UtcNow.Year;
        int nextYearShort = (currentYear + 1) % 100;
        string financialYear = $"{currentYear % 100:00}-{nextYearShort:00}";

        int totalInvoices = await _context.Invoices.CountAsync();
        int nextSequence = totalInvoices + 1;

        return $"{company.InvoicePrefix}/{financialYear}/{nextSequence:D4}";
    }

    /// <summary>
    /// Completes the checkout transaction: saves invoice, marks serialized units as SOLD,
    /// decrements bulk inventory, and writes balanced double-entry accounting ledger vouchers.
    /// </summary>
    public async Task<Invoice> ProcessCheckoutAsync(
        Invoice invoice, 
        List<PaymentTransaction> payments, 
        string billedBy = "Admin")
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            invoice.InvoiceDate = DateTime.UtcNow;
            invoice.BilledBy = billedBy;

            // 1. Process Line Items and Inventory Transitions
            foreach (var item in invoice.Items)
            {
                if (item.SerializedUnitId.HasValue)
                {
                    var unit = await _context.SerializedUnits
                        .FirstOrDefaultAsync(u => u.Id == item.SerializedUnitId.Value);

                    if (unit != null)
                    {
                        if (unit.Status != UnitStatus.InStock)
                        {
                            throw new InvalidOperationException(
                                $"Device IMEI {unit.IMEI1} is no longer available in stock (Current Status: {unit.Status}).");
                        }

                        // Lock status to SOLD upon checkout
                        unit.Status = UnitStatus.Sold;
                        unit.SoldDate = DateTime.UtcNow;
                        unit.ActualSoldPrice = item.LineTotal;
                        unit.SoldInvoice = invoice;

                        // Decrement Product total stock count
                        var parentProduct = await _context.Products.FindAsync(unit.ProductId);
                        if (parentProduct != null && parentProduct.CurrentStock > 0)
                        {
                            parentProduct.CurrentStock--;
                        }
                    }
                }
                else
                {
                    // Non-serialized item (e.g. charger, power bank, toy)
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        if (product.CurrentStock < item.Quantity)
                        {
                            throw new InvalidOperationException(
                                $"Insufficient stock for {product.Name}. Available: {product.CurrentStock}, Requested: {item.Quantity}.");
                        }
                        product.CurrentStock -= (int)item.Quantity;
                    }
                }
            }

            // 2. Attach Payments
            decimal totalPaid = 0;
            foreach (var payment in payments)
            {
                payment.Invoice = invoice;
                payment.PaymentDate = DateTime.UtcNow;
                invoice.Payments.Add(payment);
                totalPaid += payment.Amount;
            }

            invoice.AmountPaid = totalPaid;
            invoice.BalanceDue = Math.Max(0, invoice.GrandTotal - totalPaid);

            // If customer has unpaid balance, record to their credit ledger
            if (invoice.BalanceDue > 0)
            {
                var customer = await _context.Customers.FindAsync(invoice.CustomerId);
                if (customer != null)
                {
                    customer.OutstandingCredit += invoice.BalanceDue;
                }
            }

            // 3. Save Invoice to Database
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // 4. Generate Balanced Double-Entry Accounting Ledger Entries
            await CreateLedgerEntriesForInvoiceAsync(invoice, payments);

            // 5. Record Security Audit Log
            _context.AuditLogs.Add(new AuditLog
            {
                TimestampUtc = DateTime.UtcNow,
                Action = "InvoiceCheckout",
                EntityType = "Invoice",
                EntityId = invoice.InvoiceNumber,
                PerformedBy = billedBy,
                Details = $"Billed Inv #{invoice.InvoiceNumber}: Total ₹{invoice.GrandTotal:N2} via {payments.Count} payment tenders.",
                MachineName = Environment.MachineName
            });

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return invoice;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Generates strict double-entry debits and credits for standard & Rule 32(5) Margin Scheme sales
    /// </summary>
    private async Task CreateLedgerEntriesForInvoiceAsync(Invoice invoice, List<PaymentTransaction> payments)
    {
        string voucherNo = $"VCH-{invoice.InvoiceNumber}";
        var ledgerEntries = new List<LedgerEntry>();

        // Find standard chart of accounts
        var cashAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == "1001");
        var bankAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == "1002");
        var debtorAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == "1003");
        var exchangeAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == "1005") ?? cashAcc;
        var stdSalesAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == "4001");
        var marginSalesAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == "4002");
        var cgstAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == "2002");
        var sgstAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == "2003");
        var marginGstAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == "2004");
        var roundOffAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == "4004");
        var discountAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == "5006");

        // DEBITS: Payment tenders received
        foreach (var p in payments)
        {
            Account? targetAccount = p.Mode switch
            {
                PaymentMode.Cash => cashAcc,
                PaymentMode.UPI => bankAcc,
                PaymentMode.CreditDebitCard => bankAcc,
                PaymentMode.BajajFinservEMI => bankAcc,
                PaymentMode.HdbFinanceEMI => bankAcc,
                PaymentMode.PineLabsEMI => bankAcc,
                PaymentMode.OldPhoneExchange => exchangeAcc,
                _ => cashAcc
            };

            if (targetAccount != null && p.Amount > 0)
            {
                ledgerEntries.Add(new LedgerEntry
                {
                    AccountId = targetAccount.Id,
                    VoucherNumber = voucherNo,
                    TransactionDate = invoice.InvoiceDate,
                    EntryType = LedgerEntryType.Debit,
                    Amount = p.Amount,
                    Narration = $"Receipt via {p.Mode} for Inv #{invoice.InvoiceNumber}",
                    ReferenceId = invoice.Id,
                    ReferenceType = "Invoice"
                });
                targetAccount.CurrentBalance += p.Amount;
            }
        }

        // DEBIT: Unpaid balance to Customer Debtors account
        if (invoice.BalanceDue > 0 && debtorAcc != null)
        {
            ledgerEntries.Add(new LedgerEntry
            {
                AccountId = debtorAcc.Id,
                VoucherNumber = voucherNo,
                TransactionDate = invoice.InvoiceDate,
                EntryType = LedgerEntryType.Debit,
                Amount = invoice.BalanceDue,
                Narration = $"Customer credit balance on Inv #{invoice.InvoiceNumber}",
                ReferenceId = invoice.Id,
                ReferenceType = "Invoice"
            });
            debtorAcc.CurrentBalance += invoice.BalanceDue;
        }

        // DEBIT: Discount Allowed on Sales
        if (invoice.InvoiceDiscount > 0 && discountAcc != null)
        {
            ledgerEntries.Add(new LedgerEntry
            {
                AccountId = discountAcc.Id,
                VoucherNumber = voucherNo,
                TransactionDate = invoice.InvoiceDate,
                EntryType = LedgerEntryType.Debit,
                Amount = invoice.InvoiceDiscount,
                Narration = $"Discount allowed on Inv #{invoice.InvoiceNumber}",
                ReferenceId = invoice.Id,
                ReferenceType = "Invoice"
            });
            discountAcc.CurrentBalance += invoice.InvoiceDiscount;
        }

        // CREDITS: Sales Revenues and Tax Payables
        // Standard sales revenue
        if (invoice.StandardTaxableValue > 0 && stdSalesAcc != null)
        {
            ledgerEntries.Add(new LedgerEntry
            {
                AccountId = stdSalesAcc.Id,
                VoucherNumber = voucherNo,
                TransactionDate = invoice.InvoiceDate,
                EntryType = LedgerEntryType.Credit,
                Amount = invoice.StandardTaxableValue,
                Narration = $"Standard Taxable Sales on Inv #{invoice.InvoiceNumber}",
                ReferenceId = invoice.Id,
                ReferenceType = "Invoice"
            });
            stdSalesAcc.CurrentBalance += invoice.StandardTaxableValue;
        }

        // Standard Taxes
        if (invoice.TotalCGST > 0 && cgstAcc != null)
        {
            ledgerEntries.Add(new LedgerEntry
            {
                AccountId = cgstAcc.Id,
                VoucherNumber = voucherNo,
                TransactionDate = invoice.InvoiceDate,
                EntryType = LedgerEntryType.Credit,
                Amount = invoice.TotalCGST,
                Narration = $"Output CGST collected on Inv #{invoice.InvoiceNumber}",
                ReferenceId = invoice.Id,
                ReferenceType = "Invoice"
            });
            cgstAcc.CurrentBalance += invoice.TotalCGST;
        }

        if (invoice.TotalSGST > 0 && sgstAcc != null)
        {
            ledgerEntries.Add(new LedgerEntry
            {
                AccountId = sgstAcc.Id,
                VoucherNumber = voucherNo,
                TransactionDate = invoice.InvoiceDate,
                EntryType = LedgerEntryType.Credit,
                Amount = invoice.TotalSGST,
                Narration = $"Output SGST collected on Inv #{invoice.InvoiceNumber}",
                ReferenceId = invoice.Id,
                ReferenceType = "Invoice"
            });
            sgstAcc.CurrentBalance += invoice.TotalSGST;
        }

        // Margin Scheme revenue & tax (Rule 32(5))
        decimal marginRevenueTotal = invoice.Items
            .Where(i => i.IsMarginScheme)
            .Sum(i => i.LineTotal);

        if (marginRevenueTotal > 0 && marginSalesAcc != null)
        {
            ledgerEntries.Add(new LedgerEntry
            {
                AccountId = marginSalesAcc.Id,
                VoucherNumber = voucherNo,
                TransactionDate = invoice.InvoiceDate,
                EntryType = LedgerEntryType.Credit,
                Amount = marginRevenueTotal - invoice.TotalMarginTax,
                Narration = $"Margin Scheme Sales (Net) under Rule 32(5) on Inv #{invoice.InvoiceNumber}",
                ReferenceId = invoice.Id,
                ReferenceType = "Invoice"
            });
            marginSalesAcc.CurrentBalance += (marginRevenueTotal - invoice.TotalMarginTax);

            if (invoice.TotalMarginTax > 0 && marginGstAcc != null)
            {
                ledgerEntries.Add(new LedgerEntry
                {
                    AccountId = marginGstAcc.Id,
                    VoucherNumber = voucherNo,
                    TransactionDate = invoice.InvoiceDate,
                    EntryType = LedgerEntryType.Credit,
                    Amount = invoice.TotalMarginTax,
                    Narration = $"Rule 32(5) Margin GST Tax Liability on Inv #{invoice.InvoiceNumber}",
                    ReferenceId = invoice.Id,
                    ReferenceType = "Invoice"
                });
                marginGstAcc.CurrentBalance += invoice.TotalMarginTax;
            }
        }

        // Round Off Balancing Entry
        if (invoice.RoundOff != 0 && roundOffAcc != null)
        {
            if (invoice.RoundOff > 0)
            {
                ledgerEntries.Add(new LedgerEntry
                {
                    AccountId = roundOffAcc.Id,
                    VoucherNumber = voucherNo,
                    TransactionDate = invoice.InvoiceDate,
                    EntryType = LedgerEntryType.Credit,
                    Amount = invoice.RoundOff,
                    Narration = $"Round off upward rounding adjustment on Inv #{invoice.InvoiceNumber}",
                    ReferenceId = invoice.Id,
                    ReferenceType = "Invoice"
                });
                roundOffAcc.CurrentBalance += invoice.RoundOff;
            }
            else
            {
                decimal absRound = Math.Abs(invoice.RoundOff);
                ledgerEntries.Add(new LedgerEntry
                {
                    AccountId = roundOffAcc.Id,
                    VoucherNumber = voucherNo,
                    TransactionDate = invoice.InvoiceDate,
                    EntryType = LedgerEntryType.Debit,
                    Amount = absRound,
                    Narration = $"Round off downward rounding adjustment on Inv #{invoice.InvoiceNumber}",
                    ReferenceId = invoice.Id,
                    ReferenceType = "Invoice"
                });
                roundOffAcc.CurrentBalance -= absRound;
            }
        }

        // STRICT DOUBLE-ENTRY INVARIANT GUARD (Banking & CA Ledger Protection)
        decimal sumDebits = ledgerEntries.Where(e => e.EntryType == LedgerEntryType.Debit).Sum(e => e.Amount);
        decimal sumCredits = ledgerEntries.Where(e => e.EntryType == LedgerEntryType.Credit).Sum(e => e.Amount);
        if (Math.Abs(sumDebits - sumCredits) > 0.01m)
        {
            throw new InvalidOperationException(
                $"[ACID INVARIANT VIOLATION] Double-entry ledger is not balanced! Total Debits: ₹{sumDebits:N2}, Total Credits: ₹{sumCredits:N2}, Discrepancy: ₹{Math.Abs(sumDebits - sumCredits):N2}");
        }

        await _context.LedgerEntries.AddRangeAsync(ledgerEntries);
    }
}
