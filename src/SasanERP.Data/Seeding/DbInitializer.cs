using Microsoft.EntityFrameworkCore;
using SasanERP.Core.Enums;
using SasanERP.Core.Models;
using SasanERP.Data.Context;

namespace SasanERP.Data.Seeding;

/// <summary>
/// Database Migrator & Seed Data Initializer for Sasan The Gadget Lounge
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        // 1. Seed Company Profile
        if (!await context.CompanyProfiles.AnyAsync())
        {
            context.CompanyProfiles.Add(new CompanyProfile
            {
                BusinessName = "Sasan The Gadget Lounge",
                Tagline = "Premium New & Pre-Owned Gadgets, Laptops & Accessories",
                AddressLine1 = "106/7, Karthik Mahall, Subban St",
                AddressLine2 = "Opposite Theni, Suppan Ragavan Colony, NRT Nagar",
                City = "Theni",
                State = "Tamil Nadu",
                Pincode = "625531",
                StateCode = "33",
                PhoneNumber = "9080803070",
                Email = "sasanthegadgetlounge@gmail.com",
                GSTIN = "33AAECS4821N1ZH",
                BankName = "State Bank of India",
                BankAccountNumber = "38920192831",
                BankIFSC = "SBIN0000932",
                BankBranch = "Theni NRT Nagar",
                UpiId = "9080803070@sbi"
            });
            await context.SaveChangesAsync();
        }

        // 2. Seed Standard Chart of Accounts for Indian Retail & Margin Scheme ERP
        if (!await context.Accounts.AnyAsync())
        {
            var accounts = new List<Account>
            {
                new() { AccountCode = "1001", Name = "Cash in Hand", Type = AccountType.Asset, CurrentBalance = 50000.00m },
                new() { AccountCode = "1002", Name = "SBI Current Account (UPI/Card)", Type = AccountType.Asset, CurrentBalance = 150000.00m },
                new() { AccountCode = "1003", Name = "Customer Debtors / Receivables", Type = AccountType.Asset, CurrentBalance = 0m },
                new() { AccountCode = "1004", Name = "Closing Stock / Inventory", Type = AccountType.Asset, CurrentBalance = 850000.00m },
                
                new() { AccountCode = "2001", Name = "Supplier Creditors / Payables", Type = AccountType.Liability, CurrentBalance = 0m },
                new() { AccountCode = "2002", Name = "Output CGST Payable (Standard)", Type = AccountType.Liability, CurrentBalance = 0m },
                new() { AccountCode = "2003", Name = "Output SGST Payable (Standard)", Type = AccountType.Liability, CurrentBalance = 0m },
                new() { AccountCode = "2004", Name = "Rule 32(5) Margin Scheme GST Payable", Type = AccountType.Liability, CurrentBalance = 0m },
                
                new() { AccountCode = "3001", Name = "Proprietor Capital", Type = AccountType.Equity, CurrentBalance = 1050000.00m },
                
                new() { AccountCode = "4001", Name = "Revenue - Standard Gadget Sales", Type = AccountType.Revenue, CurrentBalance = 0m },
                new() { AccountCode = "4002", Name = "Revenue - Pre-Owned Margin Sales (Rule 32(5))", Type = AccountType.Revenue, CurrentBalance = 0m },
                new() { AccountCode = "4003", Name = "Revenue - Accessories & Toys Sales", Type = AccountType.Revenue, CurrentBalance = 0m },
                
                new() { AccountCode = "5001", Name = "Cost of Goods Sold (COGS)", Type = AccountType.Expense, CurrentBalance = 0m },
                new() { AccountCode = "5002", Name = "Import Customs Duty & Clearance", Type = AccountType.Expense, CurrentBalance = 0m },
                new() { AccountCode = "5003", Name = "Courier & Air Freight", Type = AccountType.Expense, CurrentBalance = 0m },
                new() { AccountCode = "5004", Name = "Showroom Rent (Karthik Mahall)", Type = AccountType.Expense, CurrentBalance = 0m },
                new() { AccountCode = "5005", Name = "Electricity & Utilities", Type = AccountType.Expense, CurrentBalance = 0m }
            };

            await context.Accounts.AddRangeAsync(accounts);
            await context.SaveChangesAsync();
        }

        // 3. Seed Sample Customer & Supplier
        Customer? defaultCustomer = null;
        if (!await context.Customers.AnyAsync())
        {
            defaultCustomer = new Customer
            {
                FullName = "Walk-in Retail Customer",
                PhoneNumber = "9080803070",
                City = "Theni",
                State = "Tamil Nadu",
                StateCode = "33"
            };
            var regularCustomer = new Customer
            {
                FullName = "Karthik Raja S",
                PhoneNumber = "9842109876",
                Email = "karthik.theni@gmail.com",
                Address = "Suppan Ragavan Colony, Theni",
                City = "Theni",
                State = "Tamil Nadu",
                StateCode = "33"
            };
            await context.Customers.AddRangeAsync(defaultCustomer, regularCustomer);
            await context.SaveChangesAsync();
        }
        else
        {
            defaultCustomer = await context.Customers.FirstAsync();
        }

        Supplier? defaultSupplier = null;
        if (!await context.Suppliers.AnyAsync())
        {
            defaultSupplier = new Supplier
            {
                Name = "Apex Gulf Electronics Imports",
                ContactPerson = "Mohammed Farooq",
                PhoneNumber = "9884012345",
                Email = "farooq.apex@gmail.com",
                Address = "Deira Wholesale Market, Dubai / Parrys, Chennai",
                IsImporter = true,
                CountryOfOrigin = "Dubai (UAE)"
            };
            await context.Suppliers.AddAsync(defaultSupplier);
            await context.SaveChangesAsync();
        }
        else
        {
            defaultSupplier = await context.Suppliers.FirstAsync();
        }

        // 4. Seed Products
        if (!await context.Products.AnyAsync())
        {
            var p1 = new Product
            {
                Name = "Apple iPhone 15 Pro",
                Brand = "Apple",
                Category = DeviceType.Smartphone,
                SKU = "IP15P-128-NAT",
                Barcode = "195949038210",
                HSNCode = "8517",
                IsSerialized = true,
                DefaultCostPrice = 75000.00m,
                MRP = 134900.00m,
                SellingPrice = 84999.00m,
                GSTPercentage = 18.0m,
                CurrentStock = 2,
                ReorderLevel = 1
            };

            var p2 = new Product
            {
                Name = "Apple MacBook Air M2 (13.6-inch)",
                Brand = "Apple",
                Category = DeviceType.Laptop,
                SKU = "MBA-M2-256-SLV",
                Barcode = "194253134983",
                HSNCode = "8471",
                IsSerialized = true,
                DefaultCostPrice = 58000.00m,
                MRP = 99900.00m,
                SellingPrice = 67999.00m,
                GSTPercentage = 18.0m,
                CurrentStock = 1,
                ReorderLevel = 1
            };

            var p3 = new Product
            {
                Name = "DJI Mini 4 Pro Drone with RC2",
                Brand = "DJI",
                Category = DeviceType.Drone,
                SKU = "DJI-M4P-RC2",
                Barcode = "6941565969513",
                HSNCode = "8806",
                IsSerialized = true,
                DefaultCostPrice = 62000.00m,
                MRP = 94900.00m,
                SellingPrice = 76500.00m,
                GSTPercentage = 18.0m,
                CurrentStock = 1,
                ReorderLevel = 1
            };

            var p4 = new Product
            {
                Name = "Anker 737 Power Bank (PowerCore 24K)",
                Brand = "Anker",
                Category = DeviceType.PowerBank,
                SKU = "ANK-737-24K",
                Barcode = "194644098934",
                HSNCode = "8504",
                IsSerialized = false, // Bulk item
                DefaultCostPrice = 7200.00m,
                MRP = 14999.00m,
                SellingPrice = 9999.00m,
                GSTPercentage = 18.0m,
                CurrentStock = 8,
                ReorderLevel = 2
            };

            var p5 = new Product
            {
                Name = "Apple 20W USB-C Fast Power Adapter",
                Brand = "Apple",
                Category = DeviceType.Charger,
                SKU = "APL-20W-ADP",
                Barcode = "194252157006",
                HSNCode = "8504",
                IsSerialized = false, // Bulk item
                DefaultCostPrice = 1100.00m,
                MRP = 1900.00m,
                SellingPrice = 1650.00m,
                GSTPercentage = 18.0m,
                CurrentStock = 15,
                ReorderLevel = 5
            };

            await context.Products.AddRangeAsync(p1, p2, p3, p4, p5);
            await context.SaveChangesAsync();

            // 5. Seed Serialized Units (Mobiles & Laptops with IMEI & Battery Health)
            var unit1 = new SerializedUnit
            {
                ProductId = p1.Id,
                IMEI1 = "354892019482103",
                IMEI2 = "354892019482111",
                SerialNumber = "F2LXC019M92D",
                Color = "Natural Titanium",
                StorageCapacity = "128GB",
                BatteryHealthPercentage = 98,
                ConditionGrade = ConditionGrade.GradeAPlus_Flawless,
                WarrantyStatus = WarrantyStatus.ShopWarranty_90Days,
                PurchaseSource = PurchaseSource.DirectImport,
                PurchaseCost = 72000.00m,
                TaggedPrice = 82999.00m,
                Status = UnitStatus.InStock,
                SupplierId = defaultSupplier?.Id,
                PurchaseDate = DateTime.UtcNow.AddDays(-14)
            };

            var unit2 = new SerializedUnit
            {
                ProductId = p1.Id,
                IMEI1 = "359182048591823",
                IMEI2 = "359182048591831",
                SerialNumber = "K7WQP918N01E",
                Color = "Blue Titanium",
                StorageCapacity = "128GB",
                BatteryHealthPercentage = 89,
                ConditionGrade = ConditionGrade.GradeA_MinorWear,
                WarrantyStatus = WarrantyStatus.ShopWarranty_30Days,
                PurchaseSource = PurchaseSource.CustomerBuyback,
                PurchaseCost = 65000.00m,
                TaggedPrice = 76999.00m,
                Status = UnitStatus.InStock,
                BuybackCustomerId = defaultCustomer?.Id,
                CustomerAadhaarDocPath = "documents/buyback/aadhaar_karthik_raja.jpg",
                PurchaseDate = DateTime.UtcNow.AddDays(-5)
            };

            var unit3 = new SerializedUnit
            {
                ProductId = p2.Id,
                IMEI1 = "C02G901XMD6R", // Serial used as primary identifier for MacBooks
                SerialNumber = "C02G901XMD6R",
                Color = "Space Gray",
                StorageCapacity = "256GB SSD / 8GB RAM",
                BatteryHealthPercentage = 95,
                ConditionGrade = ConditionGrade.GradeAPlus_Flawless,
                WarrantyStatus = WarrantyStatus.ShopWarranty_90Days,
                PurchaseSource = PurchaseSource.DirectImport,
                PurchaseCost = 55000.00m,
                TaggedPrice = 64999.00m,
                Status = UnitStatus.InStock,
                SupplierId = defaultSupplier?.Id,
                PurchaseDate = DateTime.UtcNow.AddDays(-22)
            };

            var unit4 = new SerializedUnit
            {
                ProductId = p3.Id,
                IMEI1 = "1581F4H7C230004", // Drone Flight Controller Serial
                SerialNumber = "1581F4H7C230004",
                Color = "Light Gray",
                StorageCapacity = "Fly More Combo",
                BatteryHealthPercentage = 100,
                ConditionGrade = ConditionGrade.BrandNew,
                WarrantyStatus = WarrantyStatus.BrandWarrantyRemaining,
                PurchaseSource = PurchaseSource.BrandDistributor,
                PurchaseCost = 62000.00m,
                TaggedPrice = 76500.00m,
                Status = UnitStatus.InStock,
                SupplierId = defaultSupplier?.Id,
                PurchaseDate = DateTime.UtcNow.AddDays(-10)
            };

            await context.SerializedUnits.AddRangeAsync(unit1, unit2, unit3, unit4);
            await context.SaveChangesAsync();
        }
    }
}
