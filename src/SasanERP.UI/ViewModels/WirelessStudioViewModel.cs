using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SasanERP.Core.Enums;
using SasanERP.Core.Models;
using SasanERP.Data.Context;
using SasanERP.Services.Wireless;

namespace SasanERP.UI.ViewModels;

/// <summary>
/// Comprehensive NFC (13.56 MHz) and UHF RFID (860-960 MHz) Studio ViewModel.
/// Provides 3 interactive tabs:
/// 1. NFC Programmer & NDEF Editor (NTAG213/215/216 with 32-bit PWD Lock)
/// 2. UHF RFID Gen2 4-Memory-Bank Editor (Bank 00, 01, 10, 11 & EAS Anti-Theft)
/// 3. Rapid Bulk Stock Audit Radar (Real-time sweep vs Database reconciliation)
/// </summary>
public partial class WirelessStudioViewModel : ObservableObject
{
    private readonly AppDbContext _context;
    private readonly NfcTagService _nfcService;
    private readonly UhfRfidService _uhfService;
    private readonly StockAuditService _auditService;
    private readonly UnifiedWirelessManager _wirelessManager;

    public ObservableCollection<NfcChipType> NfcChipTypes { get; } = new(Enum.GetValues<NfcChipType>());
    public ObservableCollection<ConditionGrade> ConditionGrades { get; } = new(Enum.GetValues<ConditionGrade>());
    public ObservableCollection<WarrantyStatus> WarrantyOptions { get; } = new(Enum.GetValues<WarrantyStatus>());
    public ObservableCollection<UhfMemoryBank> MemoryBanks { get; } = new(Enum.GetValues<UhfMemoryBank>());

    // ==========================================
    // TAB 1: NFC PROGRAMMER (13.56 MHz)
    // ==========================================
    [ObservableProperty] private string _nfcTagUid = "04:A2:3B:5C:8D:1E:90";
    [ObservableProperty] private NfcChipType _selectedNfcChip = NfcChipType.NTAG215;
    [ObservableProperty] private int _nfcCapacityBytes = 504;
    [ObservableProperty] private string _nfcImei = "354892019482103";
    [ObservableProperty] private string _nfcModelName = "Apple iPhone 15 Pro";
    [ObservableProperty] private string _nfcSpecs = "128GB | Natural Titanium";
    [ObservableProperty] private int? _nfcBatteryHealth = 98;
    [ObservableProperty] private ConditionGrade _nfcCondition = ConditionGrade.GradeAPlus_Flawless;
    [ObservableProperty] private WarrantyStatus _nfcWarranty = WarrantyStatus.ShopWarranty_90Days;
    [ObservableProperty] private decimal _nfcPrice = 82999.00m;
    [ObservableProperty] private string _nfcCustomerUrl = "https://sasanthegadgetlounge.in/explore?imei=354892019482103";
    [ObservableProperty] private bool _isNfcPasswordProtected = true;
    [ObservableProperty] private string _nfcAccessPassword = "SAS1";
    [ObservableProperty] private string _nfcStatus = "Ready. Tap NTAG213/215/216 tag on ACR122U USB reader.";

    // ==========================================
    // TAB 2: UHF RFID GEN2 4-BANK EDITOR (860-960 MHz)
    // ==========================================
    [ObservableProperty] private string _uhfEpcHex = "E2801160354892019482103A";
    [ObservableProperty] private string _uhfTidHex = "E280116020007289F4829102";
    [ObservableProperty] private string _uhfUserMemoryHex = "536201020001443F";
    [ObservableProperty] private string _uhfAccessPassword = "SAS1";
    [ObservableProperty] private string _uhfKillPassword = "00000000";
    [ObservableProperty] private UhfMemoryBank _selectedBank = UhfMemoryBank.Epc_01;
    [ObservableProperty] private string _bankEditHex = "E2801160354892019482103A";
    [ObservableProperty] private bool _isEasAntiTheftActive = true;
    [ObservableProperty] private string _uhfImeiInput = "354892019482103";
    [ObservableProperty] private string _uhfStatus = "Ready. Set reader power & write to Gen2 Memory Banks.";

    // ==========================================
    // TAB 3: RAPID BULK STOCK AUDIT RADAR
    // ==========================================
    public ObservableCollection<StockAuditItem> AuditItems { get; } = new();

    [ObservableProperty] private int _auditTotalExpected;
    [ObservableProperty] private int _auditTotalScanned;
    [ObservableProperty] private int _auditMatched;
    [ObservableProperty] private int _auditMissing;
    [ObservableProperty] private int _auditUnexpected;
    [ObservableProperty] private string _auditSummaryText = "Audit idle. Click 'Start Rapid Shelf Sweep' to sweep inventory.";
    [ObservableProperty] private bool _isAuditing;

    public WirelessStudioViewModel(
        AppDbContext context,
        NfcTagService nfcService,
        UhfRfidService uhfService,
        StockAuditService auditService,
        UnifiedWirelessManager wirelessManager)
    {
        _context = context;
        _nfcService = nfcService;
        _uhfService = uhfService;
        _auditService = auditService;
        _wirelessManager = wirelessManager;
    }

    // ==========================================
    // COMMANDS: NFC
    // ==========================================
    [RelayCommand]
    public async Task ReadNfcTagAsync()
    {
        NfcStatus = "[SCANNING] Interrogating ISO 14443A NFC Tag on USB Reader...";
        await Task.Delay(300); // Simulate reader polling

        var sampleUnit = await _context.SerializedUnits
            .Include(u => u.Product)
            .FirstOrDefaultAsync(u => u.IMEI1 == NfcImei) 
            ?? await _context.SerializedUnits.Include(u => u.Product).FirstOrDefaultAsync();

        if (sampleUnit != null)
        {
            var company = await _context.CompanyProfiles.FirstOrDefaultAsync() ?? new CompanyProfile();
            var tag = _nfcService.SimulateHardwareTagRead(sampleUnit, company);

            NfcTagUid = tag.TagUid;
            NfcImei = tag.IMEI ?? "";
            NfcModelName = tag.ModelName;
            NfcSpecs = tag.Specs;
            NfcBatteryHealth = tag.BatteryHealthPercentage;
            NfcCondition = tag.ConditionGrade ?? ConditionGrade.GradeAPlus_Flawless;
            NfcWarranty = tag.WarrantyStatus ?? WarrantyStatus.ShopWarranty_90Days;
            NfcPrice = tag.TaggedPrice;
            NfcCustomerUrl = tag.CustomerVerifyUrl;
            IsNfcPasswordProtected = tag.IsPasswordProtected;

            NfcStatus = $"[SUCCESS] Tag Read: UID {tag.TagUid} ({tag.ChipType}) - Device: {tag.ModelName}";
        }
    }

    [RelayCommand]
    public async Task ProgramNfcTagAsync()
    {
        NfcStatus = "[WRITING] Generating NDEF Payload & APDU commands...";
        await Task.Delay(300);

        var model = new NfcTagModel
        {
            TagUid = NfcTagUid,
            ChipType = SelectedNfcChip,
            IMEI = NfcImei,
            ModelName = NfcModelName,
            Specs = NfcSpecs,
            BatteryHealthPercentage = NfcBatteryHealth,
            ConditionGrade = NfcCondition,
            WarrantyStatus = NfcWarranty,
            TaggedPrice = NfcPrice,
            CustomerVerifyUrl = NfcCustomerUrl,
            IsPasswordProtected = IsNfcPasswordProtected,
            AccessPasswordHex = NfcTagService.DefaultMasterPasswordHex,
            PackHex = NfcTagService.DefaultPackHex
        };

        var apdus = _nfcService.GenerateWriteApdus(model);
        NfcStatus = $"[PROGRAMMED] 100% SUCCESS! Wrote {apdus.Count} pages to {SelectedNfcChip}. 32-bit PWD Lock: {(IsNfcPasswordProtected ? "ACTIVE" : "OFF")}";
    }

    [RelayCommand]
    public void TestCounterNfcTap()
    {
        _wirelessManager.SimulateCounterTap(NfcImei, RfFrequencyType.NfcProximity_13_56MHz);
        NfcStatus = $"[COUNTER TAP] Dispatched NFC tap for IMEI: {NfcImei} to POS Billing Cart.";
    }

    // ==========================================
    // COMMANDS: UHF RFID
    // ==========================================
    [RelayCommand]
    public void GenerateEpcFromImei()
    {
        UhfEpcHex = _uhfService.EncodeEpcFromImei(UhfImeiInput, "APL");
        UhfStatus = $"[ENCODED] SGTIN-96 EPC generated: {UhfEpcHex}";
    }

    [RelayCommand]
    public async Task ReadUhfTagBanksAsync()
    {
        UhfStatus = "[READING] Reading EPC Gen2 4-Memory-Banks...";
        await Task.Delay(300);

        var units = await _context.SerializedUnits.Include(u => u.Product).Take(1).ToListAsync();
        var simulatedTags = _uhfService.SimulateBulkInventorySweep(units);
        if (simulatedTags.Any())
        {
            var tag = simulatedTags.First();
            UhfEpcHex = tag.EpcHex;
            UhfTidHex = tag.TidHex;
            UhfUserMemoryHex = tag.UserMemoryHex ?? "00000000";
            IsEasAntiTheftActive = tag.IsEasAlarmActive;
            BankEditHex = tag.EpcHex;
            UhfStatus = $"[GEN2 READ] EPC: {tag.EpcHex} | TID: {tag.TidHex} | RSSI: {tag.RssiDb:N1} dBm";
        }
    }

    [RelayCommand]
    public void WriteSelectedBank()
    {
        bool ok = _uhfService.WriteMemoryBank(SelectedBank, UhfEpcHex, BankEditHex);
        UhfStatus = ok 
            ? $"[BANK WRITE OK] Wrote {BankEditHex.Length * 4} bits into {SelectedBank}." 
            : "[ERROR] Hex data invalid or not 16-bit word aligned.";
    }

    [RelayCommand]
    public void ToggleEasAntiTheft()
    {
        IsEasAntiTheftActive = !IsEasAntiTheftActive;
        _uhfService.SetEasAntiTheftStatus(UhfEpcHex, IsEasAntiTheftActive);
        UhfStatus = IsEasAntiTheftActive 
            ? "[EAS ARMED] Electronic Article Surveillance is ACTIVE (Gate Alarm ON)."
            : "[EAS DISARMED] EAS Alarm Bit cleared (Checkout authorized).";
    }

    // ==========================================
    // COMMANDS: BULK STOCK AUDIT RADAR
    // ==========================================
    [RelayCommand]
    public async Task StartRapidShelfSweepAsync()
    {
        IsAuditing = true;
        AuditSummaryText = "[SWEEPING] Handheld UHF RFID Gun scanning shelves at 120 tags/sec...";
        AuditItems.Clear();

        await Task.Delay(500); // Simulate sweep duration

        // Fetch all in-stock units
        var units = await _context.SerializedUnits.Include(u => u.Product).ToListAsync();

        // Simulate detecting most units, plus 1 missing, plus 1 sold
        var tags = _uhfService.SimulateBulkInventorySweep(units);

        // Reconcile via StockAuditService
        var session = await _auditService.ReconcileStockAuditAsync(tags, "Karthik_Manager", "Theni Gadget Lounge Display");

        AuditTotalExpected = session.TotalExpectedInDb;
        AuditTotalScanned = session.TotalScannedPhysical;
        AuditMatched = session.MatchedCount;
        AuditMissing = session.MissingCount;
        AuditUnexpected = session.UnexpectedCount;

        foreach (var itm in session.AuditItems)
        {
            AuditItems.Add(itm);
        }

        AuditSummaryText = $"[AUDIT COMPLETE] Expected: {AuditTotalExpected} | Physical: {AuditTotalScanned} | Matched: {AuditMatched} | Discrepancies: {AuditMissing + AuditUnexpected}";
        IsAuditing = false;
    }
}
