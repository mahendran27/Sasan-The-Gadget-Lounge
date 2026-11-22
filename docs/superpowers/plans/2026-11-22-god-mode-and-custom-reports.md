# God Mode & Period Business Intelligence Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement full administrative CRUD and editing capabilities across all ERP entities (Invoices, Customers, Serialized IMEI Units, and Products), time-range business intelligence (from any hour/date to any hour/date), and QuestPDF executive report generation (Shift Sales Z-Report, Stock Valuation Audit, Customer Khata).

**Architecture:** Create `PeriodAnalyticsService` for time-sliced business intelligence and administrative mutations (including invoice voiding with ACID reversals and stock returns), `ExecutiveReportPdfEngine` for professional vector A4 PDF reports, `GodModeViewModel` with tabbed management, and `GodModeView.xaml` Windows 11 Fluent UI.

**Tech Stack:** C# 12, .NET 8 LTS (WPF), Entity Framework Core 8 (SQLite WAL), QuestPDF, ClosedXML.

**Spec:** Omnipotent Administrative Control ("God Mode") covering custom time-range filtering, full in-place editing, invoice cancellation/voiding with stock restoration, Khata settlement, and vector PDF reports.

## Global Constraints
- Target Framework: `net8.0-windows` for UI and Services, `net8.0` for Core and Data.
- Zero Warnings, Zero Errors on `dotnet build`.
- 100% verification pass on `SasanERP.Verification`.
- Strict double-entry ledger balance preserved on invoice voiding and Khata settlements.
- Anti-slop UI standards: clean typography, clear hierarchy, high-contrast dark mode styling.

## Review Focus
- Date & time filtering: correctly handles time boundaries (e.g. from 10:00 AM to 02:00 PM on same day or cross-days).
- Invoice voiding transaction integrity: sets invoice status to Voided, restores SerializedUnit status back to `InStock`, decrements product sales count, and posts balanced reversal journal entries (`Reversal Dr./Cr.`).
- Stock mutation audit: every manual change to IMEI, purchase cost, or tagged price creates an `AuditLog` entry.
- Customer Khata settlement: receiving cash/UPI for outstanding debt debits Cash/Bank and credits Customer Debtors with balanced voucher.
- Report rendering: vector PDF generation works seamlessly with store branding, GSTIN, UPI QR, and itemized breakdown tables.

---

### Task 1: Time-Range Analytics & Administrative Services (`PeriodAnalyticsService.cs`)

**Files:**
- Create: `src/SasanERP.Services/Analytics/PeriodAnalyticsService.cs`
- Modify: `src/SasanERP.Core/Models/Invoice.cs`

**Interfaces:**
- Consumes: `AppDbContext`, `AuditLogService`
- Produces: `PeriodAnalyticsService.ComputePeriodReportAsync()`, `PeriodAnalyticsService.VoidInvoiceAsync()`, `PeriodAnalyticsService.UpdateSerializedUnitAsync()`, `PeriodAnalyticsService.UpdateCustomerAsync()`, `PeriodAnalyticsService.SettleCustomerCreditAsync()`.

- [ ] **Step 1: Add `IsVoided` and `VoidReason` properties to `Invoice.cs`**
- [ ] **Step 2: Implement `PeriodAnalyticsService.cs` with time-range aggregation and admin mutations**
- [ ] **Step 3: Verify build with `dotnet build`**

---

### Task 2: Vector Executive Reporting Engine (`ExecutiveReportPdfEngine.cs`)

**Files:**
- Create: `src/SasanERP.Services/Printing/ExecutiveReportPdfEngine.cs`

**Interfaces:**
- Consumes: `PeriodAnalyticsService`, `CompanyProfile`, `BarcodeGeneratorService`
- Produces: `GenerateShiftSalesReportPdf()`, `GenerateStockValuationReportPdf()`, `GenerateCustomerKhataReportPdf()`.

- [ ] **Step 1: Implement `ExecutiveReportPdfEngine.cs` using QuestPDF Fluent API**
- [ ] **Step 2: Include store header, Theni address, Neon/B&W logo, time period, and itemized financial tables**
- [ ] **Step 3: Verify build with `dotnet build`**

---

### Task 3: God Mode & Reports ViewModel & Windows 11 Fluent UI (`GodModeViewModel.cs` & `GodModeView.xaml`)

**Files:**
- Create: `src/SasanERP.UI/ViewModels/GodModeViewModel.cs`
- Create: `src/SasanERP.UI/Views/GodModeView.xaml`
- Create: `src/SasanERP.UI/Views/GodModeView.xaml.cs`
- Modify: `src/SasanERP.UI/ViewModels/MainViewModel.cs`
- Modify: `src/SasanERP.UI/MainWindow.xaml`
- Modify: `src/SasanERP.UI/MainWindow.xaml.cs`
- Modify: `src/SasanERP.UI/App.xaml.cs`

**Interfaces:**
- Consumes: `PeriodAnalyticsService`, `ExecutiveReportPdfEngine`, `MainViewModel`
- Produces: In-place 4-tab God Mode module in Navigation Rail with live charts, full entity editors, Khata manager, and 1-click report PDF exports.

- [ ] **Step 1: Implement `GodModeViewModel.cs` handling all 4 tabs (Period Reports, Stock Editor, Customer Khata, Invoice Void)**
- [ ] **Step 2: Implement `GodModeView.xaml` with tabbed interface and high-performance DataGrids**
- [ ] **Step 3: Add `NavigationSection.GodMode` and update navigation rail in `MainWindow.xaml`**
- [ ] **Step 4: Register services in `App.xaml.cs`**
- [ ] **Step 5: Verify build with `dotnet build`**

---

### Task 4: Comprehensive Verification Suite Update (Stage 17) & Final Verification

**Files:**
- Modify: `src/SasanERP.Verification/Program.cs`

**Interfaces:**
- Consumes: `PeriodAnalyticsService`, `ExecutiveReportPdfEngine`
- Produces: 17-stage verification suite certifying time-range analytics, invoice voiding with stock return, and PDF report generation.

- [ ] **Step 1: Add Stage 17 to `SasanERP.Verification/Program.cs` testing time-range queries, unit editing, invoice voiding, and report PDF export**
- [ ] **Step 2: Run verification suite `dotnet run --project src/SasanERP.Verification/SasanERP.Verification.csproj`**
- [ ] **Step 3: Commit changes with conventional git commit**
