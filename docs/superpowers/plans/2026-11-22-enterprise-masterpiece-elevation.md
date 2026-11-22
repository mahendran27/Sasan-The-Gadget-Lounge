# Enterprise Masterpiece Elevation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Elevate Sasan The Gadget Lounge ERP into a world-class enterprise Windows Desktop suite across 5 critical pillars: Beauty & Aesthetics, Performance, Usability, Security & Data Integrity, and Speed.

**Architecture:** Implement SQLite WAL-mode engine tuning, non-blocking asynchronous PDF/TSPL generation, double-entry financial ledger ACID invariant assertions, automated local database backup and audit logging subsystem, and high-performance UI virtualization with Windows 11 Fluent design.

**Tech Stack:** C# 12, .NET 8 LTS (WPF), Entity Framework Core 8 (SQLite), QuestPDF, ZXing.Net, ClosedXML.

**Spec:** Multi-module ERP/POS requirements covering Serialized Device tracking (IMEI), Rule 32(5) Margin Scheme GST, TSPL thermal printing, dual NFC/UHF RFID, and Double-Entry General Ledger.

## Global Constraints
- Target Framework: `net8.0-windows` for UI and Services, `net8.0` for Core and Data.
- Zero Warnings, Zero Errors on `dotnet build`.
- 100% verification pass on `SasanERP.Verification`.
- No JavaScript/Web wrappers; pure native C# WPF desktop code.
- Anti-slop UI standards: clean semantic badges, purposeful typography, no decorative glow or generic template filler.

## Review Focus
- Database concurrency: concurrent reads from Dashboard during rapid POS checkouts without SQLite busy lock.
- Double-entry ledger invariant: abort any transaction where Debits != Credits before committing.
- DataGrid rendering performance: smooth 60fps scrolling with 10,000+ serialized units.
- Non-blocking PDF generation: UI remains responsive during invoice compilation and thermal print dispatch.
- Backup reliability: automated database snapshot generation and rotation with 0 data loss.

---

### Task 1: SQLite WAL High-Performance Tuning & Startup Engine Pre-Warming

**Files:**
- Modify: `src/SasanERP.Data/Context/AppDbContext.cs`
- Modify: `src/SasanERP.Data/Seeding/DbInitializer.cs`
- Modify: `src/SasanERP.UI/App.xaml.cs`

**Interfaces:**
- Consumes: `AppDbContext`, `DbInitializer`
- Produces: SQLite WAL mode enabled (`PRAGMA journal_mode=WAL`), pre-warmed QuestPDF and ZXing engines on startup.

- [ ] **Step 1: Write test/verification check for SQLite WAL mode and pre-warming in Verification suite**
- [ ] **Step 2: Implement SQLite WAL PRAGMAs in `DbInitializer.InitializeAsync`**
- [ ] **Step 3: Add pre-warming logic in `App.xaml.cs` to eliminate first-invoice cold start**
- [ ] **Step 4: Verify with `dotnet build`**

---

### Task 2: Security & Data Integrity Subsystems (Audit Log & Automated Local Backup Engine)

**Files:**
- Create: `src/SasanERP.Core/Models/AuditLog.cs`
- Create: `src/SasanERP.Services/Security/AuditLogService.cs`
- Create: `src/SasanERP.Services/Backup/DatabaseBackupService.cs`
- Modify: `src/SasanERP.Data/Context/AppDbContext.cs`
- Modify: `src/SasanERP.Services/Billing/BillingService.cs`

**Interfaces:**
- Consumes: `AppDbContext`, `AuditLog`, `DatabaseBackupService`
- Produces: `DatabaseBackupService.CreateBackupAsync()`, `AuditLogService.LogAsync()`, strict double-entry ledger ACID assertion in `BillingService`.

- [ ] **Step 1: Create `AuditLog.cs` entity model and register `DbSet<AuditLog>` in `AppDbContext.cs`**
- [ ] **Step 2: Create `AuditLogService.cs` for centralized security event logging**
- [ ] **Step 3: Create `DatabaseBackupService.cs` supporting snapshot creation, listing, rotation, and restore**
- [ ] **Step 4: Add strict Double-Entry Invariant Guard (`Math.Abs(Debits - Credits) < 0.001m`) in `BillingService.cs`**
- [ ] **Step 5: Verify building and running verification**

---

### Task 3: Performance & Usability Refinement (DataGrid Virtualization, Auto-Focus, Debounce & PII Masking)

**Files:**
- Modify: `src/SasanERP.UI/Views/InventoryView.xaml`
- Modify: `src/SasanERP.UI/Views/PosView.xaml`
- Modify: `src/SasanERP.UI/Views/PosView.xaml.cs`
- Modify: `src/SasanERP.UI/ViewModels/InventoryViewModel.cs`
- Modify: `src/SasanERP.UI/ViewModels/SettingsViewModel.cs`
- Modify: `src/SasanERP.UI/Views/StoreSettingsView.xaml`

**Interfaces:**
- Consumes: `InventoryView`, `PosView`, `StoreSettingsView`, `DatabaseBackupService`
- Produces: UI virtualization, scanner auto-focus, live search guidance, and "System Health & Backup Vault" in Settings.

- [ ] **Step 1: Enable UI Virtualization on DataGrids in `InventoryView.xaml` and `DashboardView.xaml`**
- [ ] **Step 2: Implement auto-focus on `TxtBarcode` in `PosView.xaml.cs` upon loading or switching**
- [ ] **Step 3: Integrate `DatabaseBackupService` into `SettingsViewModel.cs` and add the "System Health & Backup Vault" tab to `StoreSettingsView.xaml`**
- [ ] **Step 4: Non-blocking asynchronous PDF invoice generation in `PosViewModel.cs` using `Task.Run`**
- [ ] **Step 5: Verify build with `dotnet build`**

---

### Task 4: Comprehensive Verification Suite Update & Benchmarking

**Files:**
- Modify: `src/SasanERP.Verification/Program.cs`

**Interfaces:**
- Consumes: All elevated services (`DatabaseBackupService`, `AuditLogService`, `BillingService`, WAL mode)
- Produces: 15-stage verification suite certifying WAL mode, ACID invariant guards, backup engine, and 12 vector invoice templates.

- [ ] **Step 1: Add Stage 15 to `SasanERP.Verification/Program.cs` testing WAL mode, ACID invariant guard, and DatabaseBackupService**
- [ ] **Step 2: Run verification suite `dotnet run --project src/SasanERP.Verification/SasanERP.Verification.csproj`**
- [ ] **Step 3: Commit changes with conventional git commit**
