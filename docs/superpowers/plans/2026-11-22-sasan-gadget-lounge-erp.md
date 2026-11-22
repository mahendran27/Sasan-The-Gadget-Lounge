# Sasan The Gadget Lounge - Enterprise ERP, POS & Inventory Management System
## Architecture & Technical Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a production-grade, high-performance Windows Desktop ERP, POS Billing, Barcode, and Accounting system in C# (.NET 8 LTS) with WPF, EF Core 8, QuestPDF, and ZXing for **"Sasan The Gadget Lounge"**, Theni, Tamil Nadu.

**Architecture:** Clean Architecture with 4 distinct visual studio layers (`SasanERP.Core`, `SasanERP.Data`, `SasanERP.Services`, `SasanERP.UI`) following MVVM pattern with `CommunityToolkit.Mvvm` and `Wpf.Ui` (Fluent Design System). Double-entry accounting ledger engine for complete ACID financial compliance.

**Tech Stack:**
- **Runtime:** .NET 8 LTS (C# 12) Windows Desktop (x64)
- **UI:** WPF with `Wpf.Ui` (Windows 11 Fluent Design System), `LiveChartsCore.SkiaSharpView.WPF`
- **MVVM:** `CommunityToolkit.Mvvm` (Source generators for observables & commands)
- **Database & ORM:** EF Core 8 with SQLite (Standalone) & PostgreSQL/SQL Server support
- **PDF Invoicing:** `QuestPDF` (Fluent vector-based A4 Tax Invoices)
- **Barcodes & Printing:** `ZXing.Net` (Code128, QR Code) & TSPL/ESC-POS direct thermal commands
- **Excel & Tax Reports:** `ClosedXML` for GSTR-1 and GSTR-3B audit reports

---

## Global Constraints & Business Rules
1. **Business Identity:**
   - Name: *Sasan The Gadget Lounge*
   - Address: *106/7, Karthik Mahall, Subban St, Opposite Theni, Suppan Ragavan Colony, NRT Nagar, Theni, Tamil Nadu - 625531*
   - Phone: *+91 9080803070*
   - Default Currency: Indian Rupee (₹, INR)
2. **Dual Inventory Model:**
   - Serialized: Strict tracking of IMEI 1, IMEI 2, Serial Number, Battery Health %, Condition Grade (A+, A, B), Warranty, and Source.
   - Non-Serialized: Batch, SKU, Brand, Quantity, and Reorder alerts.
3. **Indian GST Compliance:**
   - Standard GST (CGST 9% + SGST 9% = 18%, or IGST 18% interstate).
   - **GST Margin Scheme (Rule 32(5) CGST Rules 2017):** For pre-owned gadgets, GST applies exclusively on positive profit margin `(Sale Price - Purchase Cost)`. If margin is zero or negative, GST is ₹0. Invoices under Margin Scheme display mandatory statutory declaration.
4. **Hardware & POS Performance:**
   - Sub-50ms USB Barcode Scanner response on IMEI input.
   - Single-key hotkeys: `F2` (New Bill), `F4` (Print Sticker), `F9` (Complete & Print Invoice).
