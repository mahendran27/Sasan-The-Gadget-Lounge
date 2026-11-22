using Microsoft.EntityFrameworkCore;
using SasanERP.Core.Models;

namespace SasanERP.Data.Context;

/// <summary>
/// EF Core 8 Database Context for Sasan The Gadget Lounge ERP & POS
/// Supports SQLite (single-terminal offline) & PostgreSQL/SQL Server (multi-terminal LAN sync)
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<CompanyProfile> CompanyProfiles => Set<CompanyProfile>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<SerializedUnit> SerializedUnits => Set<SerializedUnit>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<Expense> Expenses => Set<Expense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ================= PRODUCT CONFIGURATION =================
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Brand).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SKU).IsRequired().HasMaxLength(60);
            entity.Property(e => e.Barcode).HasMaxLength(60);
            entity.Property(e => e.HSNCode).IsRequired().HasMaxLength(20);

            entity.HasIndex(e => e.SKU).IsUnique();
            entity.HasIndex(e => e.Barcode);

            entity.Property(e => e.DefaultCostPrice).HasPrecision(18, 2);
            entity.Property(e => e.MRP).HasPrecision(18, 2);
            entity.Property(e => e.SellingPrice).HasPrecision(18, 2);
            entity.Property(e => e.GSTPercentage).HasPrecision(5, 2);
        });

        // ================= SERIALIZED UNIT CONFIGURATION =================
        modelBuilder.Entity<SerializedUnit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IMEI1).IsRequired().HasMaxLength(20);
            entity.Property(e => e.IMEI2).HasMaxLength(20);
            entity.Property(e => e.SerialNumber).IsRequired().HasMaxLength(60);
            entity.Property(e => e.Color).HasMaxLength(50);
            entity.Property(e => e.StorageCapacity).HasMaxLength(50);

            entity.HasIndex(e => e.IMEI1);
            entity.HasIndex(e => e.IMEI2);
            entity.HasIndex(e => e.SerialNumber);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.PurchaseCost).HasPrecision(18, 2);
            entity.Property(e => e.TaggedPrice).HasPrecision(18, 2);
            entity.Property(e => e.ActualSoldPrice).HasPrecision(18, 2);

            entity.HasOne(e => e.Product)
                  .WithMany(p => p.SerializedUnits)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Supplier)
                  .WithMany(s => s.SuppliedUnits)
                  .HasForeignKey(e => e.SupplierId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.BuybackCustomer)
                  .WithMany(c => c.DevicesSoldToShop)
                  .HasForeignKey(e => e.BuybackCustomerId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.SoldInvoice)
                  .WithMany(i => i.SoldDevices)
                  .HasForeignKey(e => e.SoldInvoiceId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ================= CUSTOMER CONFIGURATION =================
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.GSTIN).HasMaxLength(20);
            entity.Property(e => e.AadhaarNumber).HasMaxLength(20);
            entity.Property(e => e.OutstandingCredit).HasPrecision(18, 2);

            entity.HasIndex(e => e.PhoneNumber);
        });

        // ================= SUPPLIER CONFIGURATION =================
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.GSTIN).HasMaxLength(20);
            entity.Property(e => e.OutstandingPayable).HasPrecision(18, 2);
        });

        // ================= INVOICE CONFIGURATION =================
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.HasIndex(e => e.InvoiceDate);

            entity.Property(e => e.GrossTotal).HasPrecision(18, 2);
            entity.Property(e => e.InvoiceDiscount).HasPrecision(18, 2);
            entity.Property(e => e.StandardTaxableValue).HasPrecision(18, 2);
            entity.Property(e => e.TotalCGST).HasPrecision(18, 2);
            entity.Property(e => e.TotalSGST).HasPrecision(18, 2);
            entity.Property(e => e.TotalIGST).HasPrecision(18, 2);
            entity.Property(e => e.TotalMarginAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalMarginTax).HasPrecision(18, 2);
            entity.Property(e => e.GrandTotal).HasPrecision(18, 2);
            entity.Property(e => e.RoundOff).HasPrecision(18, 2);
            entity.Property(e => e.AmountPaid).HasPrecision(18, 2);
            entity.Property(e => e.BalanceDue).HasPrecision(18, 2);

            entity.HasOne(e => e.Customer)
                  .WithMany(c => c.Invoices)
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ================= INVOICE ITEM CONFIGURATION =================
        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ItemDescription).IsRequired().HasMaxLength(250);
            entity.Property(e => e.HSNCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.IMEI1).HasMaxLength(20);
            entity.Property(e => e.IMEI2).HasMaxLength(20);
            entity.Property(e => e.SerialNumber).HasMaxLength(60);

            entity.Property(e => e.Quantity).HasPrecision(18, 2);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.ItemDiscount).HasPrecision(18, 2);
            entity.Property(e => e.UnitCostPrice).HasPrecision(18, 2);
            entity.Property(e => e.MarginAmount).HasPrecision(18, 2);
            entity.Property(e => e.GSTPercentage).HasPrecision(5, 2);
            entity.Property(e => e.TaxableValue).HasPrecision(18, 2);
            entity.Property(e => e.CGSTPercentage).HasPrecision(5, 2);
            entity.Property(e => e.CGSTAmount).HasPrecision(18, 2);
            entity.Property(e => e.SGSTPercentage).HasPrecision(5, 2);
            entity.Property(e => e.SGSTAmount).HasPrecision(18, 2);
            entity.Property(e => e.IGSTPercentage).HasPrecision(5, 2);
            entity.Property(e => e.IGSTAmount).HasPrecision(18, 2);
            entity.Property(e => e.LineTotal).HasPrecision(18, 2);

            entity.HasOne(e => e.Invoice)
                  .WithMany(i => i.Items)
                  .HasForeignKey(e => e.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                  .WithMany(p => p.InvoiceItems)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SerializedUnit)
                  .WithMany()
                  .HasForeignKey(e => e.SerializedUnitId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ================= PAYMENT TRANSACTION CONFIGURATION =================
        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(100);

            entity.HasOne(e => e.Invoice)
                  .WithMany(i => i.Payments)
                  .HasForeignKey(e => e.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ExchangeDevice)
                  .WithMany()
                  .HasForeignKey(e => e.ExchangeDeviceId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ================= ACCOUNT & LEDGER CONFIGURATION =================
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AccountCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.AccountCode).IsUnique();
            entity.Property(e => e.CurrentBalance).HasPrecision(18, 2);
        });

        modelBuilder.Entity<LedgerEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VoucherNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Narration).IsRequired().HasMaxLength(300);
            entity.Property(e => e.ReferenceType).HasMaxLength(50);

            entity.HasIndex(e => e.VoucherNumber);
            entity.HasIndex(e => e.TransactionDate);

            entity.HasOne(e => e.Account)
                  .WithMany(a => a.LedgerEntries)
                  .HasForeignKey(e => e.AccountId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.PaidTo).IsRequired().HasMaxLength(150);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(300);
        });
    }
}
