# Modern React + Tailwind Luxury Desktop UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transform Sasan The Gadget Lounge ERP into a 10/10 luxury modern desktop application with a responsive React 18 + TypeScript + Tailwind CSS frontend, while retaining 100% of the battle-tested C# .NET 8 backend engine (SQLite ACID, EF Core 8, double-entry accounting, GST Rule 32(5), QuestPDF, TSPL printing, and USB scanner drivers) hosted seamlessly via Microsoft WebView2.

**Architecture:** Decoupled Headless Hybrid Desktop Architecture. The backend is an embedded ASP.NET Core Minimal API service running inside the native Windows desktop process, communicating with existing `SasanERP.Services` and `SasanERP.Data`. The frontend is a high-performance single-page application built with React 18, Vite, TypeScript, Tailwind CSS, and Lucide Icons, served offline inside hardware-accelerated Microsoft WebView2 with hotkey listeners (F1-F12), real-time barcode scanning via WebSockets, and fluid responsive layouts for all monitor resolutions.

**Tech Stack:**
- **Backend Core:** C# .NET 8, ASP.NET Core Minimal APIs, Entity Framework Core 8, SQLite (WAL mode), QuestPDF 2024, SkiaSharp, TSPL/ESC-POS byte streams.
- **Frontend UI:** React 18, Vite, TypeScript, Tailwind CSS, Lucide React, Framer Motion (restrained), Tabular Numbers (`font-mono`).
- **Desktop Host:** Microsoft.Web.WebView2 WinForms/WPF Native Shell, Inno Setup 6, Single-file standalone Windows x64 packager.

**Spec:** Modern Luxury POS & Retail ERP Architecture for Sasan The Gadget Lounge (Theni Outlet).

## Global Constraints

- Backend business logic, double-entry accounting invariants, and GST Rule 32(5) margin tax calculations must remain 100% in C# .NET 8 services.
- Offline-first: The application must function 100% locally with zero internet dependency.
- Palette Restraint (`antislop-ui` R-29): Deep Obsidian Canvas (`#080E1A`), Sleek Slate Card Surfaces (`#0F172A`, `#1E293B`), Electric Cyber Cyan (`#00E5FF`) and Warm Titanium Gold (`#F59E0B`) accents, with pure Titanium White text (`#F8FAFC`).
- No decorative emojis in UI buttons, headings, or table cells (use clean, weighted SVG Lucide icons instead).
- Full Keyboard Ergonomics: Complete POS operations operable via keyboard hotkeys (`F1` to `F12`, `Enter`, `Escape`, `Tab`).
- Hardware Spooling: Physical TSPL barcode printing and ESC/POS thermal printing must trigger directly through the C# native drivers without browser print dialogs.

## Review Focus

1. **POS Speed & Latency:** Ensure barcode scanning, cart addition, and discount/split tender calculate and render in < 16ms (60 FPS).
2. **Thermal & Label Printing Direct Execution:** Clicking [F9 Print] or Printing Barcode Stickers triggers C# TSPL / QuestPDF directly without user friction.
3. **Responsive Grid & Scaling:** Layout must reflow gracefully across 1366x768, 1080p, 2K, and 4K without text clipping or horizontal scrolling.
4. **WebSocket Scanner Auto-Reconnect:** If USB serial scanner is unplugged and replugged, the connection must seamlessly recover.
5. **Release Packaging Integrity:** Final build must package as an independent single-file executable and Windows installer (`Setup.exe`).

---

### Task 1: Create Embedded ASP.NET Core API Endpoints in C# Backend

**Files:**
- Create: `src/SasanERP.Server/SasanApiServer.cs`
- Create: `src/SasanERP.Server/Controllers/PosEndpoints.cs`
- Create: `src/SasanERP.Server/Controllers/DashboardEndpoints.cs`
- Create: `src/SasanERP.Server/Controllers/InventoryEndpoints.cs`
- Create: `src/SasanERP.Server/Controllers/ReportEndpoints.cs`
- Test: `tests/SasanERP.Server.Tests/ApiEndpointsTests.cs`

**Interfaces:**
- Consumes: `AppDbContext`, `InvoiceService`, `StockService`, `PeriodAnalyticsService`, `ExecutiveDashboardService`, `GstReportService`, `StoreExpenseService`
- Produces: RESTful JSON endpoints on `http://127.0.0.1:5218/api/*` and WebSocket on `/ws/scanner`

- [ ] **Step 1: Write integration tests for API endpoints**
  Verify `/api/dashboard/telemetry`, `/api/products/search`, `/api/pos/invoice/checkout`, and `/api/reports/live-stock`.
- [ ] **Step 2: Run test to verify it fails**
  Run: `dotnet test tests/SasanERP.Server.Tests` -> Expected: Missing project/endpoint.
- [ ] **Step 3: Implement ASP.NET Core Minimal API endpoints**
  Configure Kestrel to bind to `127.0.0.1:5218` with CORS for local WebView2 origin, register existing DI services.
- [ ] **Step 4: Run tests to verify all endpoints pass**
  Run: `dotnet test tests/SasanERP.Server.Tests` -> Expected: PASS (200 OK with accurate JSON).
- [ ] **Step 5: Commit**
  `git commit -m "feat(api): add embedded asp.net core minimal api server for decoupled frontend"`

---

### Task 2: Scaffold Luxury React 18 + Vite + Tailwind CSS Project (`src/SasanERP.WebUI`)

**Files:**
- Create: `src/SasanERP.WebUI/package.json`
- Create: `src/SasanERP.WebUI/vite.config.ts`
- Create: `src/SasanERP.WebUI/tailwind.config.js`
- Create: `src/SasanERP.WebUI/src/index.css`
- Create: `src/SasanERP.WebUI/src/App.tsx`
- Create: `src/SasanERP.WebUI/src/types/domain.ts`

**Interfaces:**
- Consumes: Node.js 26.13.0, Tailwind CSS 3.4+, Lucide React
- Produces: Production build in `src/SasanERP.WebUI/dist/`

- [ ] **Step 1: Initialize Vite React TypeScript project**
  Scaffold Vite with React and TypeScript, install `tailwindcss`, `lucide-react`, `clsx`, `tailwind-merge`.
- [ ] **Step 2: Configure Luxury Obsidian Theme in `tailwind.config.js`**
  Define palette colors: `sasan-canvas (#080E1A)`, `sasan-surface (#0F172A)`, `sasan-card (#131E33)`, `sasan-border (#1E293B)`, `sasan-cyan (#00E5FF)`, `sasan-gold (#F59E0B)`.
- [ ] **Step 3: Define TypeScript Domain Models matching C# Entities**
  Define `Product`, `SerializedUnit`, `Invoice`, `InvoiceItem`, `Customer`, `DashboardTelemetry`, `PeriodSalesSummary`.
- [ ] **Step 4: Run Vite build to verify clean bundling**
  Run: `npm run build` inside `src/SasanERP.WebUI/` -> Expected: Clean build with 0 errors.
- [ ] **Step 5: Commit**
  `git commit -m "feat(ui): scaffold luxury react vite tailwind webui project"`

---

### Task 3: Build Cockpit Navigation Shell & Real-Time Top Bar with Hotkeys

**Files:**
- Create: `src/SasanERP.WebUI/src/components/layout/Shell.tsx`
- Create: `src/SasanERP.WebUI/src/components/layout/TopHeader.tsx`
- Create: `src/SasanERP.WebUI/src/components/layout/SidebarRail.tsx`
- Create: `src/SasanERP.WebUI/src/hooks/useKeyboardShortcuts.ts`

**Interfaces:**
- Consumes: Navigation state (`activeModule`), hotkey hook (`F1` to `F12`)
- Produces: Responsive cockpit navigation shell with live status indicators and window zoom

- [ ] **Step 1: Write unit tests for keyboard navigation hook**
  Verify pressing `F1` switches to Dashboard, `F2` to POS, `F3` to Stock, `F4` to Invoices, `F5` to Intake, `F12` to God Mode.
- [ ] **Step 2: Implement `useKeyboardShortcuts.ts`**
  Intercept keydown events globally (ignoring when user is typing inside text inputs, except function keys).
- [ ] **Step 3: Implement `TopHeader.tsx` and `SidebarRail.tsx`**
  Brand banner with Sasan Logo, Theni Outlet badge, live SQLite ACID indicator, active cashier, zoom level control, and navigation items.
- [ ] **Step 4: Verify in browser**
  Run: `npm run dev` -> Verify smooth tab switching and zero layout shift.
- [ ] **Step 5: Commit**
  `git commit -m "feat(ui): implement luxury cockpit shell with global f1-f12 keyboard hotkeys"`

---

### Task 4: Build Executive Dashboard View (`[F1] Dashboard`)

**Files:**
- Create: `src/SasanERP.WebUI/src/views/DashboardView.tsx`
- Create: `src/SasanERP.WebUI/src/components/dashboard/KpiMetricCard.tsx`
- Create: `src/SasanERP.WebUI/src/components/dashboard/AgingDeviceRadar.tsx`
- Create: `src/SasanERP.WebUI/src/components/dashboard/LowStockDeficitTable.tsx`
- Create: `src/SasanERP.WebUI/src/components/dashboard/RecentSalesFeed.tsx`

**Interfaces:**
- Consumes: `GET /api/dashboard/telemetry?timeframe=Today|Week|Month`
- Produces: Real-time telemetry dashboard with live stock valuation, profit metrics, and low-stock restock actions

- [ ] **Step 1: Implement `KpiMetricCard.tsx` with high-contrast typography**
  Display Today's Revenue, Net Gross Profit, Total Stock Valuation, and Khata Receivables with tabular currency numbers.
- [ ] **Step 2: Implement `LowStockDeficitTable.tsx`**
  Table with SKU badge, warning stock count pill, reorder threshold, and one-click `Restock via Intake [F5]` button. Zero white row flash!
- [ ] **Step 3: Implement `AgingDeviceRadar.tsx`**
  Shelf-aging radar highlighting devices older than 60 days with amber/rose tags and IMEI numbers.
- [ ] **Step 4: Connect with C# API telemetry endpoint and verify real-time data**
- [ ] **Step 5: Commit**
  `git commit -m "feat(ui): build executive dashboard with real-time kpi telemetry and aging radar"`

---

### Task 5: Build High-Speed POS Checkout & Split Tender Module (`[F2] POS Billing`)

**Files:**
- Create: `src/SasanERP.WebUI/src/views/PosBillingView.tsx`
- Create: `src/SasanERP.WebUI/src/components/pos/BarcodeScannerInput.tsx`
- Create: `src/SasanERP.WebUI/src/components/pos/CartItemList.tsx`
- Create: `src/SasanERP.WebUI/src/components/pos/SplitPaymentModal.tsx`
- Create: `src/SasanERP.WebUI/src/components/pos/InvoicePreviewDrawer.tsx`

**Interfaces:**
- Consumes: `GET /api/products/search`, `POST /api/pos/invoice/checkout`, `POST /api/print/thermal`
- Produces: Sub-10ms cart interaction, split tender calculation (Cash + UPI + Card + Exchange), instant thermal/A4 print

- [ ] **Step 1: Implement `BarcodeScannerInput.tsx`**
  Auto-focused scanner input supporting physical barcode scanner keyboard wedge and WebSocket serial scanner.
- [ ] **Step 2: Implement Cart & Margin Scheme Indicator**
  Display cart items with serial numbers, warranty, GST rate, Rule 32(5) badge, and quantity modifiers.
- [ ] **Step 3: Implement Split Tender & Customer Khata Balance**
  5-way split tender (Cash, UPI with dynamic QR, Card, Exchange, Finance) with real-time balance due indicator.
- [ ] **Step 4: Hook up QuestPDF Invoice Preview & 1-Click Print**
  Instant invoice preview drawer showing formatted invoice with option to print or dispatch via WhatsApp.
- [ ] **Step 5: Commit**
  `git commit -m "feat(ui): implement high-speed pos checkout with split payment and instant printing"`

---

### Task 6: Build Stock Registry, Intake & 7 Barcode Label Generator (`[F3]` & `[F5]`)

**Files:**
- Create: `src/SasanERP.WebUI/src/views/StockRegistryView.tsx`
- Create: `src/SasanERP.WebUI/src/views/DeviceIntakeView.tsx`
- Create: `src/SasanERP.WebUI/src/components/barcode/BarcodeStickerPrintModal.tsx`

**Interfaces:**
- Consumes: `GET /api/inventory/units`, `POST /api/inventory/intake`, `POST /api/print/tspl-labels`
- Produces: Serialized device inventory, bulk accessories, condition grading, and 7-style TSPL barcode sticker printing

- [ ] **Step 1: Build Stock Registry with advanced filters**
  Filter by Brand, Category, Condition (New, Like New, Grade A, Refurbished), Aging days, and in-stock status.
- [ ] **Step 2: Build Device Intake Form**
  Intake for mobile phones & laptops with IMEI1, IMEI2, Serial, Color, Storage, Battery Health, Supplier, Purchase Cost, Tagged Price.
- [ ] **Step 3: Build Barcode Sticker Printing Dialog**
  Select from 7 styles (Flagship 50x30, Compact 50x25, Jewelry Tag, Tamper Void Seal, Dual QR, Minimalist, Master Carton) with TSPL direct print.
- [ ] **Step 4: Verify direct thermal label printing**
- [ ] **Step 5: Commit**
  `git commit -m "feat(ui): implement stock registry, device intake and 7-style tspl barcode sticker generator"`

---

### Task 7: Build Daybook, GST Hub, Store Settings & God Mode (`[F6]`, `[F7]`, `[F10]`, `[F12]`)

**Files:**
- Create: `src/SasanERP.WebUI/src/views/DaybookExpensesView.tsx`
- Create: `src/SasanERP.WebUI/src/views/GstReportsView.tsx`
- Create: `src/SasanERP.WebUI/src/views/StoreSettingsView.tsx`
- Create: `src/SasanERP.WebUI/src/views/GodModeView.tsx`

**Interfaces:**
- Consumes: `POST /api/expenses`, `GET /api/reports/gst`, `GET /api/reports/shift-sales`, `POST /api/godmode/void-invoice`
- Produces: Operational expense vouchers, GSTR-1 Excel export, Khata credit settlement, executive shift audit PDFs

- [ ] **Step 1: Build Daybook & Operating Expenses**
  Cash in drawer balance, record rent/utilities/petty cash vouchers, EBITDA true net profit calculator.
- [ ] **Step 2: Build GST & Tax Compliance Hub**
  GSTR-1 B2B / B2C summaries, Rule 32(5) margin tax ledger, 1-click Excel export.
- [ ] **Step 3: Build Store Settings View**
  Company name, address, GSTIN, UPI ID, logo upload, print mode (Color / Laser B&W), terms and conditions.
- [ ] **Step 4: Build God Mode & Executive BI View**
  Full entity edit/delete, customer Khata ledger settlement with dynamic UPI payment link, invoice voiding with automatic restock, executive PDF reports.
- [ ] **Step 5: Commit**
  `git commit -m "feat(ui): implement daybook, gst hub, store settings and god mode bi in luxury webui"`

---

### Task 8: Package Native Windows WebView2 Shell & Build Standalone Installer

**Files:**
- Create: `src/SasanERP.DesktopHost/MainWindow.xaml`
- Create: `src/SasanERP.DesktopHost/MainWindow.xaml.cs`
- Create: `src/SasanERP.DesktopHost/Program.cs`
- Modify: `build-installer.ps1`
- Test: Verification suite

**Interfaces:**
- Consumes: Bundled React SPA in `src/SasanERP.WebUI/dist/`, C# Backend Services
- Produces: Standalone single-file executable `SasanERP.exe` and Inno Setup installer `SasanERP-Setup-v2.5.0.exe`

- [ ] **Step 1: Build native Windows desktop host with Microsoft.Web.WebView2**
  Embed WebView2 control inside borderless/custom caption desktop window, map virtual host `app.sasanerp.local` to Vite dist assets, connect API bridge.
- [ ] **Step 2: Update `build-installer.ps1` to compile both React UI and C# host**
  Automate `npm run build` followed by `dotnet publish --self-contained -r win-x64` and Inno Setup compilation.
- [ ] **Step 3: Execute full end-to-end verification**
  Run all 23 verification stages + API integration tests.
- [ ] **Step 4: Verify single-file standalone execution and installer generation**
- [ ] **Step 5: Commit**
  `git commit -m "feat(host): package native webview2 desktop shell and generate standalone production installer"`
