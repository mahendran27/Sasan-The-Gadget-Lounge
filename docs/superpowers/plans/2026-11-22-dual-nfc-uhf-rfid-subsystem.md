# Implementation Plan: Dual NFC (13.56 MHz) and UHF RFID (860-960 MHz) Tag Management, Programming, and Editing Subsystem

**Goal:** Build a complete, enterprise-grade NFC and UHF RFID Subsystem for "Sasan The Gadget Lounge" (.NET 8 C# / WPF), enabling:
1. **NFC Tag Editor & Programmer (13.56 MHz HF):** NDEF encoding/decoding, customer tap URL, device health specs, and 32-bit password write-protection for NTAG213/215/216 chips.
2. **UHF RFID Studio & Memory Bank Editor (860-960 MHz EPC Gen2):** Full 4-Bank read/write editor (Reserved 00, EPC 01, TID 10, User 11), SGTIN-96 / Custom Hex EPC encoding from IMEI, EAS anti-theft toggle, and Access Password protection.
3. **Rapid Bulk Stock Audit Radar:** Real-time multi-tag inventory count (50-200 tags/sec) comparing physical shelf stock with ERP database ledgers to instantly flag missing or misplaced devices.
4. **Desktop UI Studio & POS Integration:** `WirelessStudioWindow.xaml` with live reader monitors, memory bank hex views, fast-program intake workflow, and direct tap-to-cart POS checkout (`F6` hotkey).

---

## 1. Architecture & Component Map

```
SasanERP.Core/
├── Enums/
│   └── RfEnums.cs                     // RfFrequencyType, NfcChipType, UhfMemoryBank, UhfLockStatus, RfidScanMode
└── Models/
    └── RfTagModels.cs                 // NfcTagModel, UhfTagModel, StockAuditSession, StockAuditItem

SasanERP.Services/
└── Wireless/
    ├── NfcTagService.cs               // NDEF encoder/decoder, NTAG memory map, 32-bit PWD lock, WinSCard/PC-SC interop + simulator
    ├── UhfRfidService.cs              // EPC Gen2 4-bank read/write, SGTIN-96 builder, COM/Serial/LLRP listener, bulk stream processor
    ├── UnifiedWirelessManager.cs      // Hardware orchestrator detecting USB NFC & UHF readers, dispatching tap events
    └── StockAuditService.cs           // Reconciles physical RFID counts vs database stock, audits aging devices

SasanERP.UI/
├── Services/
│   └── ThemeService.cs                // (Integrated)
├── ViewModels/
│   ├── WirelessStudioViewModel.cs     // NFC Editor, UHF 4-Bank Editor, Live Bulk Radar Audit
│   └── PosViewModel.cs                // Integrated NFC/RFID auto-add cart listener
└── Views/
    ├── WirelessStudioWindow.xaml      // Fluent UI with 3 tabs (NFC Studio, UHF Bank Editor, Rapid Stock Audit)
    └── MainWindow.xaml                // Added [F6] 📡 NFC & UHF RFID Studio toolbar button
```

---

## 2. Phase Breakdown

- **Phase 1: Domain Enums & Models (`SasanERP.Core`)**
  - Define `RfFrequencyType`, `NfcChipType`, `UhfMemoryBank`, `UhfLockStatus`, `RfidScanMode`.
  - Create contracts `NfcTagModel`, `UhfTagModel`, `StockAuditSession`, and `StockAuditItem`.

- **Phase 2: Wireless Services (`SasanERP.Services/Wireless`)**
  - Implement `NfcTagService.cs` with NDEF packaging, NTAG213/215/216 specs, 32-bit PWD protection, and simulated PC/SC runner.
  - Implement `UhfRfidService.cs` with Gen2 4-bank editing (Reserved, EPC, TID, User), SGTIN-96 conversion, EAS bit alarm, and COM/Serial bulk reader stream listener.
  - Implement `StockAuditService.cs` for reconciliation between live RFID reads and ERP SQLite stock.
  - Implement `UnifiedWirelessManager.cs` orchestrating hardware connections and event routing.

- **Phase 3: UI Studio & POS Integration (`SasanERP.UI`)**
  - Implement `WirelessStudioViewModel.cs` with 3 tabs: NFC Programmer, UHF Gen2 Bank Editor, Bulk Radar Audit.
  - Create `WirelessStudioWindow.xaml` and `.xaml.cs` with Windows 11 Fluent styling, memory bank hex inspectors, and live audit discrepancy table.
  - Update `MainWindow.xaml` to add `[F6]` shortcut and top-bar button.
  - Update `PosViewModel.cs` to add tag tap listener to auto-insert device into POS Cart.

- **Phase 4: Comprehensive Verification (`SasanERP.Verification`)**
  - Update `Program.cs` to test:
    - NFC NDEF write/read with IMEI & customer URL
    - NFC 32-bit password locking
    - UHF EPC Gen2 4-bank reading and writing
    - UHF Bulk Inventory Radar stream reconciliation (Matched, Missing, Discrepancy)
  - Verify complete build and execution with 0 errors.
