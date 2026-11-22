# Implementation Plan: 7 Invoice Templates, 6 UI Themes, and 7 Barcode Sticker Styles for Sasan The Gadget Lounge

**Goal:** Implement all 7 Invoice Templates, 6 UI Design Themes/Modes, and 7 Barcode Sticker Styles from Sections 1, 2, and 3, and integrate a comprehensive Settings & Selector system in WPF/C# (.NET 8 LTS).

**Architecture:**
- `SasanERP.Core`: Enums for `InvoiceTemplateStyle` (1-7), `AppThemeMode` (1-6), `BarcodeLabelStyle` (1-7). Configuration settings entity.
- `SasanERP.Services`:
  - `InvoicePrintEngine.cs`: Factory dispatching QuestPDF generation across all 7 invoice templates (Minimalist, Luxury, Margin Scheme, 80mm Thermal, Dual-Copy Counterfoil, WhatsApp Story, Health Certificate).
  - `BarcodeLabelEngine.cs`: Factory dispatching QuestPDF & TSPL generation across all 7 sticker designs (Flagship 50x30, Compact 50x25, Dumbbell, Warranty Void Seal, Dual-QR, Multi-Up 24/40 A4 Sheet, Metallic).
- `SasanERP.UI`:
  - `ThemeService.cs`: Manages dynamic switching between all 6 themes (Cyber Cyan, Titanium Light, Nordic Frost, Kiosk Touch, Terminal HUD, Auto Day/Night).
  - UI Modals and Controls:
    - Template Selector in POS checkout (`F9`)
    - Barcode Style Selector in Inventory Intake (`F3`) with live sticker preview
    - Theme Switcher toolbar in `MainWindow`
