# God Mode Omni-Expansion & Executive Reports Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Elevate Sasan The Gadget Lounge's God Mode into a complete 360-degree business management hub with in-place Invoice payment mutation, Live Stock Valuation PDF reports with shelf-aging radar, and Customer Khata Statement PDF reports with dynamic UPI QR payment integration.

**Architecture:** 
- `PeriodAnalyticsService.cs`: Add Live Stock Valuation computation with aging radar, Customer Khata Statement chronological ledger, and Invoice Payment Split re-alignment with ACID ledger vouchers.
- `ExecutiveReportPdfEngine.cs`: Add A4 vector PDF layouts for `StockValuationReport` and `CustomerKhataStatement` with QuestPDF and QR codes.
- `GodModeViewModel.cs` & `GodModeView.xaml`: Expose 1-click PDF exports, shelf-aging filters, and invoice payment split adjustment in UI.
- `SasanERP.Verification/Program.cs`: Expand Stage 17 to verify all new God Mode capabilities.

**Tech Stack:** C# 12, .NET 8, EF Core 8, SQLite WAL, QuestPDF Fluent API, CommunityToolkit.Mvvm, WPF.

---

## Tasks

- [ ] Task 1: Add DTOs and Methods to `PeriodAnalyticsService.cs`
  - `ComputeStockValuationReportAsync()`
  - `GetCustomerKhataStatementAsync(int customerId)`
  - `UpdateInvoicePaymentSplitAsync(...)`
- [ ] Task 2: Implement PDF Generation in `ExecutiveReportPdfEngine.cs`
  - `GenerateStockValuationReportPdf(...)`
  - `GenerateCustomerKhataStatementPdf(...)`
- [ ] Task 3: Update `GodModeViewModel.cs` & `GodModeView.xaml`
  - Wire up stock valuation PDF export & shelf aging filter
  - Wire up customer Khata statement PDF export
  - Wire up invoice payment split editor
- [ ] Task 4: Verify with `dotnet build` and `SasanERP.Verification`
  - Test Stock Valuation PDF output (> 0 bytes)
  - Test Customer Khata Statement PDF output (> 0 bytes)
  - Test Invoice Payment Split mutation & ACID balance
  - All 17 verification stages pass with 0 errors/warnings
- [ ] Task 5: Git Commit & Remote Push to GitHub
